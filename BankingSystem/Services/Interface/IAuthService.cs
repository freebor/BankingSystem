using BankingSystem.Models.Entity;

namespace BankingSystem.Services.Interface
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string email, string password);
        Task<string> GenerateJwtTokenAsync(User user);
    }
}
