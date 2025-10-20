using BankingSystem.Models.Entity;
using BankingSystem.Models.Enums;
using BankingSystem.Repository.Interface;
using BankingSystem.Services.Interface;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BankingSystem.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IAccountRepository _accountRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions;
        public TransactionService(IAccountRepository accountRepo, ITransactionRepository transactionRepo, IDistributedCache cache)
        {
            _accountRepo = accountRepo; 
            _transactionRepo = transactionRepo;
            _cache = cache;
            _cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20),
                SlidingExpiration = TimeSpan.FromMinutes(30)
            };
        }
        public async Task<Transaction> DepositAsync(Guid accountId, decimal amount, string reference)
        {
            if (amount < 0)
                throw new ArgumentException("Amount should be higher than zero");

            var account = await _accountRepo.GetByIdAsync(accountId)
                ?? throw new Exception("Account not Found");

            account.Balance += amount;
            await _accountRepo.UpdateBalanceAsync(account.Id, account.Balance);

            var tx = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                TransactionType = TransactionType.Deposit,
                Amount = amount,
                Reference = reference,
                Account = account,
                CreatedAt = DateTime.UtcNow
            };

            var createdTransaction = await _transactionRepo.CreateTransactionAsync(tx); 
            
            await InvalidateAccountCaches(accountId, account.UserId);

            return createdTransaction;
        }

        public async Task<Transaction> WithdrawAsync(Guid accountId, decimal amount,string reference)
        {
            if (amount <= 0)
                throw new ArgumentException("Withdrawal amount must be greater than 0");

            var account = await _accountRepo.GetByIdAsync(accountId)
                ?? throw new Exception("Account not Found");

            if (account.Balance < amount)
                throw new Exception("Insufficient Fund");

            account.Balance -= amount;
            await _accountRepo.UpdateBalanceAsync(account.Id, account.Balance);

            var tx = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                TransactionType = TransactionType.Withdrawal,
                Amount = amount,
                Reference = reference,
                CreatedAt = DateTime.UtcNow
            };

            var createdTransaction = await _transactionRepo.CreateTransactionAsync(tx);

            // Invalidate caches affected by this withdrawal
            await InvalidateAccountCaches(accountId, account.UserId);

            return createdTransaction;
        }

        public async Task<Transaction> TransferAsync(Guid fromAccountId, Guid toAccount, decimal amount, string reference)
        {
            if (amount <= 0)
                throw new ArgumentException("Transfer amount must be greater than zero.");

            if (fromAccountId == toAccount)
                throw new Exception("cant send money to same accout");

            var sender = await _accountRepo.GetByIdAsync(fromAccountId)
                ?? throw new Exception("account does not exist");

            var reciever = await _accountRepo.GetByIdAsync(toAccount)
                ?? throw new Exception("account does not exist");

            if (sender.Balance < amount)
                throw new InvalidOperationException("Insufficient fund");

            sender.Balance -= amount;
            reciever.Balance += amount;

            await _accountRepo.UpdateBalanceAsync(sender.Id, sender.Balance);
            await _accountRepo.UpdateBalanceAsync(reciever.Id, reciever.Balance);

            var senderTx = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = sender.Id,
                TransactionType = TransactionType.Deposit,
                Amount = amount,
                Reference = reference,
                CreatedAt = DateTime.UtcNow
            };

            var recieverTx = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = reciever.Id,
                TransactionType = TransactionType.Deposit,
                Amount = amount,
                Reference = reference,
                CreatedAt = DateTime.UtcNow
            };

            await _transactionRepo.CreateTransactionAsync(senderTx);
            await _transactionRepo.CreateTransactionAsync(recieverTx);

            // Invalidate caches for both accounts
            await InvalidateAccountCaches(fromAccountId, sender.UserId);
            await InvalidateAccountCaches(toAccount, reciever.UserId);

            return senderTx;

        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByAccountAsync(Guid accountId)
        {
            var cacheKey = $"transactions:{accountId}";
            var cachedTransactions = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedTransactions))
            {
                return JsonSerializer.Deserialize<IEnumerable<Transaction>>(cachedTransactions) ?? Enumerable.Empty<Transaction>();
            }

            var getTransactions = await _transactionRepo.GetByAccountIdAsync(accountId); ;

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(getTransactions), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) // Shorter cache for transaction lists
            });

            return getTransactions;
        }

        public async Task<IEnumerable<Transaction>> GetMonthlyStatementAsync(Guid accountId, int year, int month)
        {
            var cacheKey = $"monthly_statement:{accountId}:{year}:{month}";
            var cachedStatement = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedStatement))
            {
                return JsonSerializer.Deserialize<IEnumerable<Transaction>>(cachedStatement) ?? Enumerable.Empty<Transaction>();
            }

            var statement = await _transactionRepo.GetMonthlyStatementAsync(accountId, year, month);

            // Cache monthly statements for longer since they don't change after the month ends
            var isCurrentMonth = year == DateTime.UtcNow.Year && month == DateTime.UtcNow.Month;
            var cacheTime = isCurrentMonth ? TimeSpan.FromMinutes(5) : TimeSpan.FromHours(24);

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(statement), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheTime
            });

            return statement;
        }


        private async Task InvalidateAccountCaches(Guid accountId, Guid userId)
        {
            // Invalidate account-related caches
            await _cache.RemoveAsync($"account:{accountId}");
            await _cache.RemoveAsync($"balance:{accountId}");
            await _cache.RemoveAsync($"transactions:{accountId}");
            await _cache.RemoveAsync($"user_accounts:{userId}");

            // Invalidate current month's statement
            var now = DateTime.UtcNow;
            await _cache.RemoveAsync($"monthly_statement:{accountId}:{now.Year}:{now.Month}");
        }
    }
}
