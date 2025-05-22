using Google.Cloud.Firestore;
using HippoApi.middleware;
using HippoApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace HippoApi.Controllers;

[ApiController]
[Route("owners")]
[ContentOwnerAuthorization]
public class OwnerController : ControllerBase
{
    public const string COLLECTION_NAME = "owners";
    private readonly FirestoreDb _firestore;

    public OwnerController(FirestoreDb firestore)
    {
        _firestore = firestore;
    }

    [HttpGet("{ownerId}")]
    public async Task<IActionResult> GetOwnerById([FromRoute] string ownerId)
    {
        try
        {
            // Log the ID being fetched
            Console.WriteLine($"Fetching owner with ID: {ownerId}");

            // Access the Firestore collection and specific document by ID
            DocumentReference? docRef = _firestore.Collection(COLLECTION_NAME).Document(ownerId);
            DocumentSnapshot? snapshot = await docRef.GetSnapshotAsync();

            // Log the snapshot existence
            Console.WriteLine($"Document exists: {snapshot.Exists}");

            // Check if the document exists
            if (!snapshot.Exists) return NotFound("Owner not found.");

            // Convert the Firestore document to a Owner object
            Owner? owner = snapshot.ConvertTo<Owner>();

            return Ok(owner);
        }
        catch (Exception ex)
        {
            return BadRequest($"An error occured while fetching owner\n{ex.Message}");
        }
    }

    [HttpGet("{ownerId}/therapists")]
    public async Task<IActionResult> GetTherapistsByOwnerId([FromRoute] string ownerId)
    {
        try
        {
            // Log the ID being fetched
            Console.WriteLine($"Fetching therapists with Owner Id of: {ownerId}");

            // Access the Firestore collection and specific document by ID
            DocumentReference? ownerDocRef = _firestore.Collection(COLLECTION_NAME).Document(ownerId);
            DocumentSnapshot? ownerSnapshot = await ownerDocRef.GetSnapshotAsync();
            Owner? owner = ownerSnapshot.ConvertTo<Owner>();

            // Log the snapshot existence
            Console.WriteLine($"Owner Document exists: {ownerSnapshot.Exists}");

            // Check if the document exists
            if (!ownerSnapshot.Exists) return NotFound(new { Message = "Owner not found." });

            List<Therapist> therapists =
                (await ownerDocRef.Collection(TherapistController.COLLECTION_NAME).GetSnapshotAsync())
                .Select(doc => doc.ConvertTo<Therapist>()).ToList();

            if (!therapists.Any())
            {
                Console.WriteLine($"No Therapists found under Owner {owner.FName} {owner.LName} ({ownerId})");
                return NoContent();
            }

            return Ok(therapists);
        }
        catch (Exception ex)
        {
            return BadRequest($"An error occured while fetching therapists\n{ex.Message}");
        }
    }


