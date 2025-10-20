using Xunit;
using Moq;
using BankingSystem.Services;
using BankingSystem.Repository.Interface;
using BankingSystem.Models;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BankingSystem.Tests
{
    public class AccountServiceTests
    {
        private readonly Mock<IAccountRepository> _accountRepoMock;
        private readonly AccountService _accountService;

        public AccountServiceTests()
        {
            _accountRepoMock = new Mock<IAccountRepository>();
            _accountService = new AccountService(_accountRepoMock.Object);
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldReturnNewAccount_WhenUserHasNoExistingAccount()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var account = new Account
            {
                UserId = userId,
                Currency = "USD",
                Balance = 0
            };

            _accountRepoMock.Setup(repo => repo.GetUserAccountsAsync(userId))
                .ReturnsAsync(new List<Account>()); // user has no account

            _accountRepoMock.Setup(repo => repo.CreateAccountAsync(It.IsAny<Account>()))
                .ReturnsAsync((Account acc) => acc);

            // Act
            var result = await _accountService.CreateAccountAsync(account);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
            _accountRepoMock.Verify(repo => repo.CreateAccountAsync(It.IsAny<Account>()), Times.Once);
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldThrow_WhenUserAlreadyHasAccount()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingAccount = new Account { UserId = userId };
            var account = new Account { UserId = userId };

            _accountRepoMock.Setup(repo => repo.GetUserAccountsAsync(userId))
                .ReturnsAsync(new List<Account> { existingAccount });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _accountService.CreateAccountAsync(account));
        }
    }
}
