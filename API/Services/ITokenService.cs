using System.Security.Claims;

namespace API.Services;

public interface ITokenService
{
    public string GenerateAccessToken(List<Claim> claims, string secretKey, string issuer, string audience);
    public string GenerateRefreshToken();
    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}