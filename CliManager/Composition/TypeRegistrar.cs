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
            ? new TypeResolver(_provider, ownsProvider: false)
            : new TypeResolver(_services!.BuildServiceProvider(), ownsProvider: true);

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

public sealed class TypeResolver : ITypeResolver, IDisposable
{
    private readonly IServiceProvider _provider;
    private readonly bool _ownsProvider;

    public TypeResolver(IServiceProvider provider, bool ownsProvider)
    {
        _provider = provider;
        _ownsProvider = ownsProvider;
    }

    public object? Resolve(Type? type) =>
        type is null ? null : _provider.GetService(type);

    public void Dispose()
    {
        if (_ownsProvider && _provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
