using System;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Central registry for GDShrapt services.
/// Supports module registration where later modules can override earlier ones.
/// </summary>
public interface IGDServiceRegistry
{
    /// <summary>
    /// Registers a service instance.
    /// Later registrations override earlier ones (Pro overrides Base).
    /// </summary>
    void Register<TService>(TService instance) where TService : class;

    /// <summary>
    /// Registers a service factory for lazy instantiation.
    /// </summary>
    void Register<TService>(Func<IGDServiceRegistry, TService> factory) where TService : class;

    /// <summary>
    /// Gets a service by interface type.
    /// Throws if not registered.
    /// </summary>
    TService GetService<TService>() where TService : class;

    /// <summary>
    /// Gets a service or null if not registered.
    /// </summary>
    TService? TryGetService<TService>() where TService : class;

    /// <summary>
    /// Checks if a service is registered.
    /// </summary>
    bool HasService<TService>() where TService : class;
}
