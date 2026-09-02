using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EchoTrace.Application.Common.Interfaces;
using EchoTrace.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EchoTrace.Infrastructure.Services;

public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    private const int AccessTokenLifetimeMinutes = 15;

    public TokenPair GenerateTokens(User user)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("tenantId", user.TenantId.ToString()),
            new("orgId", user.OrgId.ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(AccessTokenLifetimeMinutes);

        var token = new JwtSecurityToken(issuer, audience, claims, expires: expires, signingCredentials: credentials);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        // Refresh-token persistence/rotation (Milestone 1.2 — /auth/refresh) is not yet implemented.
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator());

        return new TokenPair(accessToken, refreshToken, AccessTokenLifetimeMinutes * 60);
    }

    public bool ValidateRefreshToken(string tokenHash, out Guid userId)
    {
        throw new NotImplementedException(
            "Refresh token validation is not implemented yet — no /auth/refresh endpoint exists.");
    }

    private static byte[] RandomNumberGenerator() =>
        System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
}
