namespace BankingSystem.Models.Entity
{
    public class User
    {
        public Guid Id { get; set; }

        public string UserName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public DateTime CreatedAt { get; set; }

        //public ICollection<Account> Accounts { get; set; } = new List<Account>();

    }
}
