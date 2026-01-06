using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Tarot.Core.Interfaces;

namespace Tarot.Infrastructure.Services;

public class MockPaymentService : IPaymentService
{
    private readonly ILogger<MockPaymentService> _logger;
    private readonly IConfiguration _config;

    public MockPaymentService(ILogger<MockPaymentService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public Task<bool> ProcessPaymentAsync(Guid userId, decimal amount, string currency = "USD")
    {
        _logger.LogInformation("Processing mock payment for User {UserId}: {Amount} {Currency}", userId, amount, currency);
        var failFlag = _config["Payments:MockFail"] ?? Environment.GetEnvironmentVariable("MOCK_PAYMENT_FAIL");
        if (bool.TryParse(failFlag, out var fail) && fail)
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
