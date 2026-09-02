using MediatR;

namespace EchoTrace.Application.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public record LoginResult(string AccessToken, int ExpiresIn, string TokenType);
