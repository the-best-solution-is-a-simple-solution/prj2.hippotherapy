using Google.Cloud.Firestore;
using HippoApi.middleware;
using Microsoft.AspNetCore.Mvc;

namespace HippoApi.Controllers;

[ApiController]
[Route("therapists")]
[ContentOwnerAuthorization]
public class TherapistController : ControllerBase
{
    public const string COLLECTION_NAME = "therapists";
    private readonly FirestoreDb _firestore;

    public TherapistController(FirestoreDb firestore)
    {
        _firestore = firestore;
    }

    /// <summary>
    ///     Method to get all Therapists stored in the Firestore emulator
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetAllTherapists()
    {
        try
        {
            // Access the Firestore collection
            CollectionReference query = _firestore.Collection(COLLECTION_NAME);

            // Retrieve all documents in the collection
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            // Convert documents to a list of therapists
            List<Therapist> therapists = snapshot.Documents
                .Select(doc => doc.ConvertTo<Therapist>())
                .ToList();

            if (therapists.Count == 0) return Ok(new { Message = "No users found." });

            return Ok(therapists);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    // Method to get a therapist by therapistId
    [HttpGet("{therapistId}")]
    public async Task<IActionResult> GetTherapistById(string therapistId, [FromQuery] string? ownerId = null)
    {
        try
        {
            // Log the IDs being fetched
            Console.WriteLine($"Fetching therapist with ID: {therapistId}, OwnerId: {ownerId}");

            // Get ownerId for therapist
            ownerId = await GetOwnerIdForTherapist(therapistId);


            DocumentReference docRef;
            if (string.IsNullOrEmpty(ownerId))
            {
                // Query the top-level therapists collection
                docRef = _firestore.Collection(COLLECTION_NAME).Document(therapistId);
            }
            else
            {
                // Validate that the owner exists
                DocumentReference ownerRef = _firestore.Collection(OwnerController.COLLECTION_NAME).Document(ownerId);
                DocumentSnapshot ownerSnapshot = await ownerRef.GetSnapshotAsync();
                if (!ownerSnapshot.Exists) return NotFound(new { Message = $"Owner with ID {ownerId} not found." });

                // Query the therapist from the owner's subcollection
                docRef = ownerRef.Collection(COLLECTION_NAME).Document(therapistId);
            }

            QuerySnapshot? therapistGroup = await _firestore.CollectionGroup("therapists").GetSnapshotAsync();

            DocumentSnapshot snapshot = null;

            foreach (DocumentSnapshot? therapistDoc in therapistGroup.Documents) // Iterate over documents
                if (therapistDoc.Id == therapistId) // Check if document ID matches
                {
                    // Log the snapshot existence
                    Console.WriteLine($"Document exists: {therapistDoc.Exists}");
                    snapshot = therapistDoc;
                    break; // Exit loop after finding the therapist
                }

            // Check if the document exists
            if (snapshot == null || !snapshot.Exists)
                return NotFound(new
                {
                    Message = string.IsNullOrEmpty(ownerId)
                        ? "Therapist not found in top-level collection."
                        : $"Therapist not found under owner {ownerId}."
                });

            // Convert the Firestore document to a Therapist object
            Therapist therapist = snapshot.ConvertTo<Therapist>();

            return Ok(therapist);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving therapist: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    ///     Gets the ownerId for a therapist
    /// </summary>
    /// <param name="therapistId">therapist to search for the owner of</param>
    /// <returns>the ownerId or null if non-existent</returns>
    public async Task<string> GetOwnerIdForTherapist(string therapistId)
    {
        try
        {
            // Get all owner documents
            QuerySnapshot ownerSnapshot =
                await _firestore.Collection(OwnerController.COLLECTION_NAME).GetSnapshotAsync();

            foreach (DocumentSnapshot ownerDoc in ownerSnapshot.Documents)
            {
                // For each owner, check their therapists subcollection
                CollectionReference therapistsCollection = ownerDoc.Reference.Collection(COLLECTION_NAME);

                // Check if this owner has a document for the given therapistId
                DocumentSnapshot therapistDoc = await therapistsCollection.Document(therapistId).GetSnapshotAsync();

                if (therapistDoc.Exists)
                    // Found the owner who has this therapist
                    return ownerDoc.Id;
            }

            // No owner found with this therapist
            return null;
        }
        catch (Exception ex)
        {
            // Handle exception
            Console.WriteLine($"Error getting owner for therapist: {ex.Message}");
            return null;
        }
    }
}