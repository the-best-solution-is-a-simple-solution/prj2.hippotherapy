namespace tests.Models;

/// Class to map responses from login requests to values
public class LoginResponse
{
    public string message { get; set; }
    public string token { get; set; }
    public string userId { get; set; }
}