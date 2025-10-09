using System.ComponentModel.DataAnnotations;

namespace BankingSystem.Models.Dto
{
    public class PaymentDto
    {
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Provider { get; set; } = "Paystack";
        public string Reference { get; set; } = string.Empty;

    }
    public class VerifyPaymentRequest
    {
        public string Reference { get; set; } = string.Empty;
    }
}
