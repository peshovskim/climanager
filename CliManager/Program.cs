using CliManager.Application;
using CliManager.Commands;
using CliManager.Composition;
using CliManager.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddTransient<AuthCommand>();

using var host = builder.Build();

await host.Services.EnsureDatabaseCreatedAsync();

var app = new CommandApp(new TypeRegistrar(host.Services));

app.Configure(config =>
{
    config.SetApplicationName("climanager");
    config.AddCommand<AuthCommand>("auth")
        .WithDescription("Sign in to Google Drive (OAuth 2.0).");
});

return await app.RunAsync(args);
