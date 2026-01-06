using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tarot.Core.Interfaces;
using Tarot.Core.Settings;

namespace Tarot.Infrastructure.Services;

public class AiInterpretationService(IOptions<AppSettings> settings, ILogger<AiInterpretationService> logger, IHttpClientFactory httpClientFactory) : IAiService
{
    private readonly AppSettings _settings = settings.Value;
    private readonly ILogger<AiInterpretationService> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<string> InterpretTarotSpreadAsync(string spreadType, IEnumerable<string> cardNames, string question)
    {
        var provider = _settings.Ai.Provider;
        var cards = string.Join(", ", cardNames);
        _logger.LogInformation("Requesting AI interpretation. Provider: {Provider}, Spread: {Spread}, Cards: {Cards}", provider, spreadType, cards);

        try 
        {
            if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                return await CallOpenAiAsync(spreadType, cards, question);
            }
            else if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
            {
                return await CallGeminiAsync(spreadType, cards, question);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Provider {Provider} failed. Falling back to Mock.", provider);
        }

        return await GetMockInterpretation(spreadType, cards, question);
    }

    private async Task<string> CallOpenAiAsync(string spreadType, string cards, string question)
    {
        var apiKey = _settings.Ai.ApiKey;
        if (string.IsNullOrEmpty(apiKey)) throw new Exception("OpenAI API Key is missing");

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var prompt = $"You are a mystical Tarot Reader. Interpret this spread.\nSpread Type: {spreadType}\nCards: {cards}\nUser's Question: {question}\nProvide a concise, empathetic, and mystical reading.";

        var requestBody = new
        {
            model = _settings.Ai.Model, // e.g., gpt-3.5-turbo
            messages = new[]
            {
                new { role = "system", content = "You are a helpful and mystical Tarot Reader." },
                new { role = "user", content = prompt }
            }
        };

        var response = await client.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "No interpretation.";
    }

    private async Task<string> CallGeminiAsync(string spreadType, string cards, string question)
    {
        var apiKey = _settings.Ai.ApiKey;
        if (string.IsNullOrEmpty(apiKey)) throw new Exception("Gemini API Key is missing");

        var client = _httpClientFactory.CreateClient();
        var prompt = $"You are a mystical Tarot Reader. Interpret this spread.\nSpread Type: {spreadType}\nCards: {cards}\nUser's Question: {question}\nProvide a concise, empathetic, and mystical reading.";

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.Ai.Model}:generateContent?key={apiKey}";
        var response = await client.PostAsJsonAsync(url, requestBody);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        try 
        {
            return json.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "No interpretation.";
        }
        catch
        {
            return "Failed to parse Gemini response.";
        }
    }

    private Task<string> GetMockInterpretation(string spreadType, string cards, string question)
    {
        var response = $"""
        ## Tarot Reading (Simulated)
        
        **Question**: {question}
        **Spread**: {spreadType}
        **Cards**: {cards}
        
        ### The Oracle Speaks
        The cards have aligned to shed light on your path. 
        
        **{cards.Split(',')[0].Trim()}** appears first, suggesting that the core of your situation involves deep reflection. The energy here is potent.
        
        As you move forward, consider the guidance of the other cards. This is a simulated reading. To unlock the true mystical power of AI, please configure the 'Ai' section in your appsettings with a valid API Key for OpenAI or Gemini.
        """;
        
        return Task.FromResult(response);
    }
}
