using System.Net;
using System.Text.RegularExpressions;
using Google.Cloud.Firestore;
using HippoApi.integration_test_data;
using HippoApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json.Linq;

namespace HippoApi.Controllers;

public class RestrictToLocalhostAttribute : ActionFilterAttribute
{
    /// <summary>
    ///     A custom attribute to restrict the sensitive integration test controller to
    ///     only allow requests from localhost
    /// </summary>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        IPAddress remoteIP = context.HttpContext.Connection.RemoteIpAddress!;
        IPAddress localIP = context.HttpContext.Connection.LocalIpAddress!;
        if (!(IPAddress.IsLoopback(remoteIP) && IPAddress.IsLoopback(localIP)))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        base.OnActionExecuting(context);
    }
}

[RestrictToLocalhost] // Use custom attribute to restrict to localhost
[ApiController]
[Route("integration-tests")]
public class IntegrationTestDataController : ControllerBase
{
    private const string AUTH_PORT = "9099";
    private const string FIRESTORE_PORT = "8080";
    private static FirestoreDb _firestore;

    // instantiate controller and db
    public IntegrationTestDataController(FirestoreDb firestore)
    {
        _firestore = firestore;
    }

    [HttpPost]
    [Route("seed-owner-therapist-info")]
    public async Task<IActionResult> SeedOwnerTherapistInfo()
    {
        Console.WriteLine("Seeding owner-therapist-info: " + _firestore.ProjectId);
        Console.WriteLine("\nSeeding Owner and Therapist info");
        try
        {
            await OwnerTherapistSeedData.SeedOwnerInfoLoginAndTherapistListPage(_firestore);
        }
        catch (Exception _)
        {
        }

        Console.WriteLine("Done");
        return Ok();
    }

    [HttpPost]
    [Route("seed-patient-list-page-data")]
    public async Task<IActionResult> SeedPatientListPageTestData()
    {
        Console.WriteLine("Seeding patient list...");
        try
        {
            await PatientInfoSeedData.SeedPatientListTestData();
        }
        catch (Exception _)
        {
        }

        Console.WriteLine("Done");

        return Ok();
    }

    [HttpPost]
    [Route("seed-patient-export-page-data")]
    public async Task<IActionResult> SeedPatientExportPageTestData()
    {
        Console.WriteLine("Seeding export info ...");
        try
        {
            await PatientInfoSeedData.SeedPatientExportInfo();
        }
        catch (Exception _)
        {
        }

        Console.WriteLine("Done");
        return Ok();
    }

    [HttpPost]
    [Route("seed-patient-info-session-tab")]
    public async Task<IActionResult> SeedPatientInfoPageSessionTabTestData()
    {
        Console.WriteLine("\nSeeding patient list...");
        try
        {
            await PatientInfoSeedData.SeedPatientInfoSessionsTabTestData();
        }
        catch (Exception _)
        {
        }

        Console.WriteLine("Done");

        return Ok();
    }

    [HttpPost]
    [Route("seed-patient-info-graph-tab")]
    public async Task<IActionResult> SeedPatientInfoPageGraphTabTestData()
    {
        Console.WriteLine("\nSeeding patients for graph tab...");
        try
        {
            await PatientInfoSeedData.SeedPatientInfoPageGraphTabTestData();
        }
        catch (Exception _)
        {
        }

        Console.WriteLine("Done");

        return Ok();
    }

    [HttpPost]
    [Route("seed-transfer-patient-data")]
    public async Task<IActionResult> SeedTransferPatientTestData()
    {
        Console.WriteLine("\nSeeding data for transfer patients...");
        try
        {
            await TransferPatientsSeedData.SeedData(_firestore);
        }
        catch (Exception _)
        {
        }

        Console.WriteLine("Done Seeding data for transfer patients");
        return Ok();
    }

    [HttpPost]
    [Route("seed-archive-data")]
    public async Task<IActionResult> SeedArchiveData()
    {
        Console.WriteLine("\nSeeding Archive Data");
        try
        {
            await ArchiveSeedData.SeedArchiveData(_firestore);
        }
        catch (Exception _)
        {
        }

        Console.WriteLine("Done");
        return Ok();
    }

    [HttpPost]
    [Route("seed-evaluation-page-data")]
    public async Task<IActionResult> SeedEvaluationPageData()
    {
        Console.WriteLine("\nSeeding Evaluation page Data");
        try
        {
            await EvaluationPageSeedData.SeedEvaluationPageData(_firestore);
        }
        catch (Exception _)
        {
        }

        Console.WriteLine("Done");
        return Ok();
    }

