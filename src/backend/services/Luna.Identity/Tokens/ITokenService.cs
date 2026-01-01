namespace Luna.Identity.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// JWT token generation and validation.
/// </summary>
public interface ITokenService
{
    string GenerateToken(string userId, string role, TimeSpan? expiresIn = null);
    bool ValidateToken(string token, out string? userId);
}

public sealed class DevJwtTokenService : ITokenService
{
    private readonly IConfiguration _config;

    public DevJwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(string userId, string role, TimeSpan? expiresIn = null)
    {
        var key = _config["Auth:DevJwtKey"] ?? "CHANGE_ME_DEV_ONLY_32+_CHARS_LONG________";
        var issuer = _config["Auth:Issuer"] ?? "luna-dev";
        var audience = _config["Auth:Audience"] ?? "luna-dev";

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(expiresIn ?? TimeSpan.FromHours(8)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool ValidateToken(string token, out string? userId)
    {
        userId = null;

        var key = _config["Auth:DevJwtKey"] ?? "CHANGE_ME_DEV_ONLY_32+_CHARS_LONG________";
        var issuer = _config["Auth:Issuer"] ?? "luna-dev";
        var audience = _config["Auth:Audience"] ?? "luna-dev";

        var tokenHandler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(15),
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, parameters, out _);
            userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return !string.IsNullOrWhiteSpace(userId);
        }
        catch
        {
            return false;
        }
    }
}
