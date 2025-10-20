using BankingSystem.Models.Entity;
using BankingSystem.Repository.Interface;
using BankingSystem.Repository.Repo;
using BankingSystem.Services.Interface;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BankingSystem.Services
{
    public class AccountService : IAccountService
    {
       private readonly IAccountRepository _accountRepo;
       private readonly IDistributedCache _cache;
       private readonly DistributedCacheEntryOptions _cacheOptions;

        public AccountService(IAccountRepository accountRepo, IDistributedCache cache)
        {
            _accountRepo = accountRepo;
            _cache = cache;
            _cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(10)
            };
        }


        public async Task<Account> CreateAccountAsync(Guid userId, string currency)
        {
            var existingAccounts = await _accountRepo.GetByUserIdAsync(userId);
            if (existingAccounts.Any())
                throw new InvalidOperationException("User already has an account.");

            var acc = new Account
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AccountNumber = GenerateAccountNumber(),
                Currency = currency,
                Balance = 0m,
                CreatedAt = DateTime.UtcNow
            };

            var accountCreated = await _accountRepo.CreateAccountAsync(acc);

            await CacheAccountAsync(accountCreated);

            await _cache.RemoveAsync($"user_account{userId}");

            return accountCreated;
        }

        public async Task<Account?> GetAccountByIdAsync(Guid accountId)
        {
            var cacheKey = $"accountId: {accountId}";
            var cachedAccount = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedAccount))
            {
                return JsonSerializer.Deserialize<Account?>(cachedAccount);
            }

            var account = await _accountRepo.GetByIdAsync(accountId);
            if(account != null)
            {
                await CacheAccountAsync(account);
            }

            return account;
        } 

        public async Task<Account?> GetAccountByNumberAsync(string accountNumber)
        {
            var cacheKey = $"account_number{accountNumber}";
            var cachedAccount = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedAccount))
            {
                return JsonSerializer.Deserialize<Account>(cachedAccount);
            }

            var account = await _accountRepo.GetByAccountNumberAsync(accountNumber);
            if(account != null)
            {
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(account), _cacheOptions);
                await CacheAccountAsync(account);
            }

            return account;
        }

        public async Task<bool> RemoveAccount(Guid accountId)
        {

            var existingAccount = await _accountRepo.GetByIdAsync(accountId);
            if (existingAccount == null)
                throw new Exception("Account not found");

            return await _accountRepo.RemoveAccountAsync(accountId);
        }

        public async Task<IEnumerable<Account>> GetUserAccountsAsync(Guid userId)
        {
            var cacheKey = $"User_account: {userId}";
            var cachedAccounts = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedAccounts))
            {
                return JsonSerializer.Deserialize<IEnumerable<Account>>(cachedAccounts) ?? Enumerable.Empty<Account>();
            }

            var account = await _accountRepo.GetByUserIdAsync(userId);
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(account), _cacheOptions);

            return account;
        }

        public async Task<bool> CloseAccount(Guid accountId)
        {
            var closeAcc = await _accountRepo.CloseAccountAsync(accountId);

            return closeAcc;
        }

        public async Task<decimal> GetBalanceAsync(Guid accountId)
        {
            var cacheKey = $"balance:{accountId}";
            var cachedBalance = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedBalance))
            {
                return decimal.Parse(cachedBalance);
            }

            var account = await _accountRepo.GetByIdAsync(accountId);
            if (account == null)
                throw new Exception("Account Not Found");

            await _cache.SetStringAsync(cacheKey, account.Balance.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) // Shorter cache for balance
            });

            return account.Balance;
        }

        private async Task CacheAccountAsync(Account account)
        {
            var cacheKey = $"acccount:{account.Id}";
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(account), _cacheOptions);
        }
        private string GenerateAccountNumber()
        {
            var random = new Random();
            return random.Next(1000000000, int.MaxValue).ToString();
        }
    }
}
