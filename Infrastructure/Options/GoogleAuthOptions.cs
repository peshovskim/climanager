namespace CliManager.Infrastructure.Options;

public sealed class GoogleAuthOptions
{
    public const string SectionName = "GoogleAuth";

    public string ClientSecretPath { get; set; } = "client_secret.json";

    public string TokenStorePath { get; set; } = ".climanager/tokens";
}
