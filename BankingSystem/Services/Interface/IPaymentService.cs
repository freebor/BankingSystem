using BankingSystem.Models.Entity;

namespace BankingSystem.Services.Interface
{
    public interface IPaymentService
    {
        Task<Payment> CreatePaymentAsync(Guid userId, decimal amount, string provider, string reference);
        Task<Payment?> VerifyPaymentAsync(Guid paymentId, string reference);
        Task<Payment?> GetPaymentByIdAsync(Guid paymentId);
        Task<Payment?> GetPaymentByReferenceAsync(string reference);
        Task<IEnumerable<Payment>> GetUserPaymentsAsync(Guid userId);
        Task<IEnumerable<Payment>> GetAllPaymentsAsync();
    }
}
