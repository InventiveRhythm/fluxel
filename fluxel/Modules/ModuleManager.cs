using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Midori.Logging;
using Midori.Networking;

namespace fluxel.Modules;

public class ModuleManager
{
    private readonly List<Type> modules;
    private readonly List<IModule> built;

    public ModuleManager()
    {
        modules = getTypes().ToList();
        built = new List<IModule>();
    }

    public void RegisterServices(IHostApplicationBuilder builder)
    {
        modules.ForEach(x =>
        {
            var methods = typeof(ServiceCollectionServiceExtensions)
                .GetMethods(BindingFlags.Static | BindingFlags.Public);

            var singleton = methods.Where(m => m.Name == nameof(ServiceCollectionServiceExtensions.AddSingleton))
                                   .Where(m => m.ContainsGenericParameters && m.GetParameters().Length == 1 && m.GetGenericArguments().Length == 2);

            var match = singleton.First().MakeGenericMethod(x, x);
            match.Invoke(null, new object?[] { builder.Services });

            if (x.IsAssignableTo(typeof(IOnlineStateManager)))
                builder.Services.AddSingleton(typeof(IOnlineStateManager), _ => built.FirstOrDefault(m => m.GetType() == x)!);
            if (x.IsAssignableTo(typeof(IMultiRoomManager)))
                builder.Services.AddSingleton(typeof(IMultiRoomManager), _ => built.FirstOrDefault(m => m.GetType() == x)!);
        });
    }

    // kind of a hack? we can't build during api requests because it deadlocks
    public void BuildModules(IServiceProvider services)
        => modules.ForEach(x => built.Add((IModule)services.GetService(x)!));

    public void RegisterControllers(HttpRouter router)
        => modules.ForEach(x => router.RegisterControllersFromAssembly(x.Assembly));

    public void SendMessage(object data) => built.ForEach(x =>
    {
        try
        {
            x.OnMessage(data);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Module '{x.GetType().Name}' failed to handle '{data.GetType().Name}'.");
        }
    });

    private IEnumerable<Type> getTypes()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = assembly.GetName().Name;
            if (name == null) continue;
            if (!name.StartsWith("fluxel", StringComparison.InvariantCultureIgnoreCase)) continue;

            var mod = findModuleType(assembly);
            if (mod != null) yield return mod;
        }

        var path = Path.GetDirectoryName(typeof(ServerHost).Assembly.Location)!;
        string[] files = Directory.GetFiles(path, "fluxel.*.dll");

        foreach (var file in files)
        {
            Type? mod = null;

            try
            {
                var assembly = Assembly.LoadFrom(file);
                mod = findModuleType(assembly);
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Failed to load module {file} from directory!");
            }

            if (mod != null) yield return mod;
        }
    }

    private Type? findModuleType(Assembly assembly)
    {
        string name = assembly.GetName().Name!;

        try
        {
            var types = assembly.GetTypes()
                                .Where(t => t.GetInterfaces().Contains(typeof(IModule)));

            foreach (var type in types)
            {
                Logger.Log($"Found module {type.Name}!");
                return type;
            }
        }
        catch (Exception e)
        {
            Logger.Error(e, $"Failed to load module {name}!");
        }

        return null;
    }
}
