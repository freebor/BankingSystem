using System.ComponentModel.DataAnnotations;

namespace BankingSystem.Models.Dto
{
    public class RegisterUserDto
    {

        public string UserName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;

    }

}
