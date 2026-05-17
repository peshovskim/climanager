using CliManager.Infrastructure.Auth;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace CliManager.Infrastructure.Google;

public sealed class GoogleDriveClientFactory(GoogleAuthService authService)
    : IGoogleDriveClientFactory
{
    private const string ApplicationName = "climanager";

    public async Task<DriveService> CreateDriveServiceAsync(CancellationToken cancellationToken = default)
    {
        var credential = await authService.GetCredentialAsync(cancellationToken);

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName,
        });
    }
}
