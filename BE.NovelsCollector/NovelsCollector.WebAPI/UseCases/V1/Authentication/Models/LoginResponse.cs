namespace NovelsCollector.WebAPI.UseCases.V1.Authentication.Models
{
    public class LoginResponse
    {
        public string Message { get; set; }
        public string AccessToken { get; set; }
        public string Email { get; set; }

        public LoginResponse(string message, string accessToken, string email)
        {
            Message = message;
            AccessToken = accessToken;
            Email = email;
        }
    }
}
