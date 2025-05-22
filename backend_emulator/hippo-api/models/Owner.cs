using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace HippoApi.Models;

[FirestoreData]
public class Owner
{
    [FirestoreDocumentId] public string OwnerId { get; set; }

    [FirestoreProperty]
    [Required(ErrorMessage = "Email is required")]
    public string Email { get; set; }

    [FirestoreProperty]
    [Required(ErrorMessage = "First Name is required")]
    public string FName { get; set; }

    [FirestoreProperty]
    [Required(ErrorMessage = "Last Name is required")]
    public string LName { get; set; }
}