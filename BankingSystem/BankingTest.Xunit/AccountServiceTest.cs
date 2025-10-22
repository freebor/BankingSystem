using Xunit;
using Moq;
using BankingSystem.Repository.Interface;
using BankingSystem.Models.Entity;
using BankingSystem.Services.Interface;
using BankingSystem.Repository.Repo;
using BankingSystem.Services;
using Microsoft.Extensions.Caching.Distributed;
using System.Data;
using System.Text;
using System.Text.Json;

namespace BankingSystem.Tests
{
    public class AccountServiceTests
    {
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly AccountService _accountService;

        public AccountServiceTests()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockCache = new Mock<IDistributedCache>();
            _accountService = new AccountService(_mockAccountRepo.Object, _mockCache.Object);
        }

        #region CreateAccountAsync Tests

        [Fact]
        public async Task CreateAccountAsync_ShouldCreateAccount_AndCacheIt()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currency = "USD";

            _mockAccountRepo.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(new List<Account>());

            _mockAccountRepo.Setup(x => x.CreateAccountAsync(It.IsAny<Account>()))
                .ReturnsAsync((Account acc) => acc);

            _mockCache.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockCache.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _accountService.CreateAccountAsync(userId, currency);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(currency, result.Currency);
            Assert.Equal(0m, result.Balance);
            Assert.NotEmpty(result.AccountNumber);
            _mockAccountRepo.Verify(x => x.CreateAccountAsync(It.IsAny<Account>()), Times.Once);
            _mockCache.Verify(x => x.RemoveAsync($"user_account{userId}", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldThrowException_WhenUserAlreadyHasAccount()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currency = "USD";
            var existingAccounts = new List<Account>
            {
                new Account { Id = Guid.NewGuid(), UserId = userId }
            };

            _mockAccountRepo.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(existingAccounts);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _accountService.CreateAccountAsync(userId, currency));
            Assert.Equal("User already has an account.", exception.Message);
        }

        [Fact]
        public async Task CreateAccountAsync_ShouldGenerateUniqueAccountNumber()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currency = "USD";

            _mockAccountRepo.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(new List<Account>());

            _mockAccountRepo.Setup(x => x.CreateAccountAsync(It.IsAny<Account>()))
                .ReturnsAsync((Account acc) => acc);

            // Act
            var result = await _accountService.CreateAccountAsync(userId, currency);

