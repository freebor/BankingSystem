using BankingSystem.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystem.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _TrfService;

        public TransactionsController(ITransactionService trfService)
        {
            _TrfService = trfService;
        }

        [HttpPost("deposit")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Deposit(Guid accountId, decimal amount, string reference)
        {
            var tx = await _TrfService.DepositAsync(accountId, amount, reference);
            return Ok(tx);
        }

        [HttpPost("withdraw")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Withdraw(Guid accountId, decimal amount, string reference)
        {
            var tx = await _TrfService.WithdrawAsync(accountId, amount, reference);
            return Ok(tx);
        }

        [HttpPost("transfer")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Transfer(Guid fromAccount, Guid toAccount, decimal amount, string reference)
        {
            var tx = await _TrfService.TransferAsync(fromAccount, toAccount, amount, reference);
            return Ok(tx);
        }

        [HttpGet("account/(accountId)")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> GetAccountTransaction(Guid accountId)
        {
            var transactions = await _TrfService.GetTransactionsByAccountAsync(accountId);
            return Ok(transactions);
        }
    }
}
