using Tarot.Core.Entities;

namespace Tarot.Core.Interfaces;

public interface ITarotPlugin
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    
    // Core capability: Analyze a draw
    Task<string> AnalyzeDrawAsync(IEnumerable<Card> cards, string question);
}

public interface IPluginManager
{
    void LoadPlugins(string path);
    IEnumerable<ITarotPlugin> GetPlugins();
    ITarotPlugin? GetPlugin(string name);
}
