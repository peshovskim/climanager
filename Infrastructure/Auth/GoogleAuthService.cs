using CliManager.Application.Drive.Interfaces;
using CliManager.Infrastructure.Options;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CliManager.Infrastructure.Auth;

public sealed class GoogleAuthService : IGoogleAuthService
{
    private static readonly string[] Scopes = [DriveService.Scope.Drive];

    private readonly GoogleAuthOptions _options;

    private readonly string _contentRoot;

    private readonly SemaphoreSlim _lock = new(1, 1);

    private UserCredential? _credential;

    public GoogleAuthService(IOptions<GoogleAuthOptions> options, IHostEnvironment hostEnvironment)
    {
        _options = options.Value;
        _contentRoot = hostEnvironment.ContentRootPath;
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

            string clientSecretPath = ResolvePath(_options.ClientSecretPath);

            if (!File.Exists(clientSecretPath))
            {
                throw new FileNotFoundException("OAuth client secret file not found");
            }

            string tokenStorePath = ResolvePath(_options.TokenStorePath);

            Directory.CreateDirectory(tokenStorePath);

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

    private string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }
    
        return Path.GetFullPath(Path.Combine(_contentRoot, path));
    }
}