            // Assert
            Assert.NotNull(result.AccountNumber);
            Assert.True(result.AccountNumber.Length >= 10);
        }

        #endregion

        #region GetAccountByIdAsync Tests

        [Fact]
        public async Task GetAccountByIdAsync_ShouldReturnCachedAccount_WhenCacheExists()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var cachedAccount = new Account
            {
                Id = accountId,
                UserId = Guid.NewGuid(),
                AccountNumber = "1234567890",
                Balance = 1000m
            };

            var cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedAccount));
            _mockCache.Setup(x => x.GetAsync($"accountId: {accountId}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _accountService.GetAccountByIdAsync(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(accountId, result.Id);
            _mockAccountRepo.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetAccountByIdAsync_ShouldQueryRepository_WhenCacheIsEmpty()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var account = new Account
            {
                Id = accountId,
                UserId = Guid.NewGuid(),
                AccountNumber = "1234567890",
                Balance = 1000m
            };

            _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            _mockAccountRepo.Setup(x => x.GetByIdAsync(accountId))
                .ReturnsAsync(account);

            // Act
            var result = await _accountService.GetAccountByIdAsync(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(accountId, result.Id);
            _mockAccountRepo.Verify(x => x.GetByIdAsync(accountId), Times.Once);
        }

        [Fact]
        public async Task GetAccountByIdAsync_ShouldReturnNull_WhenAccountNotFound()
        {
            // Arrange
            var accountId = Guid.NewGuid();

            _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            _mockAccountRepo.Setup(x => x.GetByIdAsync(accountId))
                .ReturnsAsync((Account)null);

            // Act
            var result = await _accountService.GetAccountByIdAsync(accountId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetAccountByNumberAsync Tests

        [Fact]
        public async Task GetAccountByNumberAsync_ShouldReturnCachedAccount_WhenCacheExists()
        {
            // Arrange
            var accountNumber = "1234567890";
            var cachedAccount = new Account
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                AccountNumber = accountNumber,
                Balance = 1000m
            };

            var cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedAccount));
            _mockCache.Setup(x => x.GetAsync($"account_number{accountNumber}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _accountService.GetAccountByNumberAsync(accountNumber);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(accountNumber, result.AccountNumber);
            _mockAccountRepo.Verify(x => x.GetByAccountNumberAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetAccountByNumberAsync_ShouldQueryRepository_WhenCacheIsEmpty()
        {
            // Arrange
            var accountNumber = "1234567890";
            var account = new Account
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                AccountNumber = accountNumber,
                Balance = 1000m
            };

            _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            _mockAccountRepo.Setup(x => x.GetByAccountNumberAsync(accountNumber))
                .ReturnsAsync(account);

            // Act
            var result = await _accountService.GetAccountByNumberAsync(accountNumber);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(accountNumber, result.AccountNumber);
            _mockAccountRepo.Verify(x => x.GetByAccountNumberAsync(accountNumber), Times.Once);
        }

        #endregion

        #region RemoveAccount Tests

        [Fact]
        public async Task RemoveAccount_ShouldRemoveAccount_WhenAccountExists()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var account = new Account
            {
                Id = accountId,
                UserId = Guid.NewGuid(),
                AccountNumber = "1234567890"
            };

            _mockAccountRepo.Setup(x => x.GetByIdAsync(accountId))
                .ReturnsAsync(account);

            _mockAccountRepo.Setup(x => x.RemoveAccountAsync(accountId))
                .ReturnsAsync(true);

            // Act
            var result = await _accountService.RemoveAccount(accountId);

            // Assert
            Assert.True(result);
            _mockAccountRepo.Verify(x => x.RemoveAccountAsync(accountId), Times.Once);
        }

        [Fact]
        public async Task RemoveAccount_ShouldThrowException_WhenAccountNotFound()
        {
            // Arrange
            var accountId = Guid.NewGuid();

            _mockAccountRepo.Setup(x => x.GetByIdAsync(accountId))
                .ReturnsAsync((Account)null);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _accountService.RemoveAccount(accountId));
            _mockAccountRepo.Verify(x => x.RemoveAccountAsync(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region GetUserAccountsAsync Tests

        [Fact]
        public async Task GetUserAccountsAsync_ShouldReturnCachedAccounts_WhenCacheExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var accounts = new List<Account>
            {
                new Account { Id = Guid.NewGuid(), UserId = userId, AccountNumber = "1234567890" },
                new Account { Id = Guid.NewGuid(), UserId = userId, AccountNumber = "0987654321" }
            };

            var cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(accounts));
            _mockCache.Setup(x => x.GetAsync($"User_account: {userId}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _accountService.GetUserAccountsAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockAccountRepo.Verify(x => x.GetByUserIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetUserAccountsAsync_ShouldQueryRepository_WhenCacheIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var accounts = new List<Account>
            {
                new Account { Id = Guid.NewGuid(), UserId = userId, AccountNumber = "1234567890" },
                new Account { Id = Guid.NewGuid(), UserId = userId, AccountNumber = "0987654321" }
            };

            _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            _mockAccountRepo.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(accounts);

            // Act
            var result = await _accountService.GetUserAccountsAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockAccountRepo.Verify(x => x.GetByUserIdAsync(userId), Times.Once);
        }

        #endregion

        #region CloseAccount Tests

        [Fact]
        public async Task CloseAccount_ShouldCloseAccount()
        {
            // Arrange
            var accountId = Guid.NewGuid();

            _mockAccountRepo.Setup(x => x.CloseAccountAsync(accountId))
                .ReturnsAsync(true);

            // Act
            var result = await _accountService.CloseAccount(accountId);

            // Assert
            Assert.True(result);
            _mockAccountRepo.Verify(x => x.CloseAccountAsync(accountId), Times.Once);
        }

        [Fact]
        public async Task CloseAccount_ShouldReturnFalse_WhenCloseFails()
        {
            // Arrange
            var accountId = Guid.NewGuid();

            _mockAccountRepo.Setup(x => x.CloseAccountAsync(accountId))
                .ReturnsAsync(false);

            // Act
            var result = await _accountService.CloseAccount(accountId);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetBalanceAsync Tests

        [Fact]
        public async Task GetBalanceAsync_ShouldReturnCachedBalance_WhenCacheExists()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var balance = 1500m;

            var cachedData = Encoding.UTF8.GetBytes(balance.ToString());
            _mockCache.Setup(x => x.GetAsync($"balance:{accountId}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _accountService.GetBalanceAsync(accountId);

            // Assert
            Assert.Equal(balance, result);
            _mockAccountRepo.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetBalanceAsync_ShouldQueryRepository_WhenCacheIsEmpty()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var account = new Account
            {
                Id = accountId,
                UserId = Guid.NewGuid(),
                AccountNumber = "1234567890",
                Balance = 2500m
            };

            _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            _mockAccountRepo.Setup(x => x.GetByIdAsync(accountId))
                .ReturnsAsync(account);

            // Act
            var result = await _accountService.GetBalanceAsync(accountId);

            // Assert
            Assert.Equal(2500m, result);
            _mockAccountRepo.Verify(x => x.GetByIdAsync(accountId), Times.Once);
        }

        [Fact]
        public async Task GetBalanceAsync_ShouldThrowException_WhenAccountNotFound()
        {
            // Arrange
            var accountId = Guid.NewGuid();

            _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            _mockAccountRepo.Setup(x => x.GetByIdAsync(accountId))
                .ReturnsAsync((Account)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _accountService.GetBalanceAsync(accountId));
            Assert.Equal("Account Not Found", exception.Message);
        }

        #endregion
    }

}
