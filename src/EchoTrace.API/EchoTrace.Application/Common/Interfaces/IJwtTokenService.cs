using EchoTrace.Domain.Entities;

namespace EchoTrace.Application.Common.Interfaces;

public record TokenPair(string AccessToken, string RefreshToken, int ExpiresIn);

public interface IJwtTokenService
{
    TokenPair GenerateTokens(User user);
    bool ValidateRefreshToken(string tokenHash, out Guid userId);
}
