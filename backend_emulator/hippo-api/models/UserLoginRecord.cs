using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;
using HippoApi.Models.enums;

namespace HippoApi.Models;

/// <summary>
/// A record to store logins to the app e.g. for the guest mode
/// </summary>
[FirestoreData]
public class UserLoginRecord
{
    [FirestoreDocumentId] public string LoginRecordId { get; set; }
    private DateTime _loginDate;

    [FirestoreProperty]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; }

    [FirestoreProperty]
    [Required(ErrorMessage = "DateTime is required")]
    public DateTime DateTaken
    {
        get => _loginDate;
        set => _loginDate = DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    [FirestoreProperty]
    [Required(ErrorMessage = "Role is required")]
    public AccountRole Role { get; set; }
}