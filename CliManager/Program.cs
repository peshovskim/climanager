using CliManager.Application;
using CliManager.Commands;
using CliManager.Composition;
using CliManager.Infrastructure;
using CliManager.Infrastructure.Paths;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;

_ = RepositoryPaths.FindRoot();

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddTransient<AuthCommand>();
builder.Services.AddTransient<SyncCommand>();
builder.Services.AddTransient<SearchCommand>();
builder.Services.AddTransient<UploadCommand>();

using var host = builder.Build();

await host.Services.EnsureDatabaseCreatedAsync();

var app = new CommandApp(new TypeRegistrar(host.Services));

app.Configure(config =>
{
    config.SetApplicationName("climanager");
    config.AddCommand<AuthCommand>("auth")
        .WithDescription("Sign in to Google Drive (OAuth 2.0).");

    config.AddCommand<SyncCommand>("sync")
        .WithDescription("Download all files from Google Drive to the local Downloads folder.");

    config.AddCommand<SearchCommand>("search")
        .WithDescription("Search Google Drive by name and show local sync status.");

    config.AddCommand<UploadCommand>("upload")
        .WithDescription("Upload a local file to a folder path in Google Drive (creates folders if needed).");
});

return await app.RunAsync(args);
