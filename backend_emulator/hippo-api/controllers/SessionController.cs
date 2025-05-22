using Google.Cloud.Firestore;
using HippoApi.middleware;
using HippoApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace HippoApi.Controllers;

[ApiController]
[Route("[controller]")]
[ContentOwnerAuthorization]
public class SessionController : ControllerBase
{
    public const string COLLECTION_NAME = "sessions";
    private readonly FirestoreDb _firestore;


    public SessionController(FirestoreDb firestore)
    {
        _firestore = firestore;
    }


    /// <summary>
    ///     This method will create a session based on what is passed in
    ///     http://localhost:5000/session/patient/{patientID}/submit-session
    /// </summary>
    /// <returns>
    ///     Status Code 201 (Created) if created along with its firestore ID, or a 400
    ///     (bad request) otherwise
    /// </returns>
    [HttpPost]
    [Route("patient/{patientId}/submit-session")]
    public async Task<ActionResult> CreateSession([FromBody] Session sess, [FromRoute] string patientId)
    {
        try
        {
            DocumentReference patientRef = _firestore
                .Collection(PatientController.COLLECTION_NAME)
                .Document(patientId);

            DocumentSnapshot patientSnapshot = await _firestore
                .Collection(PatientController.COLLECTION_NAME)
                .Document(patientId)
                .GetSnapshotAsync();

            // Check if the document exists
            if (!patientSnapshot.Exists) return BadRequest("Patient was not found.");

            // Update ArchivalDate to a year from now
            await patientRef.UpdateAsync(new Dictionary<string, object>
            {
                { "ArchivalDate", DateTime.UtcNow.AddYears(1).ToString("o") }
            });

            // get reference to the sessions in the Patient document itself
            CollectionReference? sessionsRef = _firestore
                .Collection(PatientController.COLLECTION_NAME)
                .Document(patientId)
                .Collection(COLLECTION_NAME);

            Session sessionToAdd = sess;

            // if session is not valid, return why it isn't valid
            if (!ModelState.IsValid) return BadRequest(ModelState);

            DocumentReference? sessionCreated = await sessionsRef.AddAsync(sessionToAdd);
            return Ok(new { Message = "Session created successfully.", sessionCreated.Id });
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    /// <summary>
    ///     Returns a list of sessions for the provided patientId
    /// </summary>
    /// <param name="patientId">The patient to get all sessions for.</param>
    /// <returns>
    ///     Status Code 200 (OK) and sessions if the patientID is found.
    ///     Status Code 404 (not found) if a patient with the ID does not exist
    ///     Code 204 (no content) if no sessions exist for that patient
    /// </returns>
    [HttpGet("patient/{patientId}/session/")]
    public async Task<IActionResult> GetAllSessions([FromRoute] string patientId)
    {
        Console.WriteLine($"\nReceived request for getAllSessions for patientID: {patientId}");
        try
        {
            CollectionReference? patientRef = _firestore.Collection(PatientController.COLLECTION_NAME);
            DocumentReference? docRef = patientRef.Document(patientId);

            DocumentSnapshot? patientSnapshot = await docRef.GetSnapshotAsync();

            // Check if the document exists
            if (!patientSnapshot.Exists)
            {
                Console.WriteLine("Patient not found");
                return NotFound();
            }

            CollectionReference? sessionsRef = patientRef.Document(patientId).Collection(COLLECTION_NAME);
            // Query for sessions with provided PatientID, convert to list
            // Query sessionsForPatientQuery = sessionsRef.WhereEqualTo("PatientID", patientID);
            // QuerySnapshot snapshot = await sessionsForPatientQuery.GetSnapshotAsync();
            QuerySnapshot? snapshot = await sessionsRef.GetSnapshotAsync();
            List<Session> sessionsList = snapshot.Documents.Select(doc => doc.ConvertTo<Session>()).ToList();

            if (!sessionsList.Any())
            {
                Console.WriteLine("No sessions found but patient exists");
                return NoContent();
            }

            // Return session list
            Console.WriteLine($"Found {sessionsList.Count} sessions");
            sessionsList.ForEach(e => Console.WriteLine(e.DateTaken));
            return Ok(sessionsList);
        }
        catch (Exception ex)
        {
            return StatusCode(500,
                new { Message = "An error occurred while fetching sessions for patient", Error = ex.Message });
        }
    }

    /// <summary>
    ///     Gets the pre and post evaluations for the provided sessionID (if they exist).
    /// </summary>
    /// <param name="patientId">the patient id to look for</param>
    /// <param name="sessionID">the session id to look for</param>
    /// <returns>
    ///     Status Code 200 (OK) if both pre and post evaluations are found in order 'pre' then 'post'
    ///     Status Code 206 (partial content) if only one was found
    ///     Status Code 204 (no content) if none found
    ///     Status Code 404 (not found) if patient or session not found
    /// </returns>
    [HttpGet("patient/{patientId}/session/{sessionID}/pre-post/")]
    public async Task<IActionResult> GetPrePostEvaluations([FromRoute] string patientId, [FromRoute] string sessionID)
    {
        Console.WriteLine($"\nReceived request for GetPrePostEvaluations for patientID: {patientId}");
        try
        {
            CollectionReference? patientRef = _firestore.Collection(PatientController.COLLECTION_NAME);
            DocumentReference? docRef = patientRef.Document(patientId);
            DocumentSnapshot? patientSnapshot = await docRef.GetSnapshotAsync();

            // If patient not exist return 404
            if (!patientSnapshot.Exists)
            {
                Console.WriteLine("Patient not found");
                return NotFound();
            }

            DocumentReference? sessionRef = docRef.Collection(COLLECTION_NAME).Document(sessionID);

            CollectionReference? evalsRef = sessionRef.Collection(PatientEvaluationController.COLLECTION_NAME);


            // Query for evaluations with provided sessionUUID
            Query? evalsForPatientQuery = evalsRef
                .OrderByDescending("EvalType"); // Sort so 'pre' is before 'post'
            QuerySnapshot? snapshot = await evalsForPatientQuery.GetSnapshotAsync();

            List<PatientEvaluation> evalsList =
                snapshot.Documents.Select(doc => doc.ConvertTo<PatientEvaluation>()).ToList();

            // If no results return 204 (no content)
            if (evalsList.Count == 0)
            {
                Console.WriteLine("No evaluations found");
                return NoContent();
            }

            // If one result return status 206 (partial content)
            if (evalsList.Count() == 1)
            {
                Console.WriteLine("Found 1 evaluation of type: " + evalsList[0].EvalType);
                return StatusCode(206, evalsList);
            }

            // If it has a pre and post evaluation return status OK
            if (evalsList.Count == 2)
            {
                PatientEvaluation eval1 = evalsList[0];
                PatientEvaluation eval2 = evalsList[1];

                if (eval1.EvalType == "pre" && eval2.EvalType == "post")
                {
                    Console.WriteLine("Found 2 evaluations");
                    return Ok(evalsList);
                }

                throw new Exception("Does not have one pre and one post evaluation");
            }

            throw new Exception("Has more than 2 evaluations has: " + evalsList.Count);
        }
        catch (Exception ex)
        {
            return StatusCode(500,
                new
                {
                    Message = "An error occurred while fetching pre and post evaluations for session",
                    Error = ex.Message
                });
        }
    }
}