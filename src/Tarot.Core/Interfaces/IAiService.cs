using System.Threading.Tasks;

namespace Tarot.Core.Interfaces;

public interface IAiService
{
    Task<string> InterpretTarotSpreadAsync(string spreadType, IEnumerable<string> cardNames, string question);
}
