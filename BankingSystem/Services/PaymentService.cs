using BankingSystem.Models.Entity;
using BankingSystem.Repository.Interface;
using BankingSystem.Repository.Repo;
using BankingSystem.Services.Interface;

namespace BankingSystem.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;

        public PaymentService(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task<Payment> CreatePaymentAsync(Guid userId, decimal amount, string provider, string reference)
        {
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = amount,
                Provider = provider,
                Status = "Pending...",
                Reference = reference,
                CreatedAt = DateTime.Now
            };

            return await _paymentRepository.CreatePaymentAsync(payment);
        }
        public async Task<Payment?> VerifyPaymentAsync(Guid paymentId, string reference)
        {
            var verify = await _paymentRepository.GetByIdAsync(paymentId);
            if (verify == null) return null;

            verify.Status = "success";
            await _paymentRepository.UpdateStatusAsync(verify.Id, verify.Status);

            return verify;
        }

        public Task<Payment?> GetPaymentByIdAsync(Guid paymentId) =>
            _paymentRepository.GetByIdAsync(paymentId);

        public async Task<Payment?> GetPaymentByReferenceAsync(string reference) =>
            await _paymentRepository.GetByReferenceAsync(reference);

        public Task<IEnumerable<Payment>> GetUserPaymentsAsync(Guid userId) =>
            _paymentRepository.GetByUserIdAsync(userId);

        public async Task<IEnumerable<Payment>> GetAllPaymentsAsync() => await _paymentRepository.GetAllPaymentsAsync();

    }
}
