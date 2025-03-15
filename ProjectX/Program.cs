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

var builder = WebApplication.CreateBuilder(args);

// Register configuration for dependency injection
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// Configure Identity options
builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;

    // User settings
    options.User.RequireUniqueEmail = true;
});

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
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ??
                                                                throw new InvalidOperationException(
                                                                    "JWT Key is not configured")))
        };

        // Configure JWT bearer to read token from cookies
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["AccessToken"];
                return Task.CompletedTask;
            },
            OnForbidden = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new { message = "No permission" });
                return context.Response.WriteAsync(result);
            },

            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() != typeof(SecurityTokenExpiredException)) return Task.CompletedTask;
                context.Response.StatusCode = StatusCodes.Status419AuthenticationTimeout;
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new { message = "Access token has expired" });
                return context.Response.WriteAsync(result);
            }
        };
    });

// Configure CORS for Next.js frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        corsPolicyBuilder => corsPolicyBuilder
            .WithOrigins("http://localhost:3000") // Next.js development server
            .AllowCredentials() // Allow cookies to be sent
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Add authorization services
builder.Services.AddAuthorizationBuilder()
    // Add authorization services
    .AddPolicy("BusinessVerifiedOnly", policy =>
        policy.Requirements.Add(new BusinessVerifiedRequirement()));

builder.Services.AddSingleton<IAuthorizationHandler, BusinessVerifiedHandler>();


// Register token service for JWT generation
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddControllers();

// Add Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS - must be before authentication/authorization
app.UseCors("AllowLocalhost");

app.UseStaticFiles();

app.UseHttpsRedirection();

// Enable cookie policy - must be before authentication
app.UseCookiePolicy();

// Enable authentication
app.UseAuthentication();

// Enable authorization
app.UseAuthorization();

app.MapControllers();

app.Run();