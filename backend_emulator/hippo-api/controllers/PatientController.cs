using Google.Cloud.Firestore;
using HippoApi.middleware;
using HippoApi.Models;
using HippoApi.Models.custom_responses;
using Microsoft.AspNetCore.Mvc;

namespace HippoApi.Controllers;

[ApiController]
[Route("[controller]")]
[ContentOwnerAuthorization]
public class PatientController : ControllerBase
{
    public const string COLLECTION_NAME = "patients-private";
    private static FirestoreDb _firestore;
    private static AuthController _authController;

    // instantiate controller and db
    public PatientController(FirestoreDb firestore)
    {
        _firestore = firestore;
        _authController = new AuthController(_firestore);
    }

    /// <summary>
    ///     Returns a list of patients assigned to the passed in therapistId
    /// </summary>
    /// <param name="therapistId">Id of the therapist whose patients should be returned</param>
    /// <returns>List of Patients assigned</returns>
    [HttpGet("therapist/{therapistId}")]
    public async Task<IActionResult> GetPatientListByTherapistId(string therapistId)
    {
        try
        {
            Query? query = _firestore
                .Collection(COLLECTION_NAME)
                .WhereEqualTo("TherapistID", therapistId)
                .WhereGreaterThan("ArchivalDate", DateTime.UtcNow.ToString("o")) // Active patients
                .WhereEqualTo("Deleted", false); // Exclude deleted patients

            QuerySnapshot? snapshot = await query.GetSnapshotAsync();
            List<PatientPrivate> archivedPatients = snapshot.Documents
                .Select(doc => doc.ConvertTo<PatientPrivate>())
                .ToList();
            return Ok(archivedPatients);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error fetching archived patients: {e.Message}");
            return BadRequest(
                new { Message = "An error occurred while fetching archived patients.", Error = e.Message });
        }
    }


