using EchoTrace.Application.Common.Interfaces;
using EchoTrace.Domain.Interfaces.Repositories;
using MediatR;

namespace EchoTrace.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IUserRepository userRepo,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await userRepo.GetByEmailAsync(request.Email, ct);
        if (user is null || !user.IsActive || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var tokens = jwtTokenService.GenerateTokens(user);

        return new LoginResult(tokens.AccessToken, tokens.ExpiresIn, "Bearer");
    }
}
