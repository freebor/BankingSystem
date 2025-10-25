using BankingSystem.Models.Entity;
using BankingSystem.Models.Enums;
using BankingSystem.Repository.Interface;
using BankingSystem.Services;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System.Text;
using System.Text.Json;

namespace BankingSystem.Test
{
    public partial class UnitTest1
    {
        public class TransactionServiceTests
        {
            private readonly Mock<IAccountRepository> _mockAccountRepo;
            private readonly Mock<ITransactionRepository> _mockTransactionRepo;
            private readonly Mock<IDistributedCache> _mockCache;
            private readonly TransactionService _transactionService;

            public TransactionServiceTests()
            {
                _mockAccountRepo = new Mock<IAccountRepository>();
                _mockTransactionRepo = new Mock<ITransactionRepository>();
                _mockCache = new Mock<IDistributedCache>();
                _transactionService = new TransactionService(
                    _mockAccountRepo.Object,
                    _mockTransactionRepo.Object,
                    _mockCache.Object);
            }

            #region DepositAsync Tests

            [Fact]
            public async Task DepositAsync_ShouldIncreaseBalance_AndCreateTransaction()
            {
                // Arrange
                var accountId = Guid.NewGuid();
                var userId = Guid.NewGuid();
                var amount = 100m;
                var reference = "DEP123";
                var account = new Account
                {
                    Id = accountId,
                    UserId = userId,
                    Balance = 500m,
                    AccountNumber = "1234567890"
                };

                _mockAccountRepo.Setup(x => x.GetByIdAsync(accountId))
                    .ReturnsAsync(account);

                _mockAccountRepo.Setup(x => x.UpdateBalanceAsync(accountId, It.IsAny<decimal>()))
                    .Returns(Task.CompletedTask);

                _mockTransactionRepo.Setup(x => x.CreateTransactionAsync(It.IsAny<Transaction>()))
                    .ReturnsAsync((Transaction t) => t);

                // Act
                var result = await _transactionService.DepositAsync(accountId, amount, reference);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(accountId, result.AccountId);
                Assert.Equal(amount, result.Amount);
                Assert.Equal(TransactionType.Deposit, result.TransactionType);
                Assert.Equal(reference, result.Reference);
                _mockAccountRepo.Verify(x => x.UpdateBalanceAsync(accountId, 600m), Times.Once);
                _mockTransactionRepo.Verify(x => x.CreateTransactionAsync(It.IsAny<Transaction>()), Times.Once);
            }

            [Fact]
            public async Task DepositAsync_ShouldThrowException_WhenAmountIsNegative()
            {
                // Arrange
                var accountId = Guid.NewGuid();
                var amount = -100m;
                var reference = "DEP123";

                // Act & Assert
                await Assert.ThrowsAsync<ArgumentException>(() =>
                    _transactionService.DepositAsync(accountId, amount, reference));
            }

            [Fact]
            public async Task DepositAsync_ShouldThrowException_WhenAccountNotFound()
            {
                // Arrange
                var accountId = Guid.NewGuid();
                var amount = 100m;
                var reference = "DEP123";

                _mockAccountRepo.Setup(x => x.GetByIdAsync(accountId))
                    .ReturnsAsync((Account)null);

                // Act & Assert
                await Assert.ThrowsAsync<Exception>(() =>
                    _transactionService.DepositAsync(accountId, amount, reference));
            }

            [Fact]
            public async Task DepositAsync_ShouldInvalidateCache()
            {
                // Arrange
                var accountId = Guid.NewGuid();
                var userId = Guid.NewGuid();
                var amount = 100m;
                var reference = "DEP123";
                var account = new Account
                {
                    Id = accountId,
                    UserId = userId,
                    Balance = 500m
                };

                _mockAccountRepo.Setup(x => x.GetByIdAsync(accountId))
                    .ReturnsAsync(account);

                _mockTransactionRepo.Setup(x => x.CreateTransactionAsync(It.IsAny<Transaction>()))
                    .ReturnsAsync((Transaction t) => t);

                // Act
                await _transactionService.DepositAsync(accountId, amount, reference);

                // Assert
                _mockCache.Verify(x => x.RemoveAsync($"account:{accountId}", It.IsAny<CancellationToken>()), Times.Once);
                _mockCache.Verify(x => x.RemoveAsync($"balance:{accountId}", It.IsAny<CancellationToken>()), Times.Once);
                _mockCache.Verify(x => x.RemoveAsync($"transactions:{accountId}", It.IsAny<CancellationToken>()), Times.Once);
                _mockCache.Verify(x => x.RemoveAsync($"user_accounts:{userId}", It.IsAny<CancellationToken>()), Times.Once);
            }