    /// <summary>
    ///     http://localhost:5000/patient/submitpatient
    ///     Creates a patient object in the database and performs validation on it
    /// </summary>
    /// <param name="patient">PatientPrivate JSON</param>
    /// <returns>
    ///     Ok status code for a created patient + that patient's ID, otherwise a 400 -- bad request
    ///     entity, with a message
    /// </returns>
    [HttpPost]
    [Route("submit-patient/{therapistId}")]
    public async Task<IActionResult> CreatePatient([FromRoute] string therapistId, [FromBody] PatientPrivate patient)
    {
        try
        {
            // Set to use the therapistId from to route to secure access
            patient.TherapistID = therapistId;

            CollectionReference? patientRef = _firestore.Collection("patients-private");

            // Set archival date to 1 year from now
            patient.ArchivalDate = DateTime.UtcNow.AddYears(1).ToString("o");
            patient.Deleted = false;
            DocumentReference? patientAdded = await patientRef.AddAsync(patient);
            string? id = patientAdded.Id;

            if (string.IsNullOrEmpty(id)) throw new ApplicationException("Patient not added successfully");

            // I want to return an OK status, and return the patientID
            CreatePatientResponse response = new("Patient Created Successfully", id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            //if there is an error, return a 400 "BadRequest" to the frontend
            return BadRequest(new
                { Message = "An error occurred while processing your request", Error = ex.Message }
            );
        }
    }


    /// <summary>
    ///     Grab a single patient with matching ID
    ///     http://localhost:5000/patient/{id}
    /// </summary>
    /// <param name="id">FirestoreID of Patient to grab URL param</param>
    /// <returns>The patient with a 200 OK status, or 404 not found</returns>
    [HttpGet]
    [Route("{patientId}")]
    public async Task<IActionResult> GetPatientById([FromRoute] string patientId)
    {
        try
        {
            DocumentReference? patientRef = _firestore.Collection("patients-private").Document(patientId);
            DocumentSnapshot? snapshot = await patientRef.GetSnapshotAsync();
            if (!snapshot.Exists) return NotFound(new { Message = "Patient not found." });

            PatientPrivate? patient = snapshot.ConvertTo<PatientPrivate>();
            if (patient.Deleted) return BadRequest("Cannot access a permanently deleted patient.");
            if (DateTime.Parse(patient.ArchivalDate) < DateTime.UtcNow)
                return BadRequest("Cannot access an archived patient.");


            return Ok(patient);
        }
        catch (Exception ex)
        {
            return NotFound(new { Message = "An error occurred while fetching patients", Error = ex.Message });
        }
    }


    /// <summary>
    ///     Update an existing patient in the database
    ///     http://localhost:5000/patient/{id}
    /// </summary>
    /// <param name="patientId">Patient's ID to update</param>
    /// <param name="patient">The data to put into that patient</param>
    /// <returns>
    ///     Ok status code for the updated patient + that patient's ID, otherwise a 404
    ///     not found message
    /// </returns>
    [HttpPut]
    [Route("{patientId}")]
    public async Task<IActionResult> UpdatePatient([FromRoute] string patientId, [FromBody] PatientPrivate patient)
    {
        // will throw bad content if the patient is invalid
        if (patient == null) return BadRequest("Patient data is required");

        DocumentSnapshot? existingPatient =
            await _firestore.Collection(COLLECTION_NAME).Document(patientId).GetSnapshotAsync();

        // check if that patient actually exists
        if (!existingPatient.Exists) return NotFound($"Patient with {patientId} not found");

        PatientPrivate? currentPatient = existingPatient.ConvertTo<PatientPrivate>();
        if (currentPatient.Deleted) return BadRequest("Cannot update a permanently deleted patient.");
        if (DateTime.Parse(currentPatient.ArchivalDate) < DateTime.UtcNow)
            return BadRequest("Cannot update an archived patient.");

        // we will now validate the patient
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Get a map of the existing patient's fields
        Dictionary<string, object>? existingPatientsFields = existingPatient.ToDictionary();

        // I will do an update on the patient and return its ID again
        // Age is stored as a long in firebase, so it will be cast to an int for comparison
        //existingPatientsFields["Age"] = (int)existingPatientsFields["Age"];
        try
        {
            Dictionary<string, object> updateFields = new();
            {
                foreach (KeyValuePair<string, object> kv in existingPatientsFields)
                {
                    // Get the property name and the existing value
                    string fieldName = kv.Key;
                    object existingValue = kv.Value;

                    // Get the corresponding value from the updated patient object
                    object? updatedValue = patient.GetType().GetProperty(fieldName)?.GetValue(patient);

                    // If the field value has changed
                    // we add it to the updateFields dictionary
                    if (!Equals(updatedValue, existingValue))
                    {
                        // If the value is null in the updated patient
                        // I will put in null so that firestore will remove that data
                        // example: we removed a patient's weight
                        if (updatedValue == null)
                            updateFields.Add(fieldName, FieldValue.Delete);
                        else
                            updateFields.Add(fieldName, updatedValue);
                    }
                }
            }
            // If there was nothing to update: the map was empty
            if (updateFields.Count == 0)
                return Ok(new { Message = $"No changes detected for {patient.FName} {patient.LName}", id = patientId });

            await _firestore.Collection("patients-private").Document(patientId).UpdateAsync(updateFields);
        }
        catch (Exception ex)
        {
            return StatusCode(500,
                new { Message = "An error occurred while fetching TODO items", Error = ex.Message });
        }

        return Ok(new { Message = "Patient Updated Successfully", id = patientId });
    }


    /// <summary>
    ///     Delete a patient from the database
    ///     http://localhost:5000/patient/{id}
    /// </summary>
    /// <param name="patientId">The patient's ID to delete</param>
    /// <returns>Ok status code if patient was deleted, 404 if patient was not found</returns>
    [HttpDelete]
    [Route("{patientId}")]
    public async Task<IActionResult> ArchivePatient([FromRoute] string patientId)
    {
        try
        {
            DocumentReference? patientRef = _firestore.Collection(COLLECTION_NAME).Document(patientId);
            DocumentSnapshot? patientSnapshot = await patientRef.GetSnapshotAsync();
            if (!patientSnapshot.Exists) return BadRequest("Patient was not found.");
            PatientPrivate? patient = patientSnapshot.ConvertTo<PatientPrivate>();
            if (patient.Deleted) return BadRequest("Cannot archive a permanently deleted patient.");

            // Update ArchivalDate to a year from now
            await patientRef.UpdateAsync(new Dictionary<string, object>
            {
                { "ArchivalDate", DateTime.UtcNow.ToString("o") }
            });

            return Ok(new { Message = "Patient archived successfully." });
        }
        catch (Exception e)
        {
            return BadRequest(new { Message = "An error occurred while restoring the patient.", Error = e.Message });
        }
    }
}