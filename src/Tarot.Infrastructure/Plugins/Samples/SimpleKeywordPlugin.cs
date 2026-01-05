using Tarot.Core.Entities;
using Tarot.Core.Interfaces;

namespace Tarot.Infrastructure.Plugins.Samples;

public class SimpleKeywordPlugin : ITarotPlugin
{
    public string Name => "SimpleKeywordAnalyzer";
    public string Version => "1.0.0";
    public string Description => "Analyzes a draw by concatenating card keywords.";

    public Task<string> AnalyzeDrawAsync(IEnumerable<Card> cards, string question)
    {
        var analysis = $"Analysis for question: '{question}'\n\n";
        
        foreach (var card in cards)
        {
            analysis += $"Card: {card.Name} ({card.Suit})\n";
            analysis += $"Meaning: {card.MeaningUpright}\n";
            analysis += $"Keywords: {card.Keywords ?? "N/A"}\n\n";
        }

        analysis += "Summary: The cards suggest a need for reflection based on the keywords above.";
        
        return Task.FromResult(analysis);
    }
}
