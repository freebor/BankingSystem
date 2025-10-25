using BankingSystem.Models.Dto;
using BankingSystem.Models.Entity;

namespace BankingSystem.Services.Interface
{
    public interface IUserService
    {
        Task<User> CreateUserAsync(RegisterUserDto user);
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllUserAsync();
        Task<bool> ValidateCredentialsAsync(string email, string password);
        string HashPassword(string password);
    }
}
