using CliManager.Infrastructure.Auth;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace CliManager.Infrastructure.Google;

internal sealed class GoogleDriveClientFactory(IGoogleCredentialSource credentialSource)
    : IGoogleDriveClientFactory
{
    private const string ApplicationName = "climanager";

    public async Task<DriveService> CreateDriveServiceAsync(CancellationToken cancellationToken = default)
    {
        var credential = await credentialSource.GetCredentialAsync(cancellationToken);

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName,
        });
    }
}
