using BankingSystem.Models.Dto;
using BankingSystem.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystem.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class AccountsController : ControllerBase
    {

        private readonly IAccountService _accountService;

        public AccountsController(IAccountService accountService)
        {
            _accountService = accountService;
        }


        /// <summary>
        /// Create a new account for a user.
        /// </summary>
        [HttpPost("create")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto currency)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);

            var account = await _accountService.CreateAccountAsync(currency.UserId, currency.Currency);
            return Ok();
        }

        /// <summary>
        /// Get account details by account Id (including transaction history).
        /// </summary>
        
        [HttpGet("{accountId:guid}")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> GetAccountById(Guid accountId)
        {
            var account = await _accountService.GetAccountByIdAsync(accountId);
            if (account == null) return NotFound();
            return Ok(account);
        }

        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task <IActionResult> GetUserAccounts(Guid userId)
        {
            var acc = await _accountService.GetUserAccountsAsync(userId);
            return Ok(acc);
        }
    }
}
