using System;
using System.Collections.Generic;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> Services = new();

    public static void Register<TService>(TService service) where TService : class
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        Services[typeof(TService)] = service;
    }

    public static TService Resolve<TService>() where TService : class
    {
        if (TryResolve<TService>(out var service))
        {
            return service;
        }

        throw new InvalidOperationException($"Service of type '{typeof(TService).Name}' is not registered.");
    }

    public static bool TryResolve<TService>(out TService service) where TService : class
    {
        if (Services.TryGetValue(typeof(TService), out var instance))
        {
            service = instance as TService;
            return service != null;
        }

        service = null;
        return false;
    }

    public static void Clear()
    {
        Services.Clear();
    }
}
