using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace StarApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public record LoginRequest(string Username, string Password);

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username and password are required." });
        }

        var adminUser = _configuration["Admin:Username"] ?? "admin";
        var adminPass = _configuration["Admin:Password"] ?? "changeme";

        if (!string.Equals(request.Username, adminUser, StringComparison.Ordinal) || !string.Equals(request.Password, adminPass, StringComparison.Ordinal))
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var jwtSection = _configuration.GetSection("Jwt");
        var issuer = jwtSection.GetValue<string>("Issuer") ?? "StarApi";
        var audience = jwtSection.GetValue<string>("Audience") ?? "StarApiAudience";
        var key = jwtSection.GetValue<string>("Key");
        if (string.IsNullOrEmpty(key))
        {
            _logger.LogError("JWT Key is not configured");
            return StatusCode(500, new { message = "Server configuration error" });
        }
        var expireMinutes = jwtSection.GetValue<int>("ExpireMinutes", 15);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, adminUser),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(expireMinutes),
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            token = tokenString,
            tokenType = "Bearer",
            expiresAt = token.ValidTo,
            user = new { username = adminUser, role = "Admin" }
        });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userName = User.Identity?.Name ?? "unknown";
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();
        return Ok(new
        {
            authenticated = User.Identity?.IsAuthenticated ?? false,
            user = new { username = userName, roles }
        });
    }
}