namespace NovelsCollector.WebAPI.UseCases.V1.Authentication.Models
{
    public class RegisterResponse
    {
        public string Message { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public RegisterResponse(string message, string email, string role)
        {
            Message = message;
            Email = email;
            Role = role;
        }
    }
}
