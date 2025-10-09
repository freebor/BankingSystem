using BankingSystem.Models.Entity;

namespace BankingSystem.Repository.Interface
{
    public interface IUserRepository
    {
        Task<User> CreateUserAsync (User user);
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync (string email);
        Task<User?> GetByUserNameAsync (string userName);
        Task<IEnumerable<User>> GetAllAsync ();
    }
}
