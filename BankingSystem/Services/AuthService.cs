using BankingSystem.Models.Entity;
using BankingSystem.Repository.Interface;
using BankingSystem.Services.Interface;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BankingSystem.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IRolesRepository _rolesRepo;
        private readonly IConfiguration _config;
        private readonly IUserService _userService;

        public AuthService(IConfiguration config, IUserRepository userRepo, IRolesRepository roleRepo, IUserService userService)
        {
            _config = config;
            _userRepo = userRepo;
            _rolesRepo = roleRepo;
            _userService = userService;
        }

        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            var user = await _userRepo.GetByEmailAsync(email);

            if (user == null) return null;

            var passwordhash = _userService.HashPassword(password);

            if (user.PasswordHash.ToString() == passwordhash.ToString())
            {
                return user; 
            }

            return null;
        }

        public async Task<string> GenerateJwtTokenAsync(User user)
        {
            var key = Environment.GetEnvironmentVariable("Jwt__SecretKey");
            if(key == null) throw new Exception("JWT Secret Key not configured");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var roles = await _rolesRepo.GetUserRolesAsync(user.Id);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email.ToString()),
                new Claim("username", user.UserName)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
            }

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims  : claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
