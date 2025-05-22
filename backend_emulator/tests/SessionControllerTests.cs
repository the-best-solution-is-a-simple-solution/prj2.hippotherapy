using Google.Api.Gax;
using Google.Cloud.Firestore;
using HippoApi;
using HippoApi.Controllers;
using HippoApi.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace tests;

[TestFixture]
public class SessionControllerTests
{
    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        // Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", "localhost:8080");

        // instantiate db
        // _firestoreDb = new FirestoreDbBuilder
        // {
        //     ProjectId = "test-project-id",
        //     EmulatorDetection = EmulatorDetection.EmulatorOnly
        // }.Build();

        _sessionController = new SessionController(_firestoreDb);
        _integrationTestDataController = new IntegrationTestDataController(_firestoreDb);
        await _integrationTestDataController.ClearFirestoreEmulatorDataAsync();

        patientWithSessionsID = "patient-with-sessions-85a79df5-0cc9-4539-9b1e-1db2c1163fe1";
        patientWithNoSessionsID = "patient-no-sessions-85a79df5-0cc9-4539-9b1e-1db2c1163fe1";

        // Add a few patients and sessions with evaluations
        PatientPrivate johnSmithPatientPrivate = new()
        {
            Id = patientWithSessionsID,
            FName = "John",
            LName = "Smith"
        };

        PatientPrivate aliceTailorPatientPrivate = new()
        {
            Id = patientWithNoSessionsID,
            FName = "Alice",
            LName = "Tailor"
        };

        // UUID's generated from https://www.uuidgenerator.net/
        sessionAWithPrePostEvalsID = "test-sessionA_e42a56a1-7abc-4b41-a48c-d2c6fd4bb482";
        evalA_PreID = "test-evalPreA_1f24bec8-ea55-479e-ba37-e5e31d1f6aa3";
        evalA_PostID = "test-evalPostA_092497af-ce43-4844-aa11-3161d934316";

        // integration session data
        Session sessionA = new()
        {
            SessionID = sessionAWithPrePostEvalsID,
            PatientID = patientWithSessionsID,
            DateTaken = new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Location = "MX"
        };

        PatientEvaluation sessionAPreEval = new()
        {
            SessionID = sessionA.SessionID,
            EvaluationID = evalA_PreID,
            EvalType = "pre",
            Lumbar = 2,
            HipFlex = 1,
            HeadAnt = 0,
            HeadLat = -1,
            KneeFlex = -2,
            Pelvic = -1,
            PelvicTilt = 0,
            Thoracic = 1,
            Trunk = 2,
            TrunkInclination = 1,
            ElbowExtension = 0
        };

        PatientEvaluation sessionAPostEval = new()
        {
            SessionID = sessionA.SessionID,
            EvaluationID = evalA_PostID,
            EvalType = "post",
            Lumbar = 2,
            HipFlex = 1,
            HeadAnt = 0,
            HeadLat = -1,
            KneeFlex = -2,
            Pelvic = -1,
            PelvicTilt = 0,
            Thoracic = 1,
            Trunk = 2,
            TrunkInclination = 1,
            ElbowExtension = 0
        };

        // Session B with only a pre eval
        sessionBWithPreEvalID = "test-sessionB_8f791aee-c253-4817-a804-a4695b93e279";
        evalB_PreID = "test-evalB_125b4668-6ee3-460c-a901-41ca84ea6c5f";

        Session sessionB = new()
        {
            SessionID = sessionBWithPreEvalID,
            PatientID = patientWithSessionsID,
            DateTaken = new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Location = "US"
        };

        PatientEvaluation sessionBPreEval = new()
        {
            SessionID = sessionB.SessionID,
            EvaluationID = evalB_PreID,
            EvalType = "pre"
        };

        sessionCWithNoEvals = "test-sessionC_964a114b-e810-408f-9a1b-5bb07f46aada";

        Session sessionWithNoEvaluations = new()
        {
            SessionID = sessionCWithNoEvals,
            PatientID = patientWithSessionsID,
            DateTaken = new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Location = "CA"
        };

        // Add Patients to db
        await AddPatient(johnSmithPatientPrivate);
        await AddPatient(aliceTailorPatientPrivate);

