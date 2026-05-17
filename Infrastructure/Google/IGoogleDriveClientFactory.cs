using Google.Apis.Drive.v3;

namespace CliManager.Infrastructure.Google;

public interface IGoogleDriveClientFactory
{
    Task<DriveService> CreateDriveServiceAsync(CancellationToken cancellationToken = default);
}
