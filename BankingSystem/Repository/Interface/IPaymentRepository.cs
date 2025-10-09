using BankingSystem.Models.Entity;

namespace BankingSystem.Repository.Interface
{
    public interface IPaymentRepository
    {
        Task<Payment> CreatePaymentAsync(Payment payment);
        Task<Payment?> GetByIdAsync(Guid id);
        Task<Payment?> GetByReferenceAsync(string reference);
        Task<IEnumerable<Payment>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<Payment>> GetAllPaymentsAsync();
        Task UpdateStatusAsync(Guid paymentId, string status);
    }
}
