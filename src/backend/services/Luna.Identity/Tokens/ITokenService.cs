namespace Luna.Identity.Tokens;

/// <summary>
/// JWT token generation and validation.
/// </summary>
public interface ITokenService
{
    string GenerateToken(string userId, string role, TimeSpan? expiresIn = null);
    bool ValidateToken(string token, out string? userId);
}

public class JwtTokenService : ITokenService
{
    public string GenerateToken(string userId, string role, TimeSpan? expiresIn = null)
    {
        // JWT generation implementation
        throw new NotImplementedException();
    }

    public bool ValidateToken(string token, out string? userId)
    {
        userId = null;
        // Token validation implementation
        throw new NotImplementedException();
    }
}
