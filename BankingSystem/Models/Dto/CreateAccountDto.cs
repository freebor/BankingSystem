using System.ComponentModel.DataAnnotations;

namespace BankingSystem.Models.Dto
{
    public class CreateAccountDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = "NGN";
    }

}
