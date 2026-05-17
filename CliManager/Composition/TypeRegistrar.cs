using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace CliManager.Composition;

public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection? _services;
    private readonly IServiceProvider? _provider;

    public TypeRegistrar(IServiceCollection services) => _services = services;

    public TypeRegistrar(IServiceProvider provider) => _provider = provider;

    public ITypeResolver Build() =>
        _provider is not null
            ? new TypeResolver(_provider)
            : new TypeResolver(_services!.BuildServiceProvider());

    public void Register(Type service, Type implementation)
    {
        _services?.AddSingleton(service, implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _services?.AddSingleton(service, implementation);
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        _services?.AddSingleton(service, _ => factory());
    }
}

public sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable
{
    public object? Resolve(Type? type) =>
        type is null ? null : provider.GetService(type);

    public void Dispose() => (provider as IDisposable)?.Dispose();
}
