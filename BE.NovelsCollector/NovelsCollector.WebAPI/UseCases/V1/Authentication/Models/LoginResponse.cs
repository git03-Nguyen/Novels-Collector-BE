namespace NovelsCollector.WebAPI.UseCases.V1.Authentication.Models
{
    public class LoginResponse
    {
        public string Message { get; set; }
        public string Accesstoken { get; set; }
        public string Email { get; set; }

        public LoginResponse(string message, string accessToken, string email)
        {
            Message = message;
            Accesstoken = accessToken;
            Email = email;
        }
    }
}
