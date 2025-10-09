using BankingSystem.Models.Entity;

namespace BankingSystem.Repository.Interface
{
    public interface IRolesRepository
    {
        Task<Roles> CreateAsync(Roles role);
        Task<Roles?> GetByIdAsync(Guid id);
        Task<Roles?> GetByNameAsync(string name);
        Task<IEnumerable<Roles>> GetAllAsync();
        Task DeleteAsync(Guid id);
        Task AssignRoleToUserAsync(Guid userId, Guid roleId);
        Task RemoveRoleFromUserAsync(Guid userId, Guid roleId);
        Task<IEnumerable<Roles>> GetUserRolesAsync(Guid userId);
        Task<bool> UserHasRoleAsync(Guid userId, string roleName);
    }
}

