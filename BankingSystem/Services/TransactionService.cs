using BankingSystem.Models.Entity;
using BankingSystem.Models.Enums;
using BankingSystem.Repository.Interface;
using BankingSystem.Services.Interface;

namespace BankingSystem.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IAccountRepository _accountRepo;
        private readonly ITransactionRepository _transactionRepo;
        public TransactionService(IAccountRepository accountRepo, ITransactionRepository transactionRepo)
        {
            _accountRepo = accountRepo; 
            _transactionRepo = transactionRepo;
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
                CreatedAt = DateTime.UtcNow
            };

            return await _transactionRepo.CreateTransactionAsync(tx);
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

            return await _transactionRepo.CreateTransactionAsync(tx);
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

            return senderTx;

        }

        public Task<IEnumerable<Transaction>> GetTransactionsByAccountAsync(Guid accountId) =>
             _transactionRepo.GetByAccountIdAsync(accountId);

        public Task<IEnumerable<Transaction>> GetMonthlyStatementAsync(Guid accountId, int year, int month) =>
            _transactionRepo.GetMonthlyStatementAsync(accountId, year, month);
    }
}
