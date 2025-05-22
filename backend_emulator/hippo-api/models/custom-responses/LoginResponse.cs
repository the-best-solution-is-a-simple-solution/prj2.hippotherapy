namespace HippoApi.models.custom_responses;

public class LoginResponse
{
    public LoginResponse(string message, string token, string userId)
    {
        Message = message;
        Token = token;
        UserId = userId;
    }

    public string Message { get; set; }
    public string Token { get; set; }
    public string UserId { get; set; }
}