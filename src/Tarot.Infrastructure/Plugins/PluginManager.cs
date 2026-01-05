using System.Reflection;
using Tarot.Core.Interfaces;

namespace Tarot.Infrastructure.Plugins;

public class PluginManager : IPluginManager
{
    private readonly List<ITarotPlugin> _plugins = [];

    public void LoadPlugins(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            return;
        }

        var dlls = Directory.GetFiles(path, "*.dll");
        foreach (var dll in dlls)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dll);
                var types = assembly.GetTypes()
                    .Where(t => typeof(ITarotPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var type in types)
                {
                    if (Activator.CreateInstance(type) is ITarotPlugin plugin)
                    {
                        _plugins.Add(plugin);
                    }
                }
            }
            catch (Exception ex)
            {
                // In a real app, log this error
                Console.WriteLine($"Error loading plugin from {dll}: {ex.Message}");
            }
        }
    }

    public IEnumerable<ITarotPlugin> GetPlugins() => _plugins;

    public ITarotPlugin? GetPlugin(string name) => _plugins.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
