using Xunit;
using Moq;
using BankingSystem.Services;
using BankingSystem.Repository.Interface;
using BankingSystem.Models;
using System;
using System.Threading.Tasks;

namespace BankingSystem.Tests
{
    public class TransactionServiceTest
    {
        private readonly Mock<ITransactionRepository> _transactionRepoMock;
        private readonly Mock<IAccountRepository> _accountRepoMock;
        private readonly TransactionService _transactionService;

        public TransactionServiceTest()
        {
            _transactionRepoMock = new Mock<ITransactionRepository>();
            _accountRepoMock = new Mock<IAccountRepository>();
            _transactionService = new TransactionService(_transactionRepoMock.Object, _accountRepoMock.Object);
        }

        [Fact]
        public async Task DepositAsync_ShouldIncreaseBalanceAndSaveTransaction()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var account = new Account { Id = accountId, Balance = 500m };

            _accountRepoMock.Setup(repo => repo.GetAccountByIdAsync(accountId))
                .ReturnsAsync(account);

            _accountRepoMock.Setup(repo => repo.UpdateAccountAsync(It.IsAny<Account>()))
                .Returns(Task.CompletedTask);

            // Act
            await _transactionService.DepositAsync(accountId, 100m);

            // Assert
            _accountRepoMock.Verify(repo => repo.UpdateAccountAsync(It.Is<Account>(a => a.Balance == 600m)), Times.Once);
            _transactionRepoMock.Verify(repo => repo.CreateTransactionAsync(It.IsAny<Transaction>()), Times.Once);
        }
    }
}
