using BankingSystem.Models.Dto;
using BankingSystem.Models.Entity;
using BankingSystem.Repository.Interface;
using BankingSystem.Services.Interface;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BankingSystem.Services
{
    public class UserService : IUserService
    {
       private readonly IUserRepository _userRepo;
       private readonly IDistributedCache _cache;
       private readonly DistributedCacheEntryOptions _cacheOptions;

        public UserService(IUserRepository userRepo, IDistributedCache cache)
        {
            _userRepo = userRepo;
            _cache = cache;
            _cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60),
                SlidingExpiration = TimeSpan.FromMinutes(20),
            };
        }

        public async Task<User> CreateUserAsync(RegisterUserDto request)
        {
            var hashPassword = HashPassword(request.Password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = request.UserName,
                Email = request.Email,
                PasswordHash = hashPassword,
                CreatedAt = DateTime.UtcNow,
            };

            var createdUser =  await _userRepo.CreateUserAsync(user);

            // Cache the newly created user
            await CacheUserAsync(createdUser);

            // Invalidate all users cache
            await _cache.RemoveAsync("all_users");

            return createdUser;
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            var cacheKey = $"user:{userId}";
            var cachedUser = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedUser))
            {
                return JsonSerializer.Deserialize<User>(cachedUser);
            }

            var user = await _userRepo.GetByIdAsync(userId);
            if (user != null)
            {
                await CacheUserAsync(user);
            }

            return user;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            var cacheKey = $"user_email:{email.ToLower()}";
            var cachedUser = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedUser))
            {
                return JsonSerializer.Deserialize<User>(cachedUser);
            }

            var user = await _userRepo.GetByEmailAsync(email);
            if (user != null)
            {
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(user), _cacheOptions);
                await CacheUserAsync(user);
            }

            return user;
        }

        public async Task<IEnumerable<User>> GetAllUserAsync()
        {
            var cacheKey = "all_users";
            var cachedUsers = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedUsers))
            {
                return JsonSerializer.Deserialize<IEnumerable<User>>(cachedUsers) ?? Enumerable.Empty<User>();
            }

            var users = await _userRepo.GetAllAsync();
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(users), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) // Shorter cache for all users
            });

            return users;
        }

        public async Task<bool> ValidateCredentialsAsync(string email, string password)
        {
            var user = await _userRepo.GetByEmailAsync(email);
            if (user == null) return false;

            return user.PasswordHash == HashPassword(password).ToString();
        }

        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private async Task CacheUserAsync(User user)
        {
            var cacheKey = $"user:{user.Id}";
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(user), _cacheOptions);
        }
    }
}
