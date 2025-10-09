using BankingSystem.Models.Entity;
using BankingSystem.Repository.Interface;
using BankingSystem.Services.Interface;

namespace BankingSystem.Services
{
    public class AccountService : IAccountService
    {
       private readonly IAccountRepository _accountRepo;

        public AccountService(IAccountRepository accountRepo)
        {
            _accountRepo = accountRepo;
        }

        public async Task<Account> CreateAccountAsync(Guid userId, string currency)
        {
            var acc = new Account
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AccountNumber = GenerateAccountNumber(),
                Currency = currency,
                Balance = 0m,
                CreatedAt = DateTime.UtcNow
            };

            return await _accountRepo.CreateAccountAsync(acc);
        }

        public Task<Account?> GetAccountByIdAsync(Guid accountId) => 
            _accountRepo.GetByIdAsync(accountId);

        public Task<Account?> GetAccountByNumberAsync(string accountNumber) => 
            _accountRepo.GetByAccountNumberAsync(accountNumber);

        public Task<IEnumerable<Account>> GetUserAccountsAsync(Guid userId) => 
            _accountRepo.GetByUserIdAsync(userId);

        public async Task<decimal> GetBalanceAsync(Guid accountId)
        {
            var account = await _accountRepo.GetByIdAsync(accountId);
            if (account == null)
                throw new Exception("Account Not Found");

            return account.Balance;
        }

        private string GenerateAccountNumber()
        {
            var random = new Random();
            return random.Next(1000000000, int.MaxValue).ToString();
        }
    }
}
