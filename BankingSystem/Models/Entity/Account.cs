using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingSystem.Models.Entity
{
    public class Account
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string AccountNumber { get; set; } = default!;
        public string Currency { get; set; } = "NGN";
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; }

        public Account()
        {
            Id = Guid.NewGuid();
        }

        //public User User { get; set; } = default!;
        //public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
