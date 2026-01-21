using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Service registry implementation with support for module-based configuration.
/// Services registered later override earlier registrations.
/// </summary>
public sealed class GDServiceRegistry : IGDServiceRegistry
{
    private readonly Dictionary<Type, object> _services = new();
    private readonly Dictionary<Type, Func<IGDServiceRegistry, object>> _factories = new();

    /// <summary>
    /// Registers a service instance.
    /// </summary>
    public void Register<TService>(TService instance) where TService : class
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        _services[typeof(TService)] = instance;
        // Remove factory if exists - instance takes precedence
        _factories.Remove(typeof(TService));
    }

    /// <summary>
    /// Registers a service factory for lazy instantiation.
    /// </summary>
    public void Register<TService>(Func<IGDServiceRegistry, TService> factory) where TService : class
    {
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        _factories[typeof(TService)] = r => factory(r);
        // Remove existing instance - factory will create new one on demand
        _services.Remove(typeof(TService));
    }

    /// <summary>
    /// Gets a service by interface type.
    /// Throws InvalidOperationException if not registered.
    /// </summary>
    public TService GetService<TService>() where TService : class
    {
        var service = TryGetService<TService>();
        if (service == null)
            throw new InvalidOperationException($"Service {typeof(TService).Name} is not registered.");
        return service;
    }

    /// <summary>
    /// Gets a service or null if not registered.
    /// </summary>
    public TService? TryGetService<TService>() where TService : class
    {
        var type = typeof(TService);

        // Check for existing instance
        if (_services.TryGetValue(type, out var instance))
            return (TService)instance;

        // Check for factory
        if (_factories.TryGetValue(type, out var factory))
        {
            var created = (TService)factory(this);
            _services[type] = created; // Cache for next time
            return created;
        }

        return null;
    }

    /// <summary>
    /// Checks if a service is registered.
    /// </summary>
    public bool HasService<TService>() where TService : class
    {
        var type = typeof(TService);
        return _services.ContainsKey(type) || _factories.ContainsKey(type);
    }

    /// <summary>
    /// Loads modules in priority order and configures their services.
    /// Modules with higher priority load later and can override earlier registrations.
    /// </summary>
    /// <param name="project">The GDScript project context.</param>
    /// <param name="modules">Modules to load.</param>
    public void LoadModules(GDScriptProject project, params IGDModule[] modules)
    {
        if (modules == null || modules.Length == 0)
            return;

        foreach (var module in modules.OrderBy(m => m.Priority))
        {
            module.Configure(this, project);
        }
    }

    /// <summary>
    /// Clears all registered services.
    /// </summary>
    public void Clear()
    {
        _services.Clear();
        _factories.Clear();
    }
}
