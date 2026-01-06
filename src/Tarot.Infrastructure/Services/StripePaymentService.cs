using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tarot.Core.Interfaces;
using Tarot.Core.Settings;

namespace Tarot.Infrastructure.Services;

public class StripePaymentService(HttpClient httpClient, IOptions<AppSettings> settings, ILogger<StripePaymentService> logger) : IPaymentService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly AppSettings _settings = settings.Value;
    private readonly ILogger<StripePaymentService> _logger = logger;

    public async Task<bool> ProcessPaymentAsync(Guid userId, decimal amount, string currency = "USD")
    {
        if (string.IsNullOrEmpty(_settings.Stripe.SecretKey))
        {
            _logger.LogWarning("Stripe SecretKey is missing. Fallback to Mock behavior.");
            
            // Allow environment variable override for testing failure scenarios
            var envFail = Environment.GetEnvironmentVariable("MOCK_PAYMENT_FAIL");
            var shouldFail = (envFail != null && bool.TryParse(envFail, out var f) && f) || _settings.Payment.MockFail;

            if (shouldFail)
            {
                _logger.LogWarning("Mock payment failure simulated for User {UserId}", userId);
                return false;
            }

            return true;
        }

        try
        {
            // Amount in cents
            long amountInCents = (long)(amount * 100);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.stripe.com/v1/payment_intents");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Stripe.SecretKey);

            // Using pm_card_visa for testing immediate confirmation
            var keyValues = new Dictionary<string, string>
            {
                { "amount", amountInCents.ToString() },
                { "currency", currency.ToLower() },
                { "payment_method", "pm_card_visa" }, 
                { "confirm", "true" }, 
                { "metadata[user_id]", userId.ToString() },
                { "return_url", "https://localhost:5001/payment/callback" } // Required for some flows
            };

            var content = new FormUrlEncodedContent(keyValues);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Stripe Payment Failed: {StatusCode} {Body}", response.StatusCode, responseBody);
                return false;
            }
            
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("status", out var statusProp))
            {
                var status = statusProp.GetString();
                if (status == "succeeded")
                {
                    _logger.LogInformation("Stripe Payment Succeeded for User {UserId}", userId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Stripe Payment Status: {Status}", status);
                    // For now, only 'succeeded' is considered true success for backend processing
                    return false;
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe payment");
            return false;
        }
    }

    public Task<bool> RefundPaymentAsync(Guid transactionId)
    {
        // Implementation for refund would go here
        return Task.FromResult(true);
    }
}
