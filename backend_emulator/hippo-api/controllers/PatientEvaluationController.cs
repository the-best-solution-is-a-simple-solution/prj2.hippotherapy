using System.Text.Json;
using Google.Cloud.Firestore;
using HippoApi.controllers;
using HippoApi.middleware;
using HippoApi.Models;
using HippoApi.Models.custom_responses;
using Microsoft.AspNetCore.Mvc;

// Controller to handle GET and POSTs
namespace HippoApi.Controllers;

[ApiController]
[Route("[controller]")]
[ContentOwnerAuthorization]
public class PatientEvaluationController : ControllerBase
{
    public const string COLLECTION_NAME = "eval-forms";
    public const string CACHED_EVAL_COLLECTION_NAME = "cached-evals";
    public const string EVAL_TYPE = "evalType";
    public const string SESSION_ID = "sessionID";
    public const string FORM_DATA = "formData";
    public const string SELECTED_IMAGES = "selectedImages";
    private static FirestoreDb _firestore;

    public PatientEvaluationController(FirestoreDb firestore)
    {
        _firestore = firestore;
    }


    /// <summary>
    ///     Return the cached evaluation for the session from the route if exists
    /// </summary>
    /// <param name="patientId">Patient to get cached eval for</param>
    /// <param name="sessionId">session to query for evaluations</param>
    /// <param name="evalType">type of evaluation to get</param>
    /// <returns>200, along with the partially completed evaluation, or false otherwise</returns>
    [HttpGet]
    [Route("{patientId}/cache/{sessionId}/{evalType}")]
    public async Task<IActionResult> GetPartialEvaluation([FromRoute] string sessionId, [FromRoute] string evalType,
        [FromRoute] string patientId)
    {
        try
        {
            if (!(evalType.Equals("pre") ^ evalType.Equals("post"))) return BadRequest("Evaltype not pre/post");

            // this will throw an InvalidOperationException if no result is found
            DocumentSnapshot? cachedEvals =
                (await _firestore.Collection(CACHED_EVAL_COLLECTION_NAME).GetSnapshotAsync()).Documents.First(x =>
                {
                    Dictionary<string, dynamic>? existing = x.ConvertTo<Dictionary<string, dynamic>>();
                    return existing[SESSION_ID].Equals(sessionId) &&
                           existing[EVAL_TYPE].Equals(evalType);
                });

            return Ok(cachedEvals.ConvertTo<Dictionary<string, dynamic>>());
        }
        catch (Exception e)
        {
            // return the 404 if it is the exception for not being found, or a generic message if
            // it is a different exception than what was expected
            return e.GetType() == typeof(InvalidOperationException)
                ? NotFound($"{evalType} evaluations not found for session: {sessionId}")
                : StatusCode(500, $"Error occured during the request {e.Message}");
        }
    }


    /// <summary>
    ///     This method will clear patient evaluation caches for the sessionID passed in
    /// </summary>
    /// <param name="patientId">Patient to cache eval for</param>
    /// <param name="sessionId">Valid existing session ID that contains caches</param>
    /// <param name="evalType">Type of evaluation to delete for that session</param>
    /// <returns>200 Ok if deleted, 404 not found if the sessionId that was queried returned nothing</returns>
    [HttpDelete]
    [Route("{patientId}/cache-clear/{sessionId}/{evalType}")]
    public async Task<IActionResult> ClearCache([FromRoute] string sessionId, [FromRoute] string evalType,
        [FromRoute] string patientId)
    {
        try
        {
            if (!(evalType.Equals("pre") ^ evalType.Equals("post"))) return BadRequest("Evaltype not pre/post");

            DocumentSnapshot? cachedEvals =
                (await _firestore.Collection(CACHED_EVAL_COLLECTION_NAME).GetSnapshotAsync()).Documents.First(x =>
                {
                    Dictionary<string, dynamic>? actual = x.ConvertTo<Dictionary<string, dynamic>>();
                    return actual[SESSION_ID] == sessionId && actual[EVAL_TYPE].Equals(evalType);
                });

            if (!cachedEvals.Exists) return NotFound($"Evaluations not found for session: {sessionId}");

            await _firestore.Collection(CACHED_EVAL_COLLECTION_NAME).Document(cachedEvals.Id).DeleteAsync();
            return Ok();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return StatusCode(500, $"Error occured during the request {e.Message}");
        }
    }