        // Add session to db
        // Create with explicit document ID
        await AddSession(johnSmithPatientPrivate, sessionA);
        await AddSession(johnSmithPatientPrivate, sessionB);
        await AddSession(johnSmithPatientPrivate, sessionWithNoEvaluations);

        // Add evaluations to db
        await AddEvaluation(johnSmithPatientPrivate, sessionA, sessionAPreEval);
        await AddEvaluation(johnSmithPatientPrivate, sessionA, sessionAPostEval);
        await AddEvaluation(johnSmithPatientPrivate, sessionB, sessionBPreEval);

        // _factory = new WebApplicationFactory<Program>();
        // _client = _factory.CreateClient();

        // Print collection to db for debugging
        await PrintCollectionContents(SessionController.COLLECTION_NAME);
        await PrintCollectionContents(PatientEvaluationController.COLLECTION_NAME);
    }


    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        _client.Dispose();
        _testServer.Dispose();
        // _factory.DisposeAsync();
        await _integrationTestDataController.ClearFirestoreEmulatorDataAsync();
    }

    public SessionControllerTests()
    {
        _testServer = new TestServer(new WebHostBuilder()
            .ConfigureAppConfiguration((context, builder) => { builder.AddJsonFile("appsettings.Emulator.json"); })
            .UseStartup<StartupEmulator>());

        // instantiate db
        _firestoreDb = new FirestoreDbBuilder
        {
            ProjectId = "hippotherapy",
            EmulatorDetection = EmulatorDetection.EmulatorOnly
        }.Build();

        _client = _testServer.CreateClient();
    }

    private readonly TestServer _testServer;

    private SessionController _sessionController;

    private readonly HttpClient _client;

    // private WebApplicationFactory<Program> _factory;
    private readonly FirestoreDb _firestoreDb;
    private IntegrationTestDataController _integrationTestDataController;

    private string patientWithSessionsID;
    private string patientWithNoSessionsID;

    private string sessionAWithPrePostEvalsID;
    private string evalA_PreID;
    private string evalA_PostID;

    private string sessionBWithPreEvalID;
    private string evalB_PreID;

    private string sessionCWithNoEvals;


    private async Task AddPatient(PatientPrivate patient)
    {
        Console.WriteLine("patient id: " + patient.Id);
        Console.WriteLine("patient id: " + patientWithSessionsID);
        CollectionReference? patientCollection = _firestoreDb.Collection(PatientController.COLLECTION_NAME);
        await patientCollection.Document(patient.Id).SetAsync(patient);
    }


    private async Task AddSession(PatientPrivate patient, Session session)
    {
        CollectionReference? patientCollection = _firestoreDb.Collection(PatientController.COLLECTION_NAME);
        CollectionReference? sessionCollection =
            patientCollection.Document(patient.Id).Collection(SessionController.COLLECTION_NAME);
        await sessionCollection.Document(session.SessionID).CreateAsync(session);
    }


    private async Task AddEvaluation(PatientPrivate patient, Session session, PatientEvaluation evaluation)
    {
        DocumentReference? patientCollection =
            _firestoreDb.Collection(PatientController.COLLECTION_NAME).Document(patient.Id);
        DocumentReference? sessionCollection =
            patientCollection.Collection(SessionController.COLLECTION_NAME).Document(session.SessionID);
        CollectionReference? evaluationCollection =
            sessionCollection.Collection(PatientEvaluationController.COLLECTION_NAME);
        await evaluationCollection.Document(evaluation.EvaluationID).SetAsync(evaluation);
    }


    private async Task DeleteAllCollections(FirestoreDb firestoreDb)
    {
        CollectionReference? patientCollection = firestoreDb.Collection(PatientController.COLLECTION_NAME);

        // clear documents in the collection before each test
        List<DocumentReference>? patients = patientCollection.ListDocumentsAsync().ToListAsync().Result;
        foreach (DocumentReference? document in patients)
        {
            // Delete all sessions
            CollectionReference? sessionCollection =
                patientCollection.Document(document.Id).Collection(SessionController.COLLECTION_NAME);
            List<DocumentReference>? sessions = await sessionCollection.ListDocumentsAsync().ToListAsync();
            foreach (DocumentReference? session in sessions) await session.DeleteAsync();

            // Delete all evaluations
            CollectionReference? evaluationCollection = patientCollection.Document(document.Id)
                .Collection(PatientEvaluationController.COLLECTION_NAME);
            List<DocumentReference>? evals = await evaluationCollection.ListDocumentsAsync().ToListAsync();
            foreach (DocumentReference? eval in evals) await eval.DeleteAsync();

            // Delete patient document
            await document.DeleteAsync();
        }
    }


    /// <summary>
    ///     A helper method to print out the session data.
    /// </summary>
    /// <param name="collectionName"></param>
    private async Task PrintCollectionContents(string collectionName)
    {
        Console.WriteLine($"\nContents of {collectionName} collection:");
        QuerySnapshot? querySnapshot = await _firestoreDb.Collection(collectionName).GetSnapshotAsync();
        foreach (DocumentSnapshot? document in querySnapshot.Documents)
        {
            Console.WriteLine($"\nSession ID: {document.Id}");
            foreach (KeyValuePair<string, object> field in document.ToDictionary())
                Console.WriteLine($"{field.Key}: {field.Value}");
        }
    }


    [Test]
    public async Task TestGetAllSessionsReturnsStatusCode200AndSessionsForPatientWithSessions()
    {
        OkObjectResult? result = await _sessionController.GetAllSessions(patientWithSessionsID) as OkObjectResult;
        List<Session> sessions = result.Value as List<Session>;
        Assert.AreEqual(3, sessions.Count);
        Assert.AreEqual(200, result.StatusCode);

        Assert.AreEqual(sessions[0].SessionID, sessionAWithPrePostEvalsID);
        Assert.AreEqual(sessions[1].SessionID, sessionBWithPreEvalID);
    }


    [Test]
    public async Task TestGetAllSessionsReturnsStatusCode206IfNoSessionsExist()
    {
        NoContentResult? result = await _sessionController.GetAllSessions(patientWithNoSessionsID) as NoContentResult;
        Assert.AreEqual(204, result.StatusCode);
    }


    [Test]
    public async Task TestGetAllSessionsReturnsStatusCode404IfPatientDoesNotExist()
    {
        NotFoundResult? result =
            await _sessionController.GetAllSessions("not-in-db-patient-asfasdfwawf") as NotFoundResult;
        Assert.AreEqual(404, result.StatusCode);
    }


    [Test]
    public async Task TestGetPrePostEvaluationsReturnsStatusCode200IfBothEvaluationsExists()
    {
        ObjectResult? result =
            await _sessionController.GetPrePostEvaluations(patientWithSessionsID, sessionAWithPrePostEvalsID)
                as ObjectResult;

        Assert.NotNull(result);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        List<PatientEvaluation>? evaluations = result.Value as List<PatientEvaluation>;
        Assert.AreEqual(2, evaluations.Count);

        // Always returned with pre eval first then post.
        Assert.AreEqual(evalA_PreID, evaluations[0].EvaluationID);
        Assert.AreEqual(evalA_PostID, evaluations[1].EvaluationID);
    }


    [Test]
    public async Task TestGetPrePostEvaluationsReturnsStatusCode206IfOnlyOneEvaluationsExists()
    {
        ObjectResult? result =
            await _sessionController.GetPrePostEvaluations(patientWithSessionsID,
                sessionBWithPreEvalID) as ObjectResult;


        Assert.NotNull(result);
        List<PatientEvaluation> evaluations = result.Value as List<PatientEvaluation>;
        Assert.AreEqual(1, evaluations.Count);
        Assert.AreEqual(evalB_PreID, evaluations[0].EvaluationID);
        Assert.AreEqual(206, result.StatusCode);
    }


    [Test]
    public async Task TestGetPrePostEvaluationsReturnsStatusCode204IfNoEvaluationsExist()
    {
        NoContentResult? result =
            await _sessionController.GetPrePostEvaluations(patientWithSessionsID, sessionCWithNoEvals) as
                NoContentResult;
        Assert.AreEqual(204, result.StatusCode);
    }


    [Test]
    public async Task TestGetPrePostEvaluationsReturnsStatusCode404IfPatientDoesNotExist()
    {
        NotFoundResult? result =
            await _sessionController.GetPrePostEvaluations("patient-not-in-db", sessionCWithNoEvals) as NotFoundResult;
        Assert.AreEqual(404, result.StatusCode);
    }
}