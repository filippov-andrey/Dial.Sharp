using Microsoft.Extensions.DependencyInjection;

namespace Dial.Sharp.DependencyInjection;

/// <summary>Fluent builder returned by <see cref="DialServiceCollectionExtensions.AddDialClient"/>.</summary>
public interface IDialClientBuilder
{
    /// <summary>The service collection the client is registered in.</summary>
    IServiceCollection Services { get; }
}

internal sealed class DialClientBuilder(IServiceCollection services) : IDialClientBuilder
{
    public IServiceCollection Services => services;
}
