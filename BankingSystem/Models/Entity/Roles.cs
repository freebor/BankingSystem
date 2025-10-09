namespace BankingSystem.Models.Entity
{
    public class Roles
    {
        public Guid RoleId { get; set; } = Guid.NewGuid();
        public string RoleName { get; set; } = string.Empty;
    }
}

