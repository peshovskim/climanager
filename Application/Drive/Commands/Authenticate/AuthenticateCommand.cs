using CliManager.Application.Common;
using CliManager.Application.Drive.Interfaces;
using MediatR;
using SharedKernel;
using SharedKernel.Cqrs;

namespace CliManager.Application.Drive.Commands.Authenticate;

public sealed record AuthenticateCommand : ICommand<Result>;

public sealed class AuthenticateCommandHandler(IGoogleAuthService googleAuthService)
    : IRequestHandler<AuthenticateCommand, Result>
{
    public async Task<Result> Handle(AuthenticateCommand command, CancellationToken cancellationToken)
    {
        try
        {
            await googleAuthService.EnsureAuthenticatedAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return DriveCommandResults.FromException(ex);
        }
    }
}
