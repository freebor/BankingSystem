using BankingSystem.Models.Dto;
using BankingSystem.Models.Entity;
using BankingSystem.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystem.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Admin")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roles;

        public RolesController(IRoleService roles)
        {
            _roles = roles;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var role = await _roles.GetAllRoles();
            return Ok(role);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] RolesDto roles)
        {
            var created = await _roles.CreateRole(roles);
            return Ok(created);
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignRoleToUser(Guid userId, Guid roleId)
        {
            await _roles.AssignRoleToUser(userId, roleId);
            return Ok("Role Assigned Successfully");
        }
    }
}
