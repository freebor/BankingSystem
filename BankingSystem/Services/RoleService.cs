using BankingSystem.Models.Dto;
using BankingSystem.Models.Entity;
using BankingSystem.Repository.Interface;
using BankingSystem.Services.Interface;

namespace BankingSystem.Services
{
    public class RoleService : IRoleService
    {
       private readonly IRolesRepository _roleRepo;

        public RoleService(IRolesRepository roleRepo)
        {
            _roleRepo = roleRepo;
        }

        public async Task<Roles> CreateRole(RolesDto roleName)
        {

            var role = new Roles
            {
                RoleId = Guid.NewGuid(),
                RoleName = roleName.RoleName,
            };

            return await _roleRepo.CreateAsync(role);
        }

        public async Task<Roles?> GetRoleById(Guid Id) =>
            await _roleRepo.GetByIdAsync(Id);

        public async Task<Roles?> GetRoleByName(string name) => await _roleRepo.GetByNameAsync(name);

        public async Task<IEnumerable<Roles>> GetAllRoles() => await _roleRepo.GetAllAsync();
        public async Task DeleteRole(Guid id) => await _roleRepo.DeleteAsync(id);
        public async Task AssignRoleToUser(Guid userId, Guid roleId) => await _roleRepo.AssignRoleToUserAsync(userId, roleId);
        public async Task RemoveRoleFromUser(Guid userId, Guid roleId) => await _roleRepo.RemoveRoleFromUserAsync(userId, roleId);
        public async Task<IEnumerable<Roles>> GetUserRoles(Guid userId) => await _roleRepo.GetUserRolesAsync(userId);
        public async Task<bool> UserHasRole(Guid userId, string roleName) => await _roleRepo.UserHasRoleAsync(userId, roleName);
    }
}
