using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace StarApi.Middleware;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check for API key in header
        if (!Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var configuredApiKey = _configuration["ApiKey"];

        if (string.IsNullOrEmpty(configuredApiKey) || apiKeyHeader != configuredApiKey)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));
        }

        // Set user as authenticated
        var claims = new[] { new Claim(ClaimTypes.Role, "Admin") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