            #endregion

            #region WithdrawAsync Tests

            [Fact]
            public async Task WithdrawAsync_ShouldDecreaseBalance_AndCreateTransaction()
            {
                // Arrange
                var accountId = Guid.NewGuid();
                var userId = Guid.NewGuid();
                var amount = 100m;
                var reference = "WTH123";
                var account = new Account
                {
                    Id = accountId,
                    UserId = userId,
                    Balance = 500m,
                    AccountNumber = "1234567890"
                };

                _mockAccountRepo.Setup(x => x.GetByIdAsync(accountId))
                    .ReturnsAsync(account);

                _mockAccountRepo.Setup(x => x.UpdateBalanceAsync(accountId, It.IsAny<decimal>()))
                    .Returns(Task.CompletedTask);

                _mockTransactionRepo.Setup(x => x.CreateTransactionAsync(It.IsAny<Transaction>()))
                    .ReturnsAsync((Transaction t) => t);

                // Act
                var result = await _transactionService.WithdrawAsync(accountId, amount, reference);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(accountId, result.AccountId);
                Assert.Equal(amount, result.Amount);
                Assert.Equal(TransactionType.Withdrawal, result.TransactionType);
                Assert.Equal(reference, result.Reference);
                _mockAccountRepo.Verify(x => x.UpdateBalanceAsync(accountId, 400m), Times.Once);
            }

            [Fact]
            public async Task WithdrawAsync_ShouldThrowException_WhenAmountIsZeroOrNegative()
            {
                // Arrange
                var accountId = Guid.NewGuid();
                var amount = 0m;
                var reference = "WTH123";

                // Act & Assert
                await Assert.ThrowsAsync<ArgumentException>(() =>
                    _transactionService.WithdrawAsync(accountId, amount, reference));
            }

            [Fact]
            public async Task WithdrawAsync_ShouldThrowException_WhenInsufficientFunds()
            {
                // Arrange
                var accountId = Guid.NewGuid();
                var amount = 600m;
                var reference = "WTH123";
                var account = new Account
                {
                    Id = accountId,
                    UserId = Guid.NewGuid(),
                    Balance = 500m
                };

                _mockAccountRepo.Setup(x => x.GetByIdAsync(accountId))
                    .ReturnsAsync(account);

                // Act & Assert
                var exception = await Assert.ThrowsAsync<Exception>(() =>
                    _transactionService.WithdrawAsync(accountId, amount, reference));
                Assert.Equal("Insufficient Fund", exception.Message);
            }

            [Fact]
            public async Task WithdrawAsync_ShouldThrowException_WhenAccountNotFound()
            {
                // Arrange
                var accountId = Guid.NewGuid();
                var amount = 100m;
                var reference = "WTH123";

                _mockAccountRepo.Setup(x => x.GetByIdAsync(accountId))
                    .ReturnsAsync((Account)null);

                // Act & Assert
                await Assert.ThrowsAsync<Exception>(() =>
                    _transactionService.WithdrawAsync(accountId, amount, reference));
            }

            #endregion

            #region TransferAsync Tests

