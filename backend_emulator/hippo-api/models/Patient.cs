using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace HippoApi.Models;

[FirestoreData]
public class Patient
{
    //Empty constructor required by Firestore

    [FirestoreDocumentId] public string? Id { get; set; }


    [FirestoreProperty]
    [DefaultValue("Default ID")]
    public string? TherapistID { get; set; }

    [FirestoreProperty]
    [Required(ErrorMessage = "Condition is required")]
    [MinLength(2, ErrorMessage = "Condition must be at least 2 characters long")]
    [MaxLength(50, ErrorMessage = "Condition must be at most 50 characters long")]
    public string Condition { get; set; }

    [FirestoreProperty] public string? ArchivalDate { get; set; }
}