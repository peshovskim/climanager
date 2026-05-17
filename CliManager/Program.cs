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

using var host = builder.Build();

await host.Services.EnsureDatabaseCreatedAsync();

var app = new CommandApp(new TypeRegistrar(host.Services));

app.Configure(config =>
{
    config.SetApplicationName("climanager");
    config.AddCommand<AuthCommand>("auth")
        .WithDescription("Sign in to Google Drive (OAuth 2.0).");

    config.AddCommand<SyncCommand>("sync")
        .WithDescription("Download all files from Google Drive to the local MyDrive folder.");
});

return await app.RunAsync(args);
