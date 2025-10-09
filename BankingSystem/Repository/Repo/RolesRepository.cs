using BankingSystem.Models.Entity;
using BankingSystem.Repository.Interface;
using Dapper;
using System.Data;

namespace BankingSystem.Repository.Repo
{
    public class RolesRepository : IRolesRepository
    {
        private readonly IDbConnection _db;
        public RolesRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<Roles> CreateAsync(Roles role)
        {
            role.RoleId = Guid.NewGuid();
            var sql = "INSERT INTO Roles (Id, RoleName) VALUES (@RoleId, @RoleName)";
            await _db.ExecuteAsync(sql, role);
            return role;
        }
        public async Task<Roles?> GetByIdAsync(Guid id)
        {
            var sql = "SELECT * FROM Roles WHERE Id = @Id";
            return await _db.QueryFirstOrDefaultAsync<Roles>(sql, new { Id = id });
        }

        public async Task<Roles?> GetByNameAsync(string name)
        {
            var sql = "SELECT * FROM Roles WHERE RoleName = @RoleName";
            return await _db.QueryFirstOrDefaultAsync<Roles>(sql, new { Name = name });
        }

        public async Task<IEnumerable<Roles>> GetAllAsync()
        {
            var sql = "SELECT * FROM Roles ORDER BY RoleName";
            return await _db.QueryAsync<Roles>(sql);
        }

        public async Task DeleteAsync(Guid id)
        {
            var sql = "DELETE FROM Roles WHERE Id = @Id";
            await _db.ExecuteAsync(sql, new { Id = id });
        }

        public async Task AssignRoleToUserAsync(Guid userId, Guid roleId)
        {
            var sql = @"INSERT INTO UserRoles (UserId, RoleId) 
                        VALUES (@UserId, @RoleId)";
            await _db.ExecuteAsync(sql, new { UserId = userId, RoleId = roleId });
        }

        public async Task RemoveRoleFromUserAsync(Guid userId, Guid roleId)
        {
            var sql = "DELETE FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId";
            await _db.ExecuteAsync(sql, new { UserId = userId, RoleId = roleId });
        }

        public async Task<IEnumerable<Roles>> GetUserRolesAsync(Guid userId)
        {
            var sql = @"SELECT r.*
                        FROM Roles r
                        INNER JOIN UserRoles ur ON ur.RoleId = r.Id
                        WHERE ur.UserId = @UserId";
            return await _db.QueryAsync<Roles>(sql, new { UserId = userId });
        }

        public async Task<bool> UserHasRoleAsync(Guid userId, string roleName)
        {
            var sql = @"SELECT COUNT(1)
                        FROM Roles r
                        INNER JOIN UserRoles ur ON ur.RoleId = r.Id
                        WHERE ur.UserId = @UserId AND r.Name = @RoleName";
            var count = await _db.ExecuteScalarAsync<int>(sql, new { UserId = userId, RoleName = roleName });
            return count > 0;
        }
    }
}
