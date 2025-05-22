using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HippoApi.Models;

public class TherapistRegistrationRequest : Therapist
{
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(20, MinimumLength = 5, ErrorMessage = "Password must be 5-20 characters long.")]
    // TODO rework this into smaller, more modular regex
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{5,20}$",
        ErrorMessage =
            "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character."
    )]
    public string Password { get; set; }

    public string? OwnerId { get; set; }

    public bool? Verified { get; set; }
    public string? Referral { get; set; }


    // exclude TherapistID from validation and serialization
    [JsonIgnore] public override string? TherapistID { get; set; }
}