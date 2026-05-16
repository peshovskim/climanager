using CliManager.Application;
using CliManager.Infrastructure;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

using var host = builder.Build();

// Composition root — add CLI commands and MediatR handlers when implementing features.
