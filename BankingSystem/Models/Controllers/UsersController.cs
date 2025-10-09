using BankingSystem.Models.Dto;
using BankingSystem.Models.Entity;
using BankingSystem.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystem.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        
        private readonly IUserService _userservice;

        public UsersController(IUserService userService)
        {
            _userservice = userService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userservice.GetAllUserAsync();
            return Ok(users);
        }

        [HttpGet("id")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> GetUserById(Guid Id)
        {
            var user = await _userservice.GetUserByIdAsync(Id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto user)
        {
            var created = await _userservice.CreateUserAsync(user);
            return Ok(created);
        }
    }
}