    /// <summary>
    ///     This method will move a list of patients from one therapist to another.
    ///     This requires that the owner have authority over both therapists, and the
    ///     old therapist containing all patients, if any data is invalid, no operation will
    ///     be carried out.
    /// </summary>
    /// <param name="ownerId">The owner who has both therapists under his domain</param>
    /// <param name="oldTherapistId">The therapist we are moving from</param>
    /// <param name="newTherapistId">The therapist to move to</param>
    /// <param name="lstPatientIDs">
    ///     List of anonymous patient ids whose therapist reference
    ///     we need to replace to the new therapist
    /// </param>
    /// <returns>
    ///     An OK status code with a success method and the list of patients under the new therapist,
    ///     a BadRequest code if one of the ID's is invalid/does not exist,
    ///     an Unauthorized code if attempting to access data not in its proper location
    /// </returns>
    [HttpPut("{ownerId}/{oldTherapistId}/{newTherapistId}")]
    public async Task<IActionResult> ReassignPatientsToDifferentTherapist
    ([FromRoute] string ownerId, [FromRoute] string oldTherapistId,
        [FromRoute] string newTherapistId, [FromBody] List<string> lstPatientIDs)
    {
        if (lstPatientIDs.Count == 0) return BadRequest("No Patient IDs were provided.");

        // check that the owner id is valid
        DocumentSnapshot? ownerRef = await _firestore.Collection(COLLECTION_NAME)
            .Document(ownerId)
            .GetSnapshotAsync();

        if (ownerRef == null || ownerRef.Exists != true) return NotFound("Owner not found.");

        // Check that the owner has both the therapists passed in inside their domain
        DocumentSnapshot? oldTherapistRef = await ownerRef.Reference.Collection(TherapistController.COLLECTION_NAME)
            .Document(oldTherapistId)
            .GetSnapshotAsync();

        DocumentSnapshot? newTherapistRef = await ownerRef.Reference.Collection(TherapistController.COLLECTION_NAME)
            .Document(newTherapistId)
            .GetSnapshotAsync();

        if (oldTherapistRef == null || oldTherapistRef.Exists == false ||
            newTherapistRef == null || newTherapistRef.Exists == false)
            return Unauthorized($"{ownerId} does not have permission to " +
                                $"reassign between {oldTherapistId} and {newTherapistId}");

        // check that the old therapist has all the patients in his care and they exist
        QuerySnapshot? oldPatientsReference = await _firestore
            .Collection(PatientController.COLLECTION_NAME)
            .WhereIn(FieldPath.DocumentId, lstPatientIDs)
            .GetSnapshotAsync();

        // Check that all the patients submitted actually exist and are valid
        if (oldPatientsReference.Count != lstPatientIDs.Count)
            return BadRequest($"Not all patients submitted were found. " +
                              $"Submitted {lstPatientIDs.Count} and found {oldPatientsReference.Count}");

        foreach (DocumentSnapshot? patient in oldPatientsReference)
            if (!patient.Exists)
                return NotFound("PatientID does not exist.");

        if (oldPatientsReference.Documents.Any(x =>
                x.ConvertTo<Patient>().TherapistID != oldTherapistId))
            return Unauthorized($"{oldTherapistId} does not have all patients in their care");

        // check that new therapist does not have them him his
        if (oldPatientsReference.Documents.Any(x =>
                x.ConvertTo<Patient>().TherapistID == newTherapistId))
            return Unauthorized($"{newTherapistId} has some or all patients already in their care");

        // now it will try to attempt the reassignment operation
        List<DocumentSnapshot> listOfPatientsToUpdate = oldPatientsReference.Documents.ToList();

        foreach (DocumentSnapshot? p in listOfPatientsToUpdate)
            await p.Reference.UpdateAsync("TherapistID", newTherapistId);

        // check new therapist has all the passed in patients
        QuerySnapshot? newPatientsRef = await _firestore
            .Collection(PatientController.COLLECTION_NAME)
            .WhereEqualTo("TherapistID", newTherapistId)
            .GetSnapshotAsync();

        foreach (DocumentSnapshot? movedPatients in newPatientsRef.Documents)
        {
            List<string> failedMoves = new();

            if (movedPatients.GetValue<string>("TherapistID") != newTherapistId)
                failedMoves.Add(movedPatients.ConvertTo<Patient>().Id);

            if (failedMoves.Count > 0)
            {
                string failures = "";
                failedMoves.ForEach(x => failures += x + ", ");
                return Problem(title: $"There was an error and {failedMoves.Count} patient(s) did not get moved",
                    detail: failures, statusCode: 500);
            }
        }

        // only then make the OK to return
        Therapist? oldT = oldTherapistRef?.ConvertTo<Therapist>();
        Therapist? newT = newTherapistRef.ConvertTo<Therapist>();

        return Ok($"Successfully transferred {listOfPatientsToUpdate.Count} patient(s) " +
                  $"from {oldT?.FName ?? ""} {oldT?.LName ?? ""} to {newT.FName} {newT.LName}");
    }
}