using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace HippoApi.Models;

[FirestoreData]
public class Session
{
    private DateTime _dateTaken;

    public Session(string patientID, string location, DateTime dateTaken)
    {
        Location = location;
        PatientID = patientID;
        DateTaken = dateTaken;
        Validator.ValidateObject(this, new ValidationContext(this), true);
    }

    public Session()
    {
    }

    [FirestoreProperty] public string PatientID { get; set; }

    // auto generate
    [FirestoreDocumentId] public string SessionID { get; set; }

    [FirestoreProperty]
    [Required(ErrorMessage = "Location is required")]
    [MinLength(2, ErrorMessage = "Location must be at least 2 characters long")]
    [MaxLength(2, ErrorMessage = "Location must be at most 2 characters long")]
    public string Location { get; set; }

    [FirestoreProperty]
    public DateTime DateTaken
    {
        get => _dateTaken;
        set => _dateTaken = DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}