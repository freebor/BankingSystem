using BankingSystem.Models.Dto;
using BankingSystem.Models.Entity;
using BankingSystem.Services;
using BankingSystem.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystem.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _payment;

        public PaymentsController(IPaymentService payment)
        {
            _payment = payment;
        }

        [HttpPost("initiate")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Initiate([FromBody] PaymentDto request)
        {
            try
            {
                var payment = await _payment.CreatePaymentAsync(request.UserId, request.Amount, request.Provider, request.Reference);

                return Ok(new
                {
                    success = true,
                    message = "Payment initiated successfully",
                    data = payment
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to initiate payment",
                    error = ex.Message
                });
            }
        }

        [HttpPost("verify/{paymentId}")]
        [Authorize(Roles = "Admin, User")]
        public async Task<IActionResult> Verify(Guid paymentId, [FromBody] VerifyPaymentRequest request)
        {
            try
            {
                var verify = await _payment.VerifyPaymentAsync(paymentId, request.Reference);

                if (verify == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "payment not found or Reference does not match"
                    });

                }
                return Ok(new
                {
                    success = true,
                    message = "Payment verified successfully",
                    data = verify
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "failed to verify Payment",
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllPayments()
        {
            try
            {
                var payments = await _payment.GetAllPaymentsAsync();

                return Ok(new
                {
                    success = true,
                    message = "Payments retrieved successfully",
                    data = payments
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to retrieve payments",
                    error = ex.Message
                });
            }
        }

        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin, User")]
        public async Task<IActionResult> GetUserPayments(Guid userId)
        {
            try
            {
                var payments = await _payment.GetUserPaymentsAsync(userId);

                return Ok(new
                {
                    success = true,
                    message = "User payments retrieved successfully",
                    data = payments
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to retrieve user payments",
                    error = ex.Message
                });
            }
        }

        [HttpGet("{paymentId}")]
        [Authorize(Roles = "Admin, User")]
        public async Task<IActionResult> GetPaymentById(Guid paymentId)
        {
            try
            {
                var payment = await _payment.GetPaymentByIdAsync(paymentId);

                if (payment == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Payment not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Payment retrieved successfully",
                    data = payment
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to retrieve payment",
                    error = ex.Message
                });
            }
        }
    }
}
