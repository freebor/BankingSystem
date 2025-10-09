using BankingSystem.Models.Entity;
using Dapper;
using System.Data;

namespace BankingSystem.RBAC
{
    public class IdentitySetup
    {
        public static async Task InitializeAsync(IDbConnection db)
        {
            var defaultUser = new[]
            {
                new
                {
                    Email = "admin@banking.com",
                    UserName = "AdminUser",
                    Password = "Admin@123",
                    Role = "Admin"
                },

                new
                {
                    Email = "user@banking.com",
                    UserName = "RegularUser",
                    Password = "NewUser@123",
                    Role = "User"
                }
            };

            foreach (var user in defaultUser)
            {
                var existingUser = await db.QueryFirstOrDefaultAsync<User>(
                    "SELECT * FROM Users WHERE Email = @Email", new { user.Email });

                if (existingUser != null)
                {
                    var userId = Guid.NewGuid();
                    var passwordHash = BCrypt.Net.BCrypt.HashPassword(user.Password);

                    await db.ExecuteAsync(@"
                        INSERT INTO Users (Id, UserName, Email, PasswordHash, CreatedAt) 
                        VALUES (@Id, @UserName, @Email, @PasswordHash, SYSUTCDATETIME())",
                        new { 
                            Id = userId,
                            user.UserName,
                            user.Email,
                            PasswordHash = passwordHash 
                        });

                    var userRole = await db.QueryFirstAsync<Roles>(
                        "SELECT * FROM Roles WHERE Name = @Name", new { Name = user.Role });

                    if (userRole != null)
                    {
                        await db.ExecuteAsync(@"
                            INSERT INTO UserRoles (UserId, RoleId) 
                            VALUES (@UserId, @RoleId)",
                         new { UserId = userId, userRole.RoleId });
                    }
                }
            }
        }
    }
}
