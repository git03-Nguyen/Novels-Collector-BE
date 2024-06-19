using System.ComponentModel.DataAnnotations;

namespace NovelsCollector.WebAPI.UseCases.V1.Authentication.Models
{
    public class RegisterRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;
    }
}
