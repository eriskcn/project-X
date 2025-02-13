using ProjectX.Services;

namespace ProjectX.Middlewares
{
    public class AccessTokenExpiryMiddleware(RequestDelegate next, TokenService tokenService)
    {
        private readonly ITokenService _tokenService = tokenService;
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Cookies.TryGetValue("AccessToken", out var accessToken))
            {
                var isExpired = _tokenService.IsAccessTokenExpired(accessToken);
                if (isExpired)
                {
                    context.Response.StatusCode = StatusCodes.Status419AuthenticationTimeout;
                    await context.Response.WriteAsync("Access token is expired");
                    return;
                }
            }

            await _next(context);
        }
    }
}