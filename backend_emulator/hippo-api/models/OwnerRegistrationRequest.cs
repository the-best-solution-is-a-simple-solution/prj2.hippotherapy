using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HippoApi.Models;

public class OwnerRegistrationRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(30, ErrorMessage = "Email must not exceed 30 characters.")]
    public string Email { get; set; }


    [Required(ErrorMessage = "Password is required.")]
    [StringLength(20, MinimumLength = 5, ErrorMessage = "Password must be 5-20 characters long.")]
    // TODO rework this into smaller, more modular regex
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{5,20}$",
        ErrorMessage =
            "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character."
    )]
    public string Password { get; set; }


    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(20, ErrorMessage = "First name must be at most 20 characters.")]
    [RegularExpression("^[a-zA-Z]+$", ErrorMessage = "First name must contain only letters.")]
    public string FName { get; set; }

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(20, ErrorMessage = "Last name must be at most 20 characters.")]
    [RegularExpression("^[a-zA-Z]+$", ErrorMessage = "Last name must contain only letters.")]
    public string LName { get; set; }

    [JsonIgnore] public bool Verified { get; set; }
}