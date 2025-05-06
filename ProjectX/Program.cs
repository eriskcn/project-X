using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectX.Authorization;
using ProjectX.Data;
using ProjectX.Models;
using ProjectX.Services;
using DotNetEnv;
using ProjectX.Hubs;
using ProjectX.Services.Email;
using ProjectX.Services.GoogleAuth;
using ProjectX.Services.Notifications;
using ProjectX.Services.QR;
using ProjectX.Services.Stats;
using VNPAY.NET;

var builder = WebApplication.CreateBuilder(args);

// Register configuration for dependency injection
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// Configure Identity options
builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;

    // User settings
    options.User.RequireUniqueEmail = true;
});

builder.Services.Configure<GoogleSettings>(builder.Configuration.GetSection("Google"));
// Configure database context

Env.Load();
var saPassword = Environment.GetEnvironmentVariable("SA_PASSWORD");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?.Replace("PLACEHOLDER_PASSWORD", saPassword);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure cookie policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // Configure for cross-origin requests (important for development with Next.js)
    options.MinimumSameSitePolicy = SameSiteMode.None;

    // Always use HTTPS in production
    options.Secure = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;

    // Prevent JavaScript access to cookies
    options.HttpOnly = HttpOnlyPolicy.Always;
});

// Add Identity with custom User and Role types
builder.Services.AddIdentity<User, Role>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure JWT authentication
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ??
                                                                               throw new InvalidOperationException(
                                                                                   "JWT Key is not configured")))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "") ??
                                context.Request.Cookies["AccessToken"];
                return Task.CompletedTask;
            },

            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.HttpContext.Items["TokenExpired"] = true;
                }

                return Task.CompletedTask;
            },
            OnForbidden = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new { message = "No permission" });
                return context.Response.WriteAsync(result);
            }
        };
    });

// Configure CORS for Next.js frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJs",
        corsPolicyBuilder => corsPolicyBuilder
            .WithOrigins("http://localhost:3000") // Next.js development server
            .AllowCredentials() // Allow cookies to be sent
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Add authorization services
builder.Services.AddAuthorizationBuilder()
    // Add authorization services
    .AddPolicy("RecruiterVerifiedOnly", policy =>
        policy.Requirements.Add(new RecruiterVerifiedRequirement()));

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("EmailConfirmed", policy =>
        policy.Requirements.Add(new EmailConfirmedRequirement()));

builder.Services.AddScoped<IAuthorizationHandler, RecruiterVerifiedHandler>();
builder.Services.AddScoped<IAuthorizationHandler, EmailConfirmedHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, ProjectXAuthorizationPolicyProvider>();
// Register token service for JWT generation
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IVietQrService, VietQrService>();
builder.Services.AddScoped<IVnpay, Vnpay>();
builder.Services.AddScoped<IStatsService, StatsService>();
builder.Services.AddControllers();
builder.Services.AddSignalR();

// Add Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<DatabaseBackupService>();
// builder.WebHost.ConfigureKestrel(options =>
// {
//     options.ListenAnyIP(8443, listenOptions => { listenOptions.UseHttps(); });
// });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS - must be before authentication/authorization
app.UseCors("AllowNextJs");

app.UseStaticFiles();

app.UseHttpsRedirection();

// Enable cookie policy - must be before authentication
app.UseCookiePolicy();

app.Use(async (context, next) =>
{
    if (context.Items.ContainsKey("TokenExpired"))
    {
        context.Response.Clear();
        context.Response.StatusCode = 419;
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new { Status = 419, Message = "Access token has expired" });
        await context.Response.WriteAsync(result);
        return;
    }

    try
    {
        await next();

        if (!context.Response.HasStarted && context.Items.ContainsKey("TokenExpired"))
        {
            context.Response.Clear();
            context.Response.StatusCode = 419;
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new { Status = 419, Message = "Access token has expired" });
            await context.Response.WriteAsync(result);
        }
    }
    catch (Exception ex)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new
                { Status = 500, Message = "An error occurred", Error = ex.Message });
            await context.Response.WriteAsync(result);
        }
    }
});
// Enable authentication
app.UseAuthentication();

// Enable authorization
app.UseAuthorization();

app.MapHub<MessageHub>("/hubs/message");
app.MapHub<NotificationHub>("/hubs/notification");
app.MapControllers();

app.Run();