using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingSystem.Models.Entity
{
    public class Payment
    {
        [Key]
        public Guid Id { get; set; }   // Primary key

        [Required]
        public Guid UserId { get; set; }   // FK to Users

        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = "Paystack";  // e.g. "Paystack"

        [Required]
        [MaxLength(100)]
        public string Reference { get; set; } = string.Empty; // Payment reference (unique)

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";  // Pending, Success, Failed

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
