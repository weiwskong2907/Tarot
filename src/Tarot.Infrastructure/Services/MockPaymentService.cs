using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tarot.Core.Interfaces;
using Tarot.Core.Settings;

namespace Tarot.Infrastructure.Services;

public class MockPaymentService : IPaymentService
{
    private readonly ILogger<MockPaymentService> _logger;
    private readonly AppSettings _settings;

    public MockPaymentService(ILogger<MockPaymentService> logger, IOptions<AppSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public Task<bool> ProcessPaymentAsync(Guid userId, decimal amount, string currency = "USD")
    {
        _logger.LogInformation("Processing mock payment for User {UserId}: {Amount} {Currency}", userId, amount, currency);
        
        // Allow environment variable override for testing
        var envFail = Environment.GetEnvironmentVariable("MOCK_PAYMENT_FAIL");
        var shouldFail = (envFail != null && bool.TryParse(envFail, out var f) && f) || _settings.Payment.MockFail;

        if (shouldFail)
        {
            _logger.LogWarning("Mock payment failure simulated for User {UserId}", userId);
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }

    public Task<bool> RefundPaymentAsync(Guid transactionId)
    {
        _logger.LogInformation("Refunding transaction {TransactionId}", transactionId);
        return Task.FromResult(true);
    }
}
