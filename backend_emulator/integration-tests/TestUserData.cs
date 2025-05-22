namespace tests.Models;

/// <summary>
///     A helper class to track essential user information for testing authorization
/// </summary>
public class TestUserData
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string OwnerId { get; set; }
    public required string FName { get; set; }
    public required string LName { get; set; }
    public string? TherapistId { get; set; }
    public string? Token { get; set; }
}