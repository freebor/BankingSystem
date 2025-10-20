using Xunit;
using Moq;
using BankingSystem.Services;
using BankingSystem.Repository.Interface;
using BankingSystem.Models;
using System;
using System.Threading.Tasks;

namespace BankingSystem.Tests
{
    public class PaymentServiceTests
    {
        private readonly Mock<IPaymentRepository> _paymentRepoMock;
        private readonly PaymentService _paymentService;

        public PaymentServiceTests()
        {
            _paymentRepoMock = new Mock<IPaymentRepository>();
            _paymentService = new PaymentService(_paymentRepoMock.Object);
        }

        [Fact]
        public async Task InitiatePaymentAsync_ShouldSavePaymentWithPendingStatus()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var payment = new Payment
            {
                UserId = userId,
                Provider = "Paystack",
                Amount = 1000m,
                Status = "Pending"
            };

            _paymentRepoMock.Setup(repo => repo.CreatePaymentAsync(It.IsAny<Payment>()))
                .ReturnsAsync(payment);

            // Act
            var result = await _paymentService.InitiatePaymentAsync(userId, 1000m, "Paystack");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Pending", result.Status);
            _paymentRepoMock.Verify(repo => repo.CreatePaymentAsync(It.IsAny<Payment>()), Times.Once);
        }
    }
}
