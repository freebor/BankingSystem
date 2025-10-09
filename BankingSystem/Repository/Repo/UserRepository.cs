using BankingSystem.Models.Entity;
using BankingSystem.Repository.Interface;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;

namespace BankingSystem.Repository.Repo
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnection _db;

        public UserRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<User> CreateUserAsync(User user)
        {
            user.Id = Guid.NewGuid();
            var sql = @"INSERT INTO Users (Id, UserName, Email, PasswordHash, CreatedAt)
                        VALUES (@Id, @UserName, @Email, @PasswordHash, @CreatedAt)";
            await _db.ExecuteAsync(sql, user);
            return user;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            var sql = "SELECT * FROM Users WHERE Id = @Id";
            return await _db.QuerySingleOrDefaultAsync<User>(sql, new {Id = id}); 
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var sql = $"SELECT [Id],[UserName],[Email],[PasswordHash],[CreatedAt] FROM [BankingDB].[dbo].[Users] WHERE Email = '{email}'";
            var obj = await _db.QuerySingleOrDefaultAsync<User>(sql);
            return obj;
        }

        public async Task<User?> GetByUserNameAsync(string userName)
        {
            var sql = "SELECT * FROM Users WHERE UserName = @UserName";
            return await _db.QuerySingleOrDefaultAsync<User>(sql, new { UserName = userName });
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            var sql = "SELECT * FROM Users";
            return await _db.QueryAsync<User>(sql);
        } 
    }
}
