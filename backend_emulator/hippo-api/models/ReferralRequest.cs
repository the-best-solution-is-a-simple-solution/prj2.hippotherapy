namespace HippoApi.Models;

/// <summary>
///     Represents the referral model to be stored in the backend (firebase)
/// </summary>
public class ReferralRequest
{
    // OwnerId of the owner who is generating the referral code
    public string OwnerId { get; set; }

    // Email of the therapist that we want to send the referral code to
    public string Email { get; set; }
}