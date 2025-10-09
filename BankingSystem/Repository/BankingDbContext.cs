using BankingSystem.Models.Entity;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Repository
{
    public class BankingDbContext : DbContext
    {
        public BankingDbContext(DbContextOptions<BankingDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Transaction> transactions { get; set; }
        public DbSet<Payment> Payments { get; set; }
    }
}
