using Google.Apis.Auth.OAuth2;

namespace CliManager.Infrastructure.Auth;

internal interface IGoogleCredentialSource
{
    Task<UserCredential> GetCredentialAsync(CancellationToken cancellationToken = default);
}
