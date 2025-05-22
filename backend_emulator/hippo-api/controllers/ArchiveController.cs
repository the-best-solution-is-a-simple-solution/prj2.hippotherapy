using Google.Cloud.Firestore;
using HippoApi.middleware;
using HippoApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace HippoApi.Controllers;

[ApiController]
[Route("[controller]")]
[ContentOwnerAuthorization]
public class ArchiveController : ControllerBase
{
    public FirestoreDb _firestore;

    public ArchiveController(FirestoreDb firestore)
    {
        _firestore = firestore;
    }

    [HttpPut("restore/{patientId}")]
    public async Task<IActionResult> RestorePatient(string patientId)
    {
        try
        {
            DocumentReference? patientRef = _firestore
                .Collection(PatientController.COLLECTION_NAME)
                .Document(patientId);

            DocumentSnapshot? patientSnapshot = await patientRef.GetSnapshotAsync();

            // Check if the document exists
            if (!patientSnapshot.Exists) return BadRequest("Patient was not found.");

            // Check if deleted
            PatientPrivate? patient = patientSnapshot.ConvertTo<PatientPrivate>();
            if (patient.Deleted) return BadRequest("Cannot restore a permanently deleted patient.");

            // Update ArchivalDate to a year from now
            await patientRef.UpdateAsync(new Dictionary<string, object>
            {
                { "ArchivalDate", DateTime.UtcNow.AddYears(1).ToString("o") }
            });

            return Ok(new { Message = "Patient restored successfully." });
        }
        catch (Exception e)
        {
            return BadRequest(new { Message = "An error occurred while restoring the patient.", Error = e.Message });
        }
    }

    [HttpDelete("{patientId}")]
    public async Task<IActionResult> DeletePatient(string patientId)
    {
        try
        {
            DocumentReference? patientRef = _firestore.Collection("patients-private").Document(patientId);
            DocumentSnapshot? patientSnapshot = await patientRef.GetSnapshotAsync();
            if (!patientSnapshot.Exists) return NotFound(new { Message = "Patient not found." });

            // Generate a unique anonymized name
            string anonymizedName = await GenerateUniqueAnonymizedName();

            // Anonymize sensitive fields by setting them to null
            Dictionary<string, object> updates = new()
            {
                { "FName", anonymizedName },
                { "LName", null },
                { "Phone", null },
                { "Email", null },
                { "DoctorPhoneNumber", null },
                { "GuardianPhoneNumber", null },
                { "ArchivalDate", null },
                { "Deleted", true }
            };
            await patientRef.UpdateAsync(updates);

            return NoContent();
        }
        catch (Exception e)
        {
            return StatusCode(500, new { Message = "Error anonymizing patient.", Error = e.Message });
        }
    }

    [HttpGet("therapist/{therapistId}")]
    public async Task<IActionResult> GetArchivedPatientListByTherapistId(string therapistId)
    {
        try
        {
            Console.WriteLine("trying to get archive patients list.");
            Query query = _firestore
                .Collection(PatientController.COLLECTION_NAME)
                .WhereEqualTo("TherapistID", therapistId)
                .WhereLessThan("ArchivalDate", DateTime.UtcNow.ToString("o"));

            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            List<PatientPrivate> archivedPatients = snapshot.Documents
                .Select(doc => doc.ConvertTo<PatientPrivate>())
                .ToList();
            Console.WriteLine($"found {archivedPatients.Count} archived patients.");
            return Ok(archivedPatients);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error fetching archived patients: {e.Message}");
            return BadRequest(
                new { Message = "An error occurred while fetching archived patients.", Error = e.Message });
        }
    }

    // Helper method to generate a unique anonymized name
    private async Task<string> GenerateUniqueAnonymizedName()
    {
        const string prefix = "Anon_";
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        Random random = new();
        string suffix;

        // Generate a 5 character random suffix
        do
        {
            suffix = new string(Enumerable.Repeat(chars, 5)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        } while (await IsNameTaken(prefix + suffix));

        return prefix + suffix;
    }

    // Check if the generated name already exists in Firestore
    private async Task<bool> IsNameTaken(string name)
    {
        Query? query = _firestore.Collection("patients-private")
            .WhereEqualTo("FName", name);
        QuerySnapshot? snapshot = await query.GetSnapshotAsync();
        return snapshot.Count > 0; // True if the name is already used
    }
}