    /// <summary>
    ///     This method will attempt to cache an in-progress patient evaluation, this allows for all
    ///     fields to be optional, except the type (pre/post) or the sessionID which is formed by the URL
    /// </summary>
    /// <param name="patientId">Patient to cache eval for</param>
    /// <param name="sessionId">Session to store the evaluation data for</param>
    /// <param name="patientEvaluationData">
    ///     The data to be submitted in a hashmap format
    /// </param>
    /// <returns>
    ///     400 Bad request if invalid sessionID is present or 404 not found if
    ///     that session does not exist in firebase and 200 OK updates
    ///     for a successful submission
    /// </returns>
    [HttpPost]
    [Route("{patientId}/cache/{sessionId}")]
    public async Task<IActionResult> SavePartialEvaluation([FromRoute] string patientId, [FromRoute] string sessionId,
        [FromBody] Dictionary<string, dynamic> patientEvaluationData)
    {
        Console.WriteLine(
            $"Received {patientEvaluationData.Count} data objects with keys:------------------------");

        patientEvaluationData[FORM_DATA] =
            JsonSerializer.Deserialize<Dictionary<string, dynamic>>(patientEvaluationData[FORM_DATA]);
        patientEvaluationData[SELECTED_IMAGES] =
            JsonSerializer.Deserialize<Dictionary<string, dynamic>>(patientEvaluationData[SELECTED_IMAGES]);

        // ## sanitize the map ## //
        Dictionary<string, string> selectedImages = new();
        foreach (dynamic? kv in patientEvaluationData[SELECTED_IMAGES])
            try
            {
                selectedImages[kv.Key] = ((JsonElement)kv.Value).GetString();
            }
            catch (Exception _)
            {
                selectedImages[kv.Key] = null;
            }

        patientEvaluationData[SELECTED_IMAGES] = selectedImages;

        foreach (dynamic? kv in patientEvaluationData[FORM_DATA])
            // in case the value is null we will continue onto the next one
            try
            {
                string currItem = ((JsonElement)kv.Value).GetRawText();
                if (long.TryParse(currItem, out long _))
                    patientEvaluationData[FORM_DATA][kv.Key] = long.Parse(currItem);
                else
                    patientEvaluationData[FORM_DATA][kv.Key] = ((JsonElement)kv.Value).GetString();
            }
            catch (Exception _)
            {
                patientEvaluationData[FORM_DATA][kv.Key] = null;
            }

        try
        {
            patientEvaluationData[EVAL_TYPE] = ((JsonElement)patientEvaluationData[EVAL_TYPE]).GetString()!;
            patientEvaluationData[SESSION_ID] = ((JsonElement)patientEvaluationData[SESSION_ID]).GetString()!;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return BadRequest($"No {EVAL_TYPE} or {SESSION_ID} was provided");
        }

        // query for the existing session
        QuerySnapshot? sessionCollection =
            await _firestore.CollectionGroup(SessionController.COLLECTION_NAME).GetSnapshotAsync();

        DocumentSnapshot? existingSession =
            sessionCollection.First(x => x.ConvertTo<Session>().SessionID.Equals(sessionId));

        if (!existingSession.Exists) return NotFound($"Session {sessionId} not found");

        // if an existing COMPLETE evaluation exists with the SAME evaltype under the session,
        // we will not allow saving partials for it
        if ((await existingSession.Reference.Collection(COLLECTION_NAME).GetSnapshotAsync()).Documents.Any(x =>
                x.ConvertTo<PatientEvaluation>().EvalType.Equals(patientEvaluationData["evalType"])))
            return BadRequest(
                $"{patientEvaluationData["evalType"]} evaluation already exists under the session {sessionId}");

        bool succeededInCaching = false;
        string errorMessage = "";

        try
        {
            // try to find an existing cached evaluation
            DocumentSnapshot? existingCachedEval = null;
            try
            {
                existingCachedEval = (await _firestore.Collection(CACHED_EVAL_COLLECTION_NAME)
                    .WhereEqualTo(EVAL_TYPE, patientEvaluationData[EVAL_TYPE])
                    .WhereEqualTo(SESSION_ID, patientEvaluationData[SESSION_ID]).GetSnapshotAsync()).Documents[0];
            }
            catch (Exception _)
            {
            }

            // if existing one was found, try to update it, otherwise non was found
            // so it will be created instead
            if (existingCachedEval != null && existingCachedEval.Exists)
            {
                await existingCachedEval.Reference.UpdateAsync(patientEvaluationData);
                succeededInCaching = true;
            }
            else
            {
                DocumentReference nuCachedEval =
                    await _firestore.Collection(CACHED_EVAL_COLLECTION_NAME).AddAsync(patientEvaluationData);

                if (nuCachedEval != null && !string.IsNullOrEmpty(nuCachedEval.Id)) succeededInCaching = true;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            errorMessage = e.Message;
        }

        return StatusCode(succeededInCaching ? 200 : 500,
            succeededInCaching ? "OK" : $"Write operation in firebase failed {errorMessage}");
    }


    /// <summary>
    ///     Gets the evaluation data from the provided UUID.
    /// </summary>
    /// <param name="evaluationUUID"></param>
    /// <returns>Status 200 with model if found. 404 If Evaluation with UUID not found. 500 if another error occurs</returns>
    [HttpGet]
    [Route("{evaluationUUID}")]
    public async Task<IActionResult> GetEvaluationById([FromBody] string evaluationUUID)
    {
        Console.WriteLine("Creating patient evaluation...");
        try
        {
            DocumentReference? docRef = _firestore.Collection(COLLECTION_NAME).Document(evaluationUUID);
            DocumentSnapshot? snapshot = await docRef.GetSnapshotAsync();


            PatientEvaluation? evaluation = snapshot.ConvertTo<PatientEvaluation>();

            if (!snapshot.Exists)
                // evaluation not exist return 404 error
                return NotFound(new { message = "Patient evaluation not found" });

            // Return status 200 with Evaluation Data
            return Ok(evaluation);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return StatusCode(500, new { message = "Failed to get evaluation data: ", error = ex.Message });
        }
    }


    /// <summary>
    ///     Creates a patient evaluation.
    /// </summary>
    /// <param name="eval"></param>
    /// <param name="patientId"></param>
    /// <returns>Status code 200 if successfully with message, 500 with message if not</returns>
    [HttpPost]
    [Route("{patientId}/submit-evaluation")]
    public async Task<IActionResult> CreatePatientEvaluation([FromBody] PatientEvaluation eval,
        [FromRoute] string patientId)
    {
        try
        {
            Console.WriteLine("STARTING PATIENT SUBMISSION");
            CollectionReference? patientCol = _firestore.Collection(PatientController.COLLECTION_NAME);
            DocumentSnapshot? patientSnap = await patientCol.Document(patientId).GetSnapshotAsync();

            if (!patientSnap.Exists)
            {
                Console.WriteLine("Patient cannot be found");
                return NotFound("Patient cannot be found");
            }


            // If this is a "post" evaluation query for the passed in session, and check that
            // there is an existing pre-evaluation, if there isn't return a bad request
            DocumentReference? sessionDoc = patientCol.Document(patientId)
                .Collection(SessionController.COLLECTION_NAME)
                .Document(eval.SessionID);

            DocumentSnapshot? sessionSnap = await sessionDoc.GetSnapshotAsync();

            if (!sessionSnap.Exists)
            {
                Console.WriteLine("Session cannot be found");
                return NotFound("Session cannot be found");
            }


            QuerySnapshot? evalsInSession = await sessionDoc
                .Collection(COLLECTION_NAME)
                .GetSnapshotAsync();

            List<PatientEvaluation>? evalsList = evalsInSession.Documents
                .Select(evals => evals.ConvertTo<PatientEvaluation>())
                .ToList();

            switch (evalsList.Count)
            {
                case 0:
                    if (eval.EvalType.Equals("post"))
                        return BadRequest(new { message = "Pre-Evaluation not found for post" });

                    break;

                case 1:
                    if (eval.EvalType.Equals("pre"))
                        return BadRequest(new { message = "There is already a pre-evaluation in this session" });

                    break;

                default:
                    return BadRequest(new
                        { message = "There is 2 or more evaluations in this session", error = evalsList.Count });
            }

            if (eval.Notes != null && (await GetEvaluationsTagsFromNotes(eval.Notes)).Count > 0)
            {
                eval.Exclude = true;
            }

            CollectionReference? makeEvalCol = patientCol.Document(patientId)
                .Collection(SessionController.COLLECTION_NAME)
                .Document(eval.SessionID)
                .Collection(COLLECTION_NAME);

            await makeEvalCol.AddAsync(eval);

            return Ok("Form submitted successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return StatusCode(500, new { message = "Failed to submit form", error = ex.Message });
        }
    }

    [HttpGet]
    [Route("patient/{patientId}")]
    public async Task<IActionResult> GetAllEvaluationDataForGraph([FromRoute] string patientId)
    {
        // Store all the data needed
        List<EvaluationGraphData> returnGraphData = new();

        // -----Get all sessions and evaluations for patient-----//

        // Get patient data
        CollectionReference? patientRef = _firestore.Collection(PatientController.COLLECTION_NAME);
        DocumentReference? patientDocRef = patientRef.Document(patientId);
        DocumentSnapshot? patientSnapshot = await patientDocRef.GetSnapshotAsync();

        // If patient does not exist return 404 error
        if (!patientSnapshot.Exists)
        {
            Console.WriteLine("Patient not found");
            return NotFound();
        }

        // Get sessions for patient
        CollectionReference? sessionsRef =
            patientRef.Document(patientId).Collection(SessionController.COLLECTION_NAME);

        QuerySnapshot? sessionsSnapshot = await sessionsRef.GetSnapshotAsync();
        List<Session> sessionsList = sessionsSnapshot.Documents.Select(doc => doc.ConvertTo<Session>()).ToList();

        if (!sessionsList.Any())
        {
            Console.WriteLine("No sessions found but patient exists");
            return NoContent();
        }


        foreach (Session session in sessionsList)
        {
            CollectionReference? evaluationsRef = sessionsRef.Document(session.SessionID)
                .Collection(COLLECTION_NAME);

            QuerySnapshot? evalsSnapshot = await evaluationsRef.GetSnapshotAsync();
            List<PatientEvaluation> evaluationsList =
                evalsSnapshot.Documents.Select(doc => doc.ConvertTo<PatientEvaluation>()).ToList();


            // if no evaluations for session go to next one

            // Add evaluations if they exist
            if (evaluationsList.Count() == 0)
            {
            }
            else if (evaluationsList.Count() == 1)
            {
                // If only one then it must be a pre
                EvaluationGraphData preEval = new(evaluationsList[0], session.DateTaken);
                returnGraphData.Add(preEval);
            }
            else if (evaluationsList.Count() == 2)
            {
                // For some reason 1 is the pre eval and 0 is the post
                EvaluationGraphData preEval = new(evaluationsList[1], session.DateTaken);
                EvaluationGraphData postEval = new(evaluationsList[0], session.DateTaken);
                returnGraphData.Add(preEval);
                returnGraphData.Add(postEval);
            }
            // Indicate error
        }

        // return custom data so dateTaken is in each evaluation
        return Ok(returnGraphData);
    }

    #region Tags

    /// <summary>
    /// Parses the string passed in to check for 'tags', which are in the format:<br />
    /// "This is a #example tag"<br />
    ///  - in this case, "example" would be extracted
    /// </summary>
    /// <param name="notes">The string to parse</param>
    /// <returns>A list of the tags found in the string</returns>
    public async Task<List<string>> GetEvaluationsTagsFromNotes(string notes)
    {
        List<string> validTags = Utils.GetEvaluationTagList();

        List<string> tagList = new();
        for (int i = 0; i < notes.Length; i++)
        {
            if (notes[i] != '#')
            {
                continue;
            }

            // Don't include the #
            string restOfNotes = notes.Substring(i + 1).Trim();

            if (restOfNotes.Length <= 0)
            {
                return tagList;
            }

            // Since we already trimmed this, only spaces in the middle of the string are valid
            int indexOfSpace = restOfNotes.IndexOf(' ');
            int indexOfHash = restOfNotes.IndexOf('#');
            int endOfTagIndex;

            if (indexOfHash == -1 && indexOfSpace == -1)
            {
                if (validTags.Contains(restOfNotes) && !tagList.Contains(restOfNotes))
                {
                    tagList.Add(restOfNotes);
                }

                return tagList;
            }

            if (indexOfHash == -1)
            {
                endOfTagIndex = indexOfSpace;
            }
            else if (indexOfSpace == -1)
            {
                endOfTagIndex = indexOfHash;
            }
            else if (indexOfHash < indexOfSpace)
            {
                endOfTagIndex = indexOfHash;
            }
            else
            {
                endOfTagIndex = indexOfSpace;
            }

            if (endOfTagIndex <= 0)
            {
                // Empty tag
                i++;
                continue;
            }

            // Tag in the middle or beginning of the string, doesn't include the space/#
            string tag = restOfNotes.Substring(0, endOfTagIndex).Trim();
            if (validTags.Contains(tag) && !tagList.Contains(tag))
            {
                tagList.Add(tag);
            }

            i += endOfTagIndex;
        }

        return tagList;
    }

    #endregion
}