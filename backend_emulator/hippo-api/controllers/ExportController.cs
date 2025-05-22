using Google.Cloud.Firestore;
using HippoApi.middleware;
using HippoApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace HippoApi.Controllers;

[ApiController]
[Route("[controller]")]
[ContentOwnerAuthorization]
public class ExportController : ControllerBase
{
    public AuthController _authController;
    public FirestoreDb _firestore;

    // instantiate controller and db
    public ExportController(FirestoreDb firestore)
    {
        _firestore = firestore;
        _authController = new AuthController(_firestore);
    }

    [HttpGet]
    [Route("records/{ownerId}")]
    public async Task<IActionResult> GetRecords(
        [FromRoute] string ownerId,
        [FromQuery] string? location = null, // can be nullable parameters, can be transferred with request string
        [FromQuery] string? dateTime = null,
        [FromQuery] string? condition = null,
        [FromQuery] string? patientName = null)
    {
        try
        {
            // QuerySnapshot? snapshot = await _firestore.CollectionGroup(PatientEvaluationController.COLLECTION_NAME)
            // .GetSnapshotAsync();

            List<DocumentSnapshot> evaluationsUnderOwner = await GetAllPatientEvaluationsForOwner(ownerId);
            List<List<string>> listOfRecords = [];

            // this method will throw an exception if it cannot find, and will execute the catch block below
            foreach (DocumentSnapshot? doc in evaluationsUnderOwner)
            {
                DocumentSnapshot? evalDoc = doc; // Assume 'doc' is the evaluation DocumentSnapshot
                PatientEvaluation? eval = evalDoc.ConvertTo<PatientEvaluation>();
                if (eval == null) throw new Exception("Failed to convert evaluation document.");
                // adding each value in the evaluation form to the record.

                List<string> record = [];
                record.AddRange([
                    eval.EvalType,
                    eval.Lumbar.ToString(),
                    eval.HipFlex.ToString(),
                    eval.HeadAnt.ToString(),
                    eval.HeadLat.ToString(),
                    eval.KneeFlex.ToString(),
                    eval.Pelvic.ToString(),
                    eval.PelvicTilt.ToString(),
                    eval.Thoracic.ToString(),
                    eval.Trunk.ToString(),
                    eval.TrunkInclination.ToString(),
                    eval.ElbowExtension.ToString()
                ]);

                DocumentSnapshot? sessionSnapshot = await evalDoc.Reference.Parent.Parent.GetSnapshotAsync();
                if (sessionSnapshot == null || !sessionSnapshot.Exists)
                    throw new Exception("Session document not found.");
                Session? session = sessionSnapshot.ConvertTo<Session>();
                if (session == null) throw new Exception("Failed to convert session document.");

                DocumentSnapshot? patientSnapshot = await sessionSnapshot.Reference.Parent.Parent.GetSnapshotAsync();
                if (patientSnapshot == null || !patientSnapshot.Exists)
                    throw new Exception("Patient document not found.");
                PatientPrivate? patient = patientSnapshot.ConvertTo<PatientPrivate>();
                if (patient == null) throw new Exception("Failed to convert patient document.");

                // Handle anonymized patients
                string patientFullName = patient.Deleted
                    ? patient.FName
                    : $"{patient.FName ?? "Unknown"} {patient.LName ?? "Unknown"}";
                record.InsertRange(0, [
                    patientFullName, // Use Anonymous if Deleted is true
                    patient.Age.ToString(),
                    patient.Weight?.ToString() ?? "N/A",
                    patient.Height?.ToString() ?? "N/A",
                    patient.Condition,
                    session.DateTaken.ToShortDateString(),
                    session.Location
                ]);

                listOfRecords.Add(record);
            }

            // after we grab all the evaluations, filter them down based on the paramter which can be null or empty.
            string trimmedPatientName = patientName?.Trim();
            foreach (List<string> record in listOfRecords.ToList())
            {
                if (!string.IsNullOrEmpty(trimmedPatientName))
                    if (!record[0].Equals(trimmedPatientName))
                        listOfRecords.Remove(record);

                if (!string.IsNullOrEmpty(dateTime))
                {
                    string[] dates = dateTime.Split(",");
                    DateTime fromDate = DateTime.Parse(dates[0]);
                    DateTime toDate = DateTime.Parse(dates[1]);
                    DateTime currentDate = DateTime.Parse(record[5]);
                    if (!(currentDate.CompareTo(fromDate) >= 0 && currentDate.CompareTo(toDate) <= 0))
                        listOfRecords.Remove(record);
                }

                if (!string.IsNullOrEmpty(condition))
                    if (!condition.Equals(record[4]))
                        listOfRecords.Remove(record);

                if (!string.IsNullOrEmpty(location))
                    if (!location.Equals(record[6]))
                        listOfRecords.Remove(record);
            }

            if (listOfRecords.Count == 0)
            {
                Console.WriteLine("No evaluations export records found");
                return NoContent();
            }

            return Ok(listOfRecords);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return StatusCode(500, new { Message = "An error occurred fetching all records", Error = ex.Message });
        }
    }

