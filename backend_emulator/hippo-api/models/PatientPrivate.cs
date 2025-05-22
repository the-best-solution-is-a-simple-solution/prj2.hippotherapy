using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace HippoApi.Models;

[FirestoreData]
public class PatientPrivate : Patient
{
    // [FirestoreDocumentId] public string? Id { get; set; }

    // Constructor to set defaults
    public PatientPrivate()
    {
        ArchivalDate = DateTime.UtcNow.AddYears(1).ToString("o"); // 1 year from now
        Deleted = false; // Explicit default
    }

    [FirestoreProperty]
    [Required(ErrorMessage = "First name is required")]
    [MinLength(2, ErrorMessage = "First name must be at least 2 characters long")]
    [MaxLength(50, ErrorMessage = "First name must be at most 50 characters long")]
    public string FName { get; set; }

    [FirestoreProperty]
    [Required(ErrorMessage = "Last name is required")]
    [MinLength(2, ErrorMessage = "Last name must be at least 2 characters long")]
    [MaxLength(50, ErrorMessage = "Last name must be at most 50 characters long")]
    public string LName { get; set; }

    [FirestoreProperty]
    [Phone(ErrorMessage = "Invalid phone number")]
    public string Phone { get; set; }

    [FirestoreProperty]
    [Required(ErrorMessage = "Age is required")]
    [Range(1, 100, ErrorMessage = "Age must be between 1 and 100")]
    public long Age { get; set; }

    [FirestoreProperty]
    [Range(20, 300, ErrorMessage = "Weight must be between 20kg and 300kg")]
    public double? Weight { get; set; }

    [FirestoreProperty]
    [Range(50, 300, ErrorMessage = "Age must be between 50cm and 300cm")]
    public double? Height { get; set; }

    [FirestoreProperty]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Must be a valid email address")]
    public string Email { get; set; }

    [FirestoreProperty]
    [Required(ErrorMessage = "Doctor/physician phone number is required")]
    [Phone(ErrorMessage = "Invalid doctor/physician phone number")]
    public string DoctorPhoneNumber { get; set; }

    [FirestoreProperty]
    [CustomValidation(typeof(PatientPrivate), "ValidateGuardianPhoneNumber")]
    public string? GuardianPhoneNumber { get; set; }

    [FirestoreProperty]
    public string? Emoji { get; set; }

    [FirestoreProperty] public bool Deleted { get; set; }

    public static ValidationResult ValidateGuardianPhoneNumber(string guardianPhoneNumber, ValidationContext context)
    {
        PatientPrivate patient = (PatientPrivate)context.ObjectInstance;

        // Treat empty strings as null
        if (string.IsNullOrWhiteSpace(guardianPhoneNumber)) guardianPhoneNumber = null;

        // If the patient is under 18 and GuardianPhoneNumber is null, return an error
        if (patient.Age < 18 && guardianPhoneNumber == null)
            return new ValidationResult("Guardian phone number is required when the age is below 18",
                new[] { "GuardianPhoneNumber" });


        // If there's a phone number, validate its format
        if (guardianPhoneNumber != null)
        {
            PhoneAttribute phoneNumberAttribute = new();
            bool isValidPhoneNumber = phoneNumberAttribute.IsValid(guardianPhoneNumber);

            if (!isValidPhoneNumber)
                return new ValidationResult("Invalid guardian phone number", new[] { "GuardianPhoneNumber" });
        }

        return ValidationResult.Success;
    }
}