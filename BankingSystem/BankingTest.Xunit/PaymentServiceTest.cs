using Moq;
using BankingSystem.Repository.Interface;
using BankingSystem.Models.Entity;
using BankingSystem.Services;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

namespace BankingXunit.Test
{
    public class PaymentServiceTests
    {
        private readonly Mock<IPaymentRepository> _mockPaymentRepo;
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly PaymentService _paymentService;

        public PaymentServiceTests()
        {
            _mockPaymentRepo = new Mock<IPaymentRepository>();
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockCache = new Mock<IDistributedCache>();
            _paymentService = new PaymentService(
                _mockPaymentRepo.Object,
                _mockAccountRepo.Object,
                _mockCache.Object);
        }

        #region CreatePaymentAsync Tests

        [Fact]
        public async Task CreatePaymentAsync_ShouldCreatePayment_AndCacheIt()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var amount = 1000m;
            var provider = "Paystack";
            var reference = "PAY123";

            var expectedPayment = new Payment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = amount,
                Provider = provider,
                Status = "Pending...",
                Reference = reference,
                CreatedAt = DateTime.Now
            };

            _mockPaymentRepo.Setup(x => x.CreatePaymentAsync(It.IsAny<Payment>()))
                .ReturnsAsync(expectedPayment);

