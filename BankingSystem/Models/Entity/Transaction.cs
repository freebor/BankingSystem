using BankingSystem.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace BankingSystem.Models.Entity;


public class Transaction
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();


    [Required]
    public Guid AccountId { get; set; }


    public TransactionType TransactionType { get; set; }


    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public string? Reference { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Account Account { get; set; } = default!;
}