    /// <summary>
    ///     Grabs all the unique names of all the patients in the database
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("get-unique-names/{ownerId}")]
    public async Task<IActionResult> GetUniqueNames([FromRoute] string ownerId)
    {
        try
        {
            // Get all therapistId's for owner
            List<string> therapistIds = await _authController.GetAllTherapistIdsForOwner(ownerId);

            // Get patients only for therapists under that owner
            CollectionReference patientRef = _firestore.Collection(PatientController.COLLECTION_NAME);
            QuerySnapshot snapshot = await patientRef
                .WhereIn("TherapistID", therapistIds) // only look under owner's therapists
                .WhereEqualTo("Deleted", false) // Exclude deleted patients
                .WhereGreaterThan("ArchivalDate", DateTime.UtcNow.ToString("o")) // Exclude archived patients
                .GetSnapshotAsync();

            // If patient not exist return 404
            if (snapshot.Count == 0)
            {
                Console.WriteLine("Sessions not found");
                return NotFound();
            }

            // grab every patient
            List<PatientPrivate> patientList =
                snapshot.Documents.Select(doc => doc.ConvertTo<PatientPrivate>()).ToList();

            // initalize the list of names
            List<string> uniqueNames = new();
            foreach (PatientPrivate p in patientList)
            {
                // make fullname
                string fullName = p.FName + " " + p.LName;
                // add if list doesnt contain all of them
                if (!uniqueNames.Contains(fullName)) uniqueNames.Add(fullName);
            }

            return Ok(uniqueNames);
        }
        catch (Exception ex)
        {
            return NotFound(new
                { Message = "An error occurred while fetching unique patient names", Error = ex.Message });
        }
    }

    /// <summary>
    ///     Grabs all the patients with the same conditions
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("get-unique-conditions/{ownerId}")]
    public async Task<IActionResult> GetUniqueConditions([FromRoute] string ownerId)
    {
        try
        {
            // Get all therapistId's for owner
            List<string> therapistIds = await _authController.GetAllTherapistIdsForOwner(ownerId);

            Query patientRef = _firestore.Collection(PatientController.COLLECTION_NAME)
                .WhereIn("TherapistID", therapistIds) // look for patients under provided owner
                .WhereEqualTo("Deleted", false) // Exclude deleted patients
                .WhereGreaterThan("ArchivalDate", DateTime.UtcNow.ToString("o")); // Exclude archived patients
            QuerySnapshot snapshot = await patientRef.GetSnapshotAsync();

            // If patient not exist return 404
            if (snapshot.Count == 0)
            {
                Console.WriteLine("Sessions not found");
                return NotFound();
            }

            // grab every patient
            List<PatientPrivate> patientList =
                snapshot.Documents.Select(doc => doc.ConvertTo<PatientPrivate>()).ToList();

            // initalize the list of conditions
            List<string> uniqueConditions = new();
            foreach (PatientPrivate p in patientList)
                // add if list doesnt contain all of them
                if (!uniqueConditions.Contains(p.Condition))
                    uniqueConditions.Add(p.Condition);

            return Ok(uniqueConditions);
        }
        catch (Exception ex)
        {
            return StatusCode(500,
                new { Message = "An error occurred while fetching unique patient conditions", Error = ex.Message });
        }
    }


    /// <summary>
    ///     Grabs the unique locations of all sessions in the db under provided owner
    /// </summary>
    /// <returns>List Of Unique Locations of all sessions</returns>
    [HttpGet]
    [Route("get-unique-locations/{ownerId}")]
    public async Task<IActionResult> GetUniqueLocations([FromRoute] string ownerId)
    {
        try
        {
            // Get only sessions under ownerId
            List<DocumentSnapshot> snapshot = await GetAllPatientSessionsForOwner(ownerId);

            // QuerySnapshot? snapshot = await _firestore.CollectionGroup("sessions").GetSnapshotAsync();

            // If sessions not exist return 404
            if (snapshot.Count == 0)
            {
                Console.WriteLine("Sessions not found");
                return NotFound();
            }

            // grab every session
            List<Session> sessionList = snapshot.Select(doc => doc.ConvertTo<Session>()).ToList();
            // List<Session> sessionList = snapshot.Documents.Select(doc => doc.ConvertTo<Session>()).ToList();

            // initialize the list of locations
            List<string> uniqueLocations = new();
            foreach (Session s in sessionList)
                // add if list doesn't contain of them
                if (!uniqueLocations.Contains(s.Location))
                    uniqueLocations.Add(s.Location);

            return Ok(uniqueLocations);
        }
        catch (Exception ex)
        {
            return StatusCode(500,
                new
                {
                    Message = "An error occurred while fetching unique locations from sessions", Error = ex.Message
                });
        }
    }

    /// <summary>
    ///     Gets the lowest year search sessions under owner
    /// </summary>
    /// <param name="ownerId"></param>
    /// <returns>lowest year</returns>
    [HttpGet]
    [Route("get-lowest-year/{ownerId}")]
    public async Task<IActionResult> GetLowestYear([FromRoute] string ownerId)
    {
        try
        {
            // Get all therapistId's for owner
            List<string> therapistIds = await _authController.GetAllTherapistIdsForOwner(ownerId);

            if (therapistIds.Count == 0)
            {
                Console.WriteLine("No therapists found under owner");
                return NoContent();
            }

            // Get all patients for these therapists - but only fetch the IDs, not the entire documents
            QuerySnapshot patientsSnapshot = await _firestore
                .Collection(PatientController.COLLECTION_NAME)
                .WhereIn("TherapistID", therapistIds)
                .WhereEqualTo("Deleted", false)
                .Select("__name__") // Only fetch document IDs, not the entire document
                .GetSnapshotAsync();

            DateTime? earliestDate = null;

            // For each patient, find their earliest session date
            foreach (DocumentSnapshot patient in patientsSnapshot.Documents)
            {
                string patientId = patient.Id;

                // Get the earliest session for this patient - only fetch the DateTaken field
                QuerySnapshot earliestSessionSnapshot = await _firestore
                    .Collection(PatientController.COLLECTION_NAME)
                    .Document(patientId)
                    .Collection(SessionController.COLLECTION_NAME)
                    .OrderBy("DateTaken")
                    .Limit(1)
                    .Select("DateTaken") // Only fetch the DateTaken field
                    .GetSnapshotAsync();

                // If this patient has sessions, check if it's the earliest we've seen
                if (earliestSessionSnapshot.Count > 0)
                {
                    DateTime sessionDate = earliestSessionSnapshot.Documents[0].GetValue<DateTime>("DateTaken");

                    if (earliestDate == null || sessionDate < earliestDate) earliestDate = sessionDate;
                }
            }

            // If no sessions found, return 404
            if (earliestDate == null)
            {
                Console.WriteLine($"No sessions while getting lowest year for owner {ownerId}");
                return NotFound();
            }

            // Return the year of the earliest session
            return Ok(earliestDate.Value.Year);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting lowest year: {ex.Message}");
            return StatusCode(500, "An error occurred while processing your request");
        }
    }

    #region Helper Methods

    /// <summary>
    ///     A helper method to get all patient sessions under a provided owner
    /// </summary>
    /// <param name="ownerId">ownerId to look under</param>
    /// <returns>List of session document snapshots</returns>
    public async Task<List<DocumentSnapshot>> GetAllPatientSessionsForOwner(string ownerId)
    {
        // Store all evaluations in a list
        List<DocumentSnapshot> returnSessionDocs = new();

        // Get all therapistId's for owner
        List<string> therapistIds = await _authController.GetAllTherapistIdsForOwner(ownerId);

        // Get all patients for these therapists
        QuerySnapshot patientsSnapshot = await _firestore
            .Collection(PatientController.COLLECTION_NAME)
            .WhereIn("TherapistID", therapistIds)
            .WhereEqualTo("Deleted", false) // Exclude deleted patients
            .GetSnapshotAsync();

        // For each patient, get all their sessions 
        foreach (DocumentSnapshot patient in patientsSnapshot.Documents)
        {
            string patientId = patient.Id;

            // Get all sessions for this patient
            QuerySnapshot sessionsSnapshot = await _firestore
                .Collection(PatientController.COLLECTION_NAME)
                .Document(patientId)
                .Collection(SessionController.COLLECTION_NAME)
                .GetSnapshotAsync();

            returnSessionDocs.AddRange(sessionsSnapshot.Documents);
        }

        return returnSessionDocs;
    }

    /// <summary>
    ///     A helper method to get all patient evaluations for provided owner
    /// </summary>
    /// <param name="ownerId">ownerId to look under</param>
    /// <returns>List of evaluation document snapshots</returns>
    public async Task<List<DocumentSnapshot>> GetAllPatientEvaluationsForOwner(string ownerId)
    {
        // Store all evaluations in a list
        List<DocumentSnapshot> allEvaluations = new();

        // Get all therapistId's for owner
        List<string> therapistIds = await _authController.GetAllTherapistIdsForOwner(ownerId);
        if (therapistIds.Count == 0)
        {
            Console.WriteLine("No therapists found under owner");
            return allEvaluations;
        }

        // Get all patients for these therapists
        QuerySnapshot patientsSnapshot = await _firestore
            .Collection(PatientController.COLLECTION_NAME)
            .WhereIn("TherapistID", therapistIds)
            .GetSnapshotAsync();

        // For each patient, get all their sessions and then evaluations
        foreach (DocumentSnapshot patient in patientsSnapshot.Documents)
        {
            string patientId = patient.Id;

            // Get all sessions for this patient
            QuerySnapshot sessionsSnapshot = await _firestore
                .Collection(PatientController.COLLECTION_NAME)
                .Document(patientId)
                .Collection(SessionController.COLLECTION_NAME)
                .GetSnapshotAsync();

            // For each session, get all evaluations
            foreach (DocumentSnapshot session in sessionsSnapshot.Documents)
            {
                string sessionId = session.Id;

                QuerySnapshot sessionEvaluations = await _firestore
                    .Collection(PatientController.COLLECTION_NAME)
                    .Document(patientId)
                    .Collection(SessionController.COLLECTION_NAME)
                    .Document(sessionId)
                    .Collection(PatientEvaluationController.COLLECTION_NAME)
                    .GetSnapshotAsync();

                // Add all evaluations to our list
                allEvaluations.AddRange(sessionEvaluations.Documents);
            }
        }

        return allEvaluations;
    }

    #endregion
}