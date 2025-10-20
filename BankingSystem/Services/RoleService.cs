using BankingSystem.Models.Dto;
using BankingSystem.Models.Entity;
using BankingSystem.Repository.Interface;
using BankingSystem.Services.Interface;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BankingSystem.Services
{
    public class RoleService : IRoleService
    {
       private readonly IRolesRepository _roleRepo;
       private readonly IDistributedCache _cache;
       private readonly DistributedCacheEntryOptions _cacheOptions;

        public RoleService(IRolesRepository roleRepo, IDistributedCache cache)
        {
            _roleRepo = roleRepo;
            _cache = cache;
            _cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60),
                SlidingExpiration = TimeSpan.FromMinutes(20)
            };
        }

        public async Task<Roles> CreateRole(RolesDto roleName)
        {

            var role = new Roles
            {
                RoleId = Guid.NewGuid(),
                RoleName = roleName.RoleName,
            };

            var createdRole = await _roleRepo.CreateAsync(role);

            // Cache the role
            await CacheRoleAsync(createdRole);

            // Invalidate all roles cache
            await _cache.RemoveAsync("all_roles");

            return createdRole;
        }

        public async Task<Roles?> GetRoleById(Guid Id)
        {
            var cacheKey = $"RoleId{Id}";
            var cacheRole = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cacheRole))
            {
                return JsonSerializer.Deserialize<Roles?>(cacheRole);
            }

            var role = await _roleRepo.GetByIdAsync(Id);
            if (role != null)
            {
                await CacheRoleAsync(role);
            }
            return role;
        }

        public async Task<Roles?> GetRoleByName(string name)
        {
            var cacheKey = $"role_name:{name.ToLower()}";
            var cachedRole = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedRole))
            {
                return JsonSerializer.Deserialize<Roles>(cachedRole);
            }

            var role = await _roleRepo.GetByNameAsync(name);
            if (role != null)
            {
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(role), _cacheOptions);
                await CacheRoleAsync(role);
            }

            return role;
        }

        public async Task<IEnumerable<Roles>> GetAllRoles()
        {
            var cacheKey = $"allRoles";
            var cacheRole = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cacheRole))
            {
                return JsonSerializer.Deserialize<IEnumerable<Roles>>(cacheRole) ?? Enumerable.Empty<Roles>();
            }

            var roles = await _roleRepo.GetAllAsync();
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(roles), _cacheOptions);

            return roles;
        }
        public async Task DeleteRole(Guid id)
        {
            await _roleRepo.DeleteAsync(id);

            // Invalidate caches
            await _cache.RemoveAsync($"role:{id}");
            await _cache.RemoveAsync("all_roles");
        }
        public async Task AssignRoleToUser(Guid userId, Guid roleId)
        {
            await _roleRepo.AssignRoleToUserAsync(userId, roleId);

            // Invalidate user roles cache
            await _cache.RemoveAsync($"user_roles:{userId}");
            await InvalidateUserRoleCheckCache(userId);
        }
        public async Task RemoveRoleFromUser(Guid userId, Guid roleId)
        {
            await _roleRepo.RemoveRoleFromUserAsync(userId, roleId);

            // Invalidate user roles cache
            await _cache.RemoveAsync($"user_roles:{userId}");
            await InvalidateUserRoleCheckCache(userId);
        }
        public async Task<IEnumerable<Roles>> GetUserRoles(Guid userId)
        {
            var cacheKey = $"user_roles:{userId}";
            var cachedRoles = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedRoles))
            {
                return JsonSerializer.Deserialize<IEnumerable<Roles>>(cachedRoles) ?? Enumerable.Empty<Roles>();
            }

            var roles = await _roleRepo.GetUserRolesAsync(userId);
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(roles), _cacheOptions);

            return roles;
        }
        public async Task<bool> UserHasRole(Guid userId, string roleName)
        {
            var cacheKey = $"user_has_role:{userId}:{roleName.ToLower()}";
            var cachedResult = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                return bool.Parse(cachedResult);
            }

            var hasRole = await _roleRepo.UserHasRoleAsync(userId, roleName);
            await _cache.SetStringAsync(cacheKey, hasRole.ToString(), _cacheOptions);

            return hasRole;
        }

        private async Task CacheRoleAsync(Roles role)
        {
            var cacheKey = $"role:{role.RoleId}";
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(role), _cacheOptions);
        }

        private async Task InvalidateUserRoleCheckCache(Guid userId)
        {
            // In a real implementation, you'd want to track all role names to invalidate specific keys
            // For simplicity, this is a placeholder - consider using Redis key patterns with SCAN
            var allRoles = await GetAllRoles();
            foreach (var role in allRoles)
            {
                await _cache.RemoveAsync($"user_has_role:{userId}:{role.RoleName.ToLower()}");
            }
        }
    }
}
