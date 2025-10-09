using BankingSystem.Models.Dto;
using BankingSystem.Models.Entity;
using BankingSystem.Repository.Interface;
using BankingSystem.Services.Interface;
using System.Security.Cryptography;
using System.Text;

namespace BankingSystem.Services
{
    public class UserService : IUserService
    {
       private readonly IUserRepository _userRepo;

        public UserService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<User> CreateUserAsync(RegisterUserDto request)
        {
            var hashPassword = HashPassword(request.Password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = request.UserName,
                Email = request.Email,
                PasswordHash = hashPassword.ToString(),
                CreatedAt = DateTime.UtcNow,
            };

            return await _userRepo.CreateUserAsync(user);
        }

        public Task<User?> GetUserByIdAsync(Guid userId) =>
            _userRepo.GetByIdAsync(userId);

        public Task<User?> GetUserByEmailAsync(string email) => _userRepo.GetByEmailAsync(email);

        public Task<IEnumerable<User>> GetAllUserAsync() => _userRepo.GetAllAsync();

        public async Task<bool> ValidateCredentialsAsync(string email, string password)
        {
            var user = await _userRepo.GetByEmailAsync(email);
            if (user == null) return false;

            return user.PasswordHash == HashPassword(password);
        }

        public object HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}