            [Fact]
            public async Task TransferAsync_ShouldTransferFunds_AndCreateTransactions()
            {
                // Arrange
                var senderAccountId = Guid.NewGuid();
                var receiverAccountId = Guid.NewGuid();
                var senderUserId = Guid.NewGuid();
                var receiverUserId = Guid.NewGuid();
                var amount = 100m;
                var reference = "TRF123";

                var senderAccount = new Account
                {
                    Id = senderAccountId,
                    UserId = senderUserId,
                    Balance = 500m,
                    AccountNumber = "1234567890"
                };

                var receiverAccount = new Account
                {
                    Id = receiverAccountId,
                    UserId = receiverUserId,
                    Balance = 300m,
                    AccountNumber = "0987654321"
                };

                _mockAccountRepo.Setup(x => x.GetByIdAsync(senderAccountId))
                    .ReturnsAsync(senderAccount);

                _mockAccountRepo.Setup(x => x.GetByIdAsync(receiverAccountId))
                    .ReturnsAsync(receiverAccount);

                _mockAccountRepo.Setup(x => x.UpdateBalanceAsync(It.IsAny<Guid>(), It.IsAny<decimal>()))
                    .Returns(Task.CompletedTask);

                _mockTransactionRepo.Setup(x => x.CreateTransactionAsync(It.IsAny<Transaction>()))
                    .ReturnsAsync((Transaction t) => t);

                // Act
                var result = await _transactionService.TransferAsync(senderAccountId, receiverAccountId, amount, reference);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(senderAccountId, result.AccountId);
                Assert.Equal(amount, result.Amount);
                _mockAccountRepo.Verify(x => x.UpdateBalanceAsync(senderAccountId, 400m), Times.Once);
                _mockAccountRepo.Verify(x => x.UpdateBalanceAsync(receiverAccountId, 400m), Times.Once);
                _mockTransactionRepo.Verify(x => x.CreateTransactionAsync(It.IsAny<Transaction>()), Times.Exactly(2));
            }

            [Fact]
            public async Task TransferAsync_ShouldThrowException_WhenAmountIsZeroOrNegative()
            {
                // Arrange
                var senderAccountId = Guid.NewGuid();
                var receiverAccountId = Guid.NewGuid();
                var amount = 0m;
                var reference = "TRF123";

                // Act & Assert
                await Assert.ThrowsAsync<ArgumentException>(() =>
                    _transactionService.TransferAsync(senderAccountId, receiverAccountId, amount, reference));
            }

            [Fact]
            public async Task TransferAsync_ShouldThrowException_WhenTransferringToSameAccount()
            {
                // Arrange
                var accountId = Guid.NewGuid();
                var amount = 100m;
                var reference = "TRF123";

                // Act & Assert
                var exception = await Assert.ThrowsAsync<Exception>(() =>
                    _transactionService.TransferAsync(accountId, accountId, amount, reference));
                Assert.Equal("cant send money to same accout", exception.Message);
            }

            [Fact]
            public async Task TransferAsync_ShouldThrowException_WhenSenderAccountNotFound()
            {
                // Arrange
                var senderAccountId = Guid.NewGuid();
                var receiverAccountId = Guid.NewGuid();
                var amount = 100m;
                var reference = "TRF123";

                _mockAccountRepo.Setup(x => x.GetByIdAsync(senderAccountId))
                    .ReturnsAsync((Account)null);

                // Act & Assert
                await Assert.ThrowsAsync<Exception>(() =>
                    _transactionService.TransferAsync(senderAccountId, receiverAccountId, amount, reference));
            }

            [Fact]
            public async Task TransferAsync_ShouldThrowException_WhenReceiverAccountNotFound()
            {
                // Arrange
                var senderAccountId = Guid.NewGuid();
                var receiverAccountId = Guid.NewGuid();
                var amount = 100m;
                var reference = "TRF123";

                var senderAccount = new Account
                {
                    Id = senderAccountId,
                    UserId = Guid.NewGuid(),
                    Balance = 500m
                };

                _mockAccountRepo.Setup(x => x.GetByIdAsync(senderAccountId))
                    .ReturnsAsync(senderAccount);

                _mockAccountRepo.Setup(x => x.GetByIdAsync(receiverAccountId))
                    .ReturnsAsync((Account)null);

                // Act & Assert
                await Assert.ThrowsAsync<Exception>(() =>
                    _transactionService.TransferAsync(senderAccountId, receiverAccountId, amount, reference));
            }