    [HttpPost]
    [Route("seed-cached-evaluation-data")]
    public async Task<IActionResult> SeedCachedEvaluationData()
    {
        Console.WriteLine("\nSeeding Cached Evaluation Data");
        try
        {
            await EvaluationPageSeedData.SeedCachedEvaluationData(_firestore);
        }
        catch (Exception _)
        {
        }

        Console.WriteLine("Done");
        return Ok();
    }

    /// This method will respond to a ping request
    [HttpGet]
    [Route("ping")]
    public async Task<IActionResult> Ping()
    {
        return Ok(new { message = "Pong", status = "Service is running" });
    }

    /// <summary>
    ///     Clear the firestore database for testing
    /// </summary>
    /// <returns>Status 200 if successful 500 otherwise</returns>
    [HttpDelete]
    [Route("clear")]
    public async Task<IActionResult> ClearFirestoreEmulatorDataAsync()
    {
        using (HttpClient client = new())
        {
            string emulatorUrl =
                $"http://localhost:{FIRESTORE_PORT}/emulator/v1/projects/{_firestore.ProjectId}/databases/(default)/documents";
            HttpResponseMessage response = await client.DeleteAsync(emulatorUrl);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine(
                    $"Firestore emulator data for project {_firestore.ProjectId} cleared successfully on path {emulatorUrl}");
                return Ok(
                    $"Firestore emulator  for project {_firestore.ProjectId} cleared successfully on path {emulatorUrl}");
            }

            string error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Failed to clear Firestore emulator data: {error}");
            Console.WriteLine($"attempted url: {emulatorUrl}");
            return StatusCode(500, error);
        }
    }

    /// <summary>
    ///     Clears the Firebase authentication emulator
    /// </summary>
    /// <returns>Status 200 if successful</returns>
    [HttpDelete]
    [Route("clear-auth")]
    public async Task<IActionResult> ClearFirebaseAuthenticationEmulatorDataAsync()
    {
        using (HttpClient client = new())
        {
            string emulatorUrl = $"http://localhost:{AUTH_PORT}/emulator/v1/projects/{_firestore.ProjectId}/accounts";
            HttpResponseMessage response = await client.DeleteAsync(emulatorUrl);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine(
                    $"Firebase Authentication emulator for project {_firestore.ProjectId} data cleared successfully. on path {emulatorUrl}");
                return Ok(
                    $"Firebase Authentication emulator for project {_firestore.ProjectId} cleared successfully on path {emulatorUrl}");
            }

            string error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Failed to clear Firebase Authentication emulator data: {error}");
            Console.WriteLine($"attempted url: {emulatorUrl}");
            return StatusCode(500, error);
        }
    }


    /// <summary>
    ///     Clears Mail Hog's emails
    /// </summary>
    /// <returns>OK object result if successful</returns>
    [HttpDelete]
    [Route("clear-mail")]
    public async Task<IActionResult> ClearMailEmulatorDataAsync()
    {
        try
        {
            using (HttpClient client = new())
            {
                await client.DeleteAsync("http://localhost:8025/api/v1/messages");
            }

            return Ok("Mail emulator data cleared successfully");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Problem(e.Message);
        }
    }

    /// <summary>
    ///     Grabs the LINK in an verify or referral email enclosed in an
    ///     <a>
    ///         href.
    ///         WILL NOT WORK WITH POSTMAN, NEEDS TO BE CALLED WITH BROWSER since reqyest.header.origin is initialized with a
    ///         browser call
    ///         LINK WILL BE /request in email ---- instead of localhost:/###/request if called in backend
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("get-latest-mailv2")]
    public async Task<IActionResult> GetLatestMailV2()
    {
        using HttpClient httpClient = new();
        try
        {
            // Fetch latest email
            HttpResponseMessage response =
                await httpClient.GetAsync("http://localhost:8025/api/v2/messages?start=0&limit=1");

            if (!response.IsSuccessStatusCode) return Problem("Failed to fetch latest messages from MailHog.");

            string jsonResponse = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(jsonResponse);

            // Extract email body
            JToken? latestMessage = json["items"]?.FirstOrDefault();
            string? encodedBody = latestMessage?["Content"]?["Body"]?.ToString();

            if (string.IsNullOrEmpty(encodedBody)) return NotFound("No email body found.");

            // Clean up the encoded body (Fix 'href=3D' and remove newlines)
            string cleanedBody = encodedBody.Replace("=\r\n", "") // Remove soft line breaks
                .Replace("=\n", "") // Remove newlines
                .Replace("=3D", "="); // Fix encoded equals sign

            Console.WriteLine(cleanedBody);

            // Extract first URL from <a href="...">
            Match match = Regex.Match(cleanedBody, @"href=""([^""]+)""");

            Console.WriteLine(match);
            if (match.Success)
            {
                Console.WriteLine("MATCH GROUP 1");
                Console.WriteLine(match.Groups[1].Value);
                return Ok(match.Groups[1].Value);
            }

            return NotFound("No URL found in email body.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    ///     Add patient with data to collection.
    /// </summary>
    /// <param name="patient">patient data</param>
    public static async Task AddPatient(PatientPrivate patient)
    {
        CollectionReference? patientCollection = _firestore.Collection(PatientController.COLLECTION_NAME);
        await patientCollection.Document(patient.Id).SetAsync(patient);
    }

    /// <summary>
    ///     Add session to patient sub collection inside patient.
    /// </summary>
    /// <param name="patient">patient to be added under</param>
    /// <param name="session">session data</param>
    public static async Task AddSession(PatientPrivate patient, Session session)
    {
        CollectionReference? patientCollection = _firestore.Collection(PatientController.COLLECTION_NAME);
        CollectionReference? sessionCollection =
            patientCollection.Document(patient.Id).Collection(SessionController.COLLECTION_NAME);
        await sessionCollection.Document(session.SessionID).SetAsync(session);
    }

    /// <summary>
    ///     Add evaluation to evaluation collection inside patient document
    /// </summary>
    /// <param name="patient">patient document</param>
    /// <param name="evaluation">evaluation to add</param>
    /// <param name="session">session to add</param>
    public static async Task AddEvaluation(PatientPrivate patient, PatientEvaluation evaluation, Session session)
    {
        DocumentReference? patientCollection =
            _firestore.Collection(PatientController.COLLECTION_NAME).Document(patient.Id);
        DocumentReference? sessionCollection =
            patientCollection.Collection(SessionController.COLLECTION_NAME).Document(session.SessionID);
        CollectionReference? evaluationCollection =
            sessionCollection.Collection(PatientEvaluationController.COLLECTION_NAME);
        await evaluationCollection.Document(evaluation.EvaluationID).SetAsync(evaluation);
    }

    /// <summary>
    ///     Add therapist to the therapists collection.
    /// </summary>
    /// <param name="therapist">Therapist data</param>
    /// <param name="ownerId">Optional owner to put them under</param>
    public static async Task AddTherapist(Therapist therapist, string? ownerId)
    {
        try
        {
            if (ownerId != null)
            {
                DocumentReference? OwnerDoc = _firestore.Collection(OwnerController.COLLECTION_NAME).Document(ownerId);
                CollectionReference? ownerTherapistCollection =
                    OwnerDoc.Collection(TherapistController.COLLECTION_NAME);
                therapist.TherapistID ??= Guid.NewGuid().ToString();
                await ownerTherapistCollection.Document(therapist.TherapistID).SetAsync(therapist);
                return;
            }

            // Get the therapists collection
            CollectionReference? therapistCollection = _firestore.Collection("therapists");

            // Generate a unique ID if none is provided
            therapist.TherapistID ??= Guid.NewGuid().ToString();

            // Save the therapist to Firestore
            DocumentReference? docRef = therapistCollection.Document(therapist.TherapistID);
            await docRef.SetAsync(therapist);

            Console.WriteLine($"Therapist {therapist.FName} {therapist.LName} added with ID {therapist.TherapistID}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding therapist: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Add therapist to the owners' collection.
    /// </summary>
    /// <param name="owner">Owner</param>
    public static async Task AddOwner(Owner owner)
    {
        try
        {
            CollectionReference? ownerCollection = _firestore.Collection(OwnerController.COLLECTION_NAME);

            await ownerCollection.Document(owner.OwnerId).SetAsync(owner);

            Console.WriteLine($"Owner {owner.FName} {owner.LName} added with ID {owner.OwnerId}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding owner: {ex.Message}");
            throw;
        }
    }
}