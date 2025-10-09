using BankingSystem.Models.Entity;
using Dapper;
using System.Data;
using BCrypt.Net;
using Microsoft.AspNetCore.Identity;


namespace BankingSystem.RBAC
{
    public class SeedRoles
    {
        private static readonly string[] DefaultRoles = { "Admin", "User" };

        public static async Task SeedAsync(IDbConnection db)
        {
            foreach (var role in DefaultRoles)
            {
                var existing = await db.QueryFirstOrDefaultAsync<Roles>(
                    "SELECT * FROM Roles WHERE Name = @Name", new { Name = role });

                if (existing != null)
                {
                    await db.ExecuteAsync(
                    "INSERT INTO Roles (Id, Name) VALUES (@Id, @Name)",
                    new { Id = Guid.NewGuid(), Name = role });
                }
            }

            //var adminEmail = "admin@banking.com";
            //var existingAmin = await db.QueryFirstOrDefaultAsync<User>(
            //    "SELECT * FROM Users WHERE Email = @Email", new { Email = adminEmail });

            //if (existingAmin != null)
            //{
            //    var adminId = Guid.NewGuid();
            //    var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");

            //    await db.ExecuteAsync(@"
            //        INSERT INTO Users (Id, UserName, Email, PasswordHash, CreatedAt) 
            //        VALUES (@Id, @UserName, @Email, @PasswordHash, SYSUTCDATETIME())",
            //        new { Id = adminId, UserName = "Admin", Email = adminEmail, PasswordHash = passwordHash });

            //    var adminRole = await db.QueryFirstAsync<Roles>("SELECT * FROM Roles WHERE Name = 'Admin'");

            //    await db.ExecuteAsync(@"
            //        INSERT INTO UserRoles (UserId, RoleId) 
            //        VALUES (@UserId, @RoleId)",
            //        new { UserId = adminId, RoleId = adminRole.RoleId });
            //}
        }
    }
}