            [Fact]
            public async Task TransferAsync_ShouldThrowException_WhenInsufficientFunds()
            {
                // Arrange
                var senderAccountId = Guid.NewGuid();
                var receiverAccountId = Guid.NewGuid();
                var amount = 600m;
                var reference = "TRF123";

                var senderAccount = new Account
                {
                    Id = senderAccountId,
                    UserId = Guid.NewGuid(),
                    Balance = 500m
                };

                var receiverAccount = new Account
                {
                    Id = receiverAccountId,
                    UserId = Guid.NewGuid(),
                    Balance = 300m
                };

                _mockAccountRepo.Setup(x => x.GetByIdAsync(senderAccountId))
                    .ReturnsAsync(senderAccount);

                _mockAccountRepo.Setup(x => x.GetByIdAsync(receiverAccountId))
                    .ReturnsAsync(receiverAccount);

                // Act & Assert
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    _transactionService.TransferAsync(senderAccountId, receiverAccountId, amount, reference));
            }

            #endregion

            #region GetTransactionsByAccountAsync Tests

            [Fact]
            public async Task GetTransactionsByAccountAsync_ShouldReturnCachedTransactions_WhenCacheExists()
            {
                // Arrange
                var accountId = Guid.NewGuid();
                var transactions = new List<Transaction>
            {
                new Transaction { Id = Guid.NewGuid(), AccountId = accountId, Amount = 100m },
                new Transaction { Id = Guid.NewGuid(), AccountId = accountId, Amount = 200m }
            };

                var cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(transactions));
                _mockCache.Setup(x => x.GetAsync($"transactions:{accountId}", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(cachedData);

                // Act
                var result = await _transactionService.GetTransactionsByAccountAsync(accountId);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Count());
                _mockTransactionRepo.Verify(x => x.GetByAccountIdAsync(It.IsAny<Guid>()), Times.Never);
            }

            [Fact]
            public async Task GetTransactionsByAccountAsync_ShouldQueryRepository_WhenCacheIsEmpty()
            {
                // Arrange
                var accountId = Guid.NewGuid();
                var transactions = new List<Transaction>
            {
                new Transaction { Id = Guid.NewGuid(), AccountId = accountId, Amount = 100m },
                new Transaction { Id = Guid.NewGuid(), AccountId = accountId, Amount = 200m }
            };

                _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((byte[])null);

                _mockTransactionRepo.Setup(x => x.GetByAccountIdAsync(accountId))
                    .ReturnsAsync(transactions);

                // Act
                var result = await _transactionService.GetTransactionsByAccountAsync(accountId);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Count());
                _mockTransactionRepo.Verify(x => x.GetByAccountIdAsync(accountId), Times.Once);
            }

            #endregion

            #region GetMonthlyStatementAsync Tests

            [Fact]
            public async Task GetMonthlyStatementAsync_ShouldReturnCachedStatement_WhenCacheExists()
            {
                // Arrange
                var accountId = Guid.NewGuid();
                var year = 2024;
                var month = 10;
                var transactions = new List<Transaction>
            {
                new Transaction { Id = Guid.NewGuid(), AccountId = accountId, Amount = 100m },
                new Transaction { Id = Guid.NewGuid(), AccountId = accountId, Amount = 200m }
            };

                var cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(transactions));
                _mockCache.Setup(x => x.GetAsync($"monthly_statement:{accountId}:{year}:{month}", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(cachedData);

                // Act
                var result = await _transactionService.GetMonthlyStatementAsync(accountId, year, month);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Count());
                _mockTransactionRepo.Verify(x => x.GetMonthlyStatementAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            }

            [Fact]
            public async Task GetMonthlyStatementAsync_ShouldQueryRepository_WhenCacheIsEmpty()
            {
                // Arrange
                var accountId = Guid.NewGuid();
                var year = 2024;
                var month = 10;
                var transactions = new List<Transaction>
            {
                new Transaction { Id = Guid.NewGuid(), AccountId = accountId, Amount = 100m },
                new Transaction { Id = Guid.NewGuid(), AccountId = accountId, Amount = 200m }
            };

                _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((byte[])null);

                _mockTransactionRepo.Setup(x => x.GetMonthlyStatementAsync(accountId, year, month))
                    .ReturnsAsync(transactions);

                // Act
                var result = await _transactionService.GetMonthlyStatementAsync(accountId, year, month);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Count());
                _mockTransactionRepo.Verify(x => x.GetMonthlyStatementAsync(accountId, year, month), Times.Once);
            }

            #endregion
        }
    }
}