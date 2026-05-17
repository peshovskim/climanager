namespace CliManager.Application.Drive.Interfaces;

public interface IGoogleAuthService
{
    Task EnsureAuthenticatedAsync(CancellationToken cancellationToken = default);
}
