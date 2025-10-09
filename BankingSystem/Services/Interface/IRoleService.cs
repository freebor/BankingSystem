using BankingSystem.Models.Dto;
using BankingSystem.Models.Entity;

namespace BankingSystem.Services.Interface
{
    public interface IRoleService
    {
        Task<Roles> CreateRole(RolesDto roleName);
        Task<Roles?> GetRoleById(Guid id);
        Task<Roles?> GetRoleByName(string name);
        Task<IEnumerable<Roles>> GetAllRoles();
        Task DeleteRole(Guid id);
        Task AssignRoleToUser(Guid userId, Guid roleId);
        Task RemoveRoleFromUser(Guid userId, Guid roleId);
        Task<IEnumerable<Roles>> GetUserRoles(Guid userId);
        Task<bool> UserHasRole(Guid userId, string roleName);
    }
}