            _mockCache.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockCache.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _paymentService.CreatePaymentAsync(userId, amount, provider, reference);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(amount, result.Amount);
            Assert.Equal(provider, result.Provider);
            Assert.Equal(reference, result.Reference);
            Assert.Equal("Pending...", result.Status);
            _mockPaymentRepo.Verify(x => x.CreatePaymentAsync(It.IsAny<Payment>()), Times.Once);
            _mockCache.Verify(x => x.RemoveAsync($"user_payments:{userId}", It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(x => x.RemoveAsync("all_payments", It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region VerifyPaymentAsync Tests

        [Fact]
        public async Task VerifyPaymentAsync_ShouldVerifyPayment_AndUpdateBalance()
        {
            // Arrange
            var paymentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var accountId = Guid.NewGuid();
            var reference = "PAY123";
            var amount = 1000m;

            var payment = new Payment
            {
                Id = paymentId,
                UserId = userId,
                Amount = amount,
                Status = "Pending...",
                Reference = reference
            };

            var account = new Account
            {
                Id = accountId,
                UserId = userId,
                Balance = 500m,
                AccountNumber = "1234567890"
            };

            _mockPaymentRepo.Setup(x => x.GetByIdAsync(paymentId))
                .ReturnsAsync(payment);

            _mockPaymentRepo.Setup(x => x.UpdateStatusAsync(paymentId, "success"))
                .Returns(Task.CompletedTask);

            _mockAccountRepo.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(new List<Account> { account });

            _mockAccountRepo.Setup(x => x.UpdateBalanceAsync(accountId, amount))
                .Returns(Task.CompletedTask);

            _mockCache.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockCache.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _paymentService.VerifyPaymentAsync(paymentId, reference);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("success", result.Status);
            _mockPaymentRepo.Verify(x => x.UpdateStatusAsync(paymentId, "success"), Times.Once);
            _mockAccountRepo.Verify(x => x.UpdateBalanceAsync(accountId, amount), Times.Once);
            _mockCache.Verify(x => x.RemoveAsync($"payment_ref:{reference}", It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(x => x.RemoveAsync($"user_payments:{userId}", It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(x => x.RemoveAsync($"account:{accountId}", It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(x => x.RemoveAsync($"balance:{accountId}", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task VerifyPaymentAsync_ShouldReturnNull_WhenPaymentNotFound()
        {
            // Arrange
            var paymentId = Guid.NewGuid();
            var reference = "PAY123";

            _mockPaymentRepo.Setup(x => x.GetByIdAsync(paymentId))
                .ReturnsAsync((Payment)null);

            // Act
            var result = await _paymentService.VerifyPaymentAsync(paymentId, reference);

            // Assert
            Assert.Null(result);
            _mockPaymentRepo.Verify(x => x.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task VerifyPaymentAsync_ShouldVerifyPayment_WithoutUpdatingBalance_WhenNoAccountFound()
        {
            // Arrange
            var paymentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var reference = "PAY123";

            var payment = new Payment
            {
                Id = paymentId,
                UserId = userId,
                Amount = 1000m,
                Status = "Pending...",
                Reference = reference
            };

            _mockPaymentRepo.Setup(x => x.GetByIdAsync(paymentId))
                .ReturnsAsync(payment);

            _mockPaymentRepo.Setup(x => x.UpdateStatusAsync(paymentId, "success"))
                .Returns(Task.CompletedTask);

            _mockAccountRepo.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync((IEnumerable<Account>)null);

            _mockCache.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockCache.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _paymentService.VerifyPaymentAsync(paymentId, reference);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("success", result.Status);
            _mockAccountRepo.Verify(x => x.UpdateBalanceAsync(It.IsAny<Guid>(), It.IsAny<decimal>()), Times.Never);
        }

        #endregion

        #region GetPaymentByIdAsync Tests

        [Fact]
        public async Task GetPaymentByIdAsync_ShouldReturnCachedPayment_WhenCacheExists()
        {
            // Arrange
            var paymentId = Guid.NewGuid();
            var cachedPayment = new Payment
            {
                Id = paymentId,
                UserId = Guid.NewGuid(),
                Amount = 1000m,
                Provider = "Paystack",
                Status = "success"
            };

            var cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedPayment));
            _mockCache.Setup(x => x.GetAsync($"payment:{paymentId}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _paymentService.GetPaymentByIdAsync(paymentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(paymentId, result.Id);
            _mockPaymentRepo.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetPaymentByIdAsync_ShouldQueryRepository_WhenCacheIsEmpty()
        {
            // Arrange
            var paymentId = Guid.NewGuid();
            var payment = new Payment
            {
                Id = paymentId,
                UserId = Guid.NewGuid(),
                Amount = 1000m,
                Provider = "Paystack",
                Status = "success"
            };

            _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            _mockPaymentRepo.Setup(x => x.GetByIdAsync(paymentId))
                .ReturnsAsync(payment);

            // Act
            var result = await _paymentService.GetPaymentByIdAsync(paymentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(paymentId, result.Id);
            _mockPaymentRepo.Verify(x => x.GetByIdAsync(paymentId), Times.Once);
        }

        [Fact]
        public async Task GetPaymentByIdAsync_ShouldReturnNull_WhenPaymentNotFound()
        {
            // Arrange
            var paymentId = Guid.NewGuid();

            _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            _mockPaymentRepo.Setup(x => x.GetByIdAsync(paymentId))
                .ReturnsAsync((Payment)null);

            // Act
            var result = await _paymentService.GetPaymentByIdAsync(paymentId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetPaymentByReferenceAsync Tests

        [Fact]
        public async Task GetPaymentByReferenceAsync_ShouldReturnCachedPayment_WhenCacheExists()
        {
            // Arrange
            var reference = "PAY123";
            var cachedPayment = new Payment
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Amount = 1000m,
                Reference = reference
            };

            var cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedPayment));
            _mockCache.Setup(x => x.GetAsync($"payment_ref:{reference}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _paymentService.GetPaymentByReferenceAsync(reference);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(reference, result.Reference);
            _mockPaymentRepo.Verify(x => x.GetByReferenceAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetPaymentByReferenceAsync_ShouldQueryRepository_WhenCacheIsEmpty()
        {
            // Arrange
            var reference = "PAY123";
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Amount = 1000m,
                Reference = reference
            };

            _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            _mockPaymentRepo.Setup(x => x.GetByReferenceAsync(reference))
                .ReturnsAsync(payment);

            // Act
            var result = await _paymentService.GetPaymentByReferenceAsync(reference);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(reference, result.Reference);
            _mockPaymentRepo.Verify(x => x.GetByReferenceAsync(reference), Times.Once);
        }

        #endregion

        #region GetUserPaymentsAsync Tests

        [Fact]
        public async Task GetUserPaymentsAsync_ShouldReturnCachedPayments_WhenCacheExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var payments = new List<Payment>
            {
                new Payment { Id = Guid.NewGuid(), UserId = userId, Amount = 1000m },
                new Payment { Id = Guid.NewGuid(), UserId = userId, Amount = 2000m }
            };

            var cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payments));
            _mockCache.Setup(x => x.GetAsync($"user_payments:{userId}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _paymentService.GetUserPaymentsAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockPaymentRepo.Verify(x => x.GetByUserIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetUserPaymentsAsync_ShouldQueryRepository_WhenCacheIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var payments = new List<Payment>
            {
                new Payment { Id = Guid.NewGuid(), UserId = userId, Amount = 1000m },
                new Payment { Id = Guid.NewGuid(), UserId = userId, Amount = 2000m }
            };

            _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            _mockPaymentRepo.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(payments);

            // Act
            var result = await _paymentService.GetUserPaymentsAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockPaymentRepo.Verify(x => x.GetByUserIdAsync(userId), Times.Once);
        }

        #endregion

        #region GetAllPaymentsAsync Tests

        [Fact]
        public async Task GetAllPaymentsAsync_ShouldReturnCachedPayments_WhenCacheExists()
        {
            // Arrange
            var payments = new List<Payment>
            {
                new Payment { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Amount = 1000m },
                new Payment { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Amount = 2000m }
            };

            var cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payments));
            _mockCache.Setup(x => x.GetAsync("all_payments", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _paymentService.GetAllPaymentsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockPaymentRepo.Verify(x => x.GetAllPaymentsAsync(), Times.Never);
        }

        [Fact]
        public async Task GetAllPaymentsAsync_ShouldQueryRepository_WhenCacheIsEmpty()
        {
            // Arrange
            var payments = new List<Payment>
            {
                new Payment { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Amount = 1000m },
                new Payment { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Amount = 2000m }
            };

            _mockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            _mockPaymentRepo.Setup(x => x.GetAllPaymentsAsync())
                .ReturnsAsync(payments);

            // Act
            var result = await _paymentService.GetAllPaymentsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockPaymentRepo.Verify(x => x.GetAllPaymentsAsync(), Times.Once);
        }

        #endregion
    }


}
