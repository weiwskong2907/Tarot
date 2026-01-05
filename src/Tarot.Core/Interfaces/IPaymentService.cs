namespace Tarot.Core.Interfaces;

public interface IPaymentService
{
    Task<bool> ProcessPaymentAsync(Guid userId, decimal amount, string currency = "USD");
    Task<bool> RefundPaymentAsync(Guid transactionId);
}
