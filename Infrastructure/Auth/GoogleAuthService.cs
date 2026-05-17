using CliManager.Application.Drive.Interfaces;
using CliManager.Infrastructure.Options;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Options;

namespace CliManager.Infrastructure.Auth;

public sealed class GoogleAuthService : IGoogleAuthService, IGoogleCredentialSource
{
    private static readonly string[] Scopes = [DriveService.Scope.Drive];

    private readonly GoogleAuthOptions _options;

    private readonly SemaphoreSlim _lock = new(1, 1);

    private UserCredential? _credential;

    public GoogleAuthService(IOptions<GoogleAuthOptions> options)
    {
        _options = options.Value;
    }

    public async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        await GetCredentialAsync(cancellationToken);
    }

    public async Task<UserCredential> GetCredentialAsync(CancellationToken cancellationToken = default)
    {
        if (_credential is not null)
        {
            return _credential;
        }

        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (_credential is not null)
            {
                return _credential;
            }

            string clientSecretPath = _options.ClientSecretPath;
            string tokenStorePath = _options.TokenStorePath;

            await using var stream = new FileStream(
                clientSecretPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            var clientSecrets = await GoogleClientSecrets.FromStreamAsync(stream, cancellationToken);

            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets.Secrets,
                Scopes,
                "user",
                cancellationToken,
                new FileDataStore(tokenStorePath, fullPath: true));

            return _credential;
        }
        finally
        {
            _lock.Release();
        }
    }

}
