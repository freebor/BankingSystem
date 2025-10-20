using BankingSystem.Models.Entity;
using BankingSystem.Repository.Interface;
using BankingSystem.Repository.Repo;
using BankingSystem.Services.Interface;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BankingSystem.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions;

        public PaymentService(IPaymentRepository paymentRepository, IAccountRepository accountRepository, IDistributedCache cache)
        {
            _paymentRepository = paymentRepository;
            _accountRepository = accountRepository;
            _cache = cache;
            _cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20),
                SlidingExpiration = TimeSpan.FromMinutes(5)
            };
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

            var createdPayment = await _paymentRepository.CreatePaymentAsync(payment);

            // Cache the payment
            await CachePaymentAsync(createdPayment);

            // Invalidate user payments cache
            await _cache.RemoveAsync($"user_payments:{userId}");
            await _cache.RemoveAsync("all_payments");

            return createdPayment;
        }

       
        public async Task<Payment?> VerifyPaymentAsync(Guid paymentId, string reference)
        {
            var verify = await _paymentRepository.GetByIdAsync(paymentId);
            if (verify == null) return null;

            verify.Status = "success";
            await _paymentRepository.UpdateStatusAsync(verify.Id, verify.Status);

            await CachePaymentAsync(verify);
            // Invalidate related caches
            await _cache.RemoveAsync($"payment_ref:{reference}");
            await _cache.RemoveAsync($"user_payments:{verify.UserId}");
            await _cache.RemoveAsync("all_payments");

            // Update user's account balance
            var accounts = await _accountRepository.GetByUserIdAsync(verify.UserId);
            var account = accounts?.FirstOrDefault();
            if (account != null)
            {
                await _accountRepository.UpdateBalanceAsync(account.Id, verify.Amount);

                // Invalidate related caches
                await _cache.RemoveAsync($"account:{account.Id}");
                await _cache.RemoveAsync($"balance:{account.Id}");
                await _cache.RemoveAsync($"user_accounts:{verify.UserId}");
                return verify;
            }

            return verify;
        }

        public async Task<Payment?> GetPaymentByIdAsync(Guid paymentId)
        {
            var cacheKey = $"payment:{paymentId}";
            var cachedPayment = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedPayment))
            {
                return JsonSerializer.Deserialize<Payment>(cachedPayment);
            }

            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment != null)
            {
                await CachePaymentAsync(payment);
            }

            return payment;
        }
        public async Task<Payment?> GetPaymentByReferenceAsync(string reference)
        {
            var cacheKey = $"payment_ref:{reference}";
            var cachedPayment = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedPayment))
            {
                return JsonSerializer.Deserialize<Payment>(cachedPayment);
            }

            var payment = await _paymentRepository.GetByReferenceAsync(reference);
            if (payment != null)
            {
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(payment), _cacheOptions);
                await CachePaymentAsync(payment);
            }

            return payment;
        }

        public async Task<IEnumerable<Payment>> GetUserPaymentsAsync(Guid userId)
        {
            var cacheKey = $"user_payments:{userId}";
            var cachedPayments = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedPayments))
            {
                return JsonSerializer.Deserialize<IEnumerable<Payment>>(cachedPayments) ?? Enumerable.Empty<Payment>();
            }

            var payments = await _paymentRepository.GetByUserIdAsync(userId);
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(payments), _cacheOptions);

            return payments;
        }

        public async Task<IEnumerable<Payment>> GetAllPaymentsAsync()
        {
            var cacheKey = "all_payments";
            var cachedPayments = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedPayments))
            {
                return JsonSerializer.Deserialize<IEnumerable<Payment>>(cachedPayments) ?? Enumerable.Empty<Payment>();
            }

            var payments = await _paymentRepository.GetAllPaymentsAsync();
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(payments), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) // Shorter cache for all payments
            });

            return payments;
        }


        private async Task CachePaymentAsync(Payment payment)
        {
            var cacheKey = $"payment:{payment.Id}";
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(payment), _cacheOptions);
        }

    }
}
