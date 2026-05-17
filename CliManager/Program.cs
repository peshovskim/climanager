using CliManager.Application;
using CliManager.Commands;
using CliManager.Composition;
using CliManager.Infrastructure;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = new CommandApp(new TypeRegistrar(builder.Services));

app.Configure(config =>
{
    config.SetApplicationName("climanager");
    config.AddCommand<AuthCommand>("auth")
        .WithDescription("Sign in to Google Drive (OAuth 2.0).");
});

return await app.RunAsync(args);
