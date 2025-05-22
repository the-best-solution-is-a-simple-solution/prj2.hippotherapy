using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

[FirestoreData]
public class Therapist
{
    [FirestoreDocumentId] public virtual string TherapistID { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(30, ErrorMessage = "Email must not exceed 30 characters.")]
    [FirestoreProperty]
    public string Email { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(20, ErrorMessage = "First name must be at most 20 characters.")]
    [RegularExpression("^[a-zA-Z]+$", ErrorMessage = "First name must contain only letters.")]
    [FirestoreProperty]
    public string FName { get; set; }

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(20, ErrorMessage = "Last name must be at most 20 characters.")]
    [RegularExpression("^[a-zA-Z]+$", ErrorMessage = "Last name must contain only letters.")]
    [FirestoreProperty]
    public string LName { get; set; }

    [MaxLength(20, ErrorMessage = "Country must not exceed 20 characters.")]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Country must contain only letters and spaces.")]
    [FirestoreProperty]
    public string? Country { get; set; }

    [MaxLength(20, ErrorMessage = "City must not exceed 20 characters.")]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "City must contain only letters and spaces.")]
    [FirestoreProperty]
    public string? City { get; set; }

    [MaxLength(20, ErrorMessage = "Street must not exceed 20 characters.")]
    [RegularExpression(@"^[A-Za-zÀ-ÿ0-9\s.,\-'/]+$",
        ErrorMessage = "Street must contain only letters, numbers, spaces, and common punctuation.")]
    [FirestoreProperty]
    public string? Street { get; set; }

    [RegularExpression(@"^[A-Za -z]\d[A-Za-z][ -]?\d[A-Za-z]\d$",
        ErrorMessage = "Postal code should be in the form L#L #L#.")]
    [FirestoreProperty]
    public string? PostalCode { get; set; }

    [RegularExpression(@"^\+?(\d{1,3})?[\s-]?(\(?\d{3}\)?[\s-]?)\d{3}[\s-]?\d{4}$",
        ErrorMessage = "Please enter a valid phone number (e.g., +1-555-555-5555).")]
    [FirestoreProperty]
    public string? Phone { get; set; }

    [MaxLength(25, ErrorMessage = "Profession must not exceed 25 characters.")]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Profession must contain only letters and spaces.")]
    [FirestoreProperty]
    public string? Profession { get; set; }

    [MaxLength(25, ErrorMessage = "Major must not exceed 25 characters.")]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Major must contain only letters and spaces.")]
    [FirestoreProperty]
    public string? Major { get; set; }

    [Range(0, 100, ErrorMessage = "Years of experience must be an integer between 0 and 100.")]
    [FirestoreProperty]
    public int? YearsExperienceInHippotherapy { get; set; }
}