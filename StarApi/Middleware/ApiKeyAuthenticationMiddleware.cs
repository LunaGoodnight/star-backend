using System.Security.Claims;

namespace StarApi.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check for API key in header
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
        {
            var configuredApiKey = _configuration["ApiKey"];

            if (!string.IsNullOrEmpty(configuredApiKey) && apiKeyHeader == configuredApiKey)
            {
                // Set user as authenticated
                var claims = new[] { new Claim(ClaimTypes.Role, "Admin") };
                var identity = new ClaimsIdentity(claims, "ApiKey");
                context.User = new ClaimsPrincipal(identity);
            }
        }

        await _next(context);
    }
}
