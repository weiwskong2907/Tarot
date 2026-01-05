using Microsoft.Extensions.Logging;
using Tarot.Core.Interfaces;

namespace Tarot.Infrastructure.Services;

public class MockPaymentService : IPaymentService
{
    private readonly ILogger<MockPaymentService> _logger;

    public MockPaymentService(ILogger<MockPaymentService> logger)
    {
        _logger = logger;
    }

    public Task<bool> ProcessPaymentAsync(Guid userId, decimal amount, string currency = "USD")
    {
        _logger.LogInformation("Processing mock payment for User {UserId}: {Amount} {Currency}", userId, amount, currency);
        // Simulate success
        return Task.FromResult(true);
    }

    public Task<bool> RefundPaymentAsync(Guid transactionId)
    {
        _logger.LogInformation("Refunding transaction {TransactionId}", transactionId);
        return Task.FromResult(true);
    }
}
