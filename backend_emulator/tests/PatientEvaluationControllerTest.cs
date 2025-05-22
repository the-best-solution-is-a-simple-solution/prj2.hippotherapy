using System.Net;
using System.Text;
using Google.Api.Gax;
using Google.Cloud.Firestore;
using HippoApi;
using HippoApi.Controllers;
using HippoApi.Models;
using HippoApi.Models.custom_responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace tests;

[TestFixture]
public class PatientEvaluationControllerTest : IDisposable
{
    private List<string> tagList = new List<string>
    {
        "sick",
        "tired",
        "injured",
        "unwell",
        "uncooperative",
        "weather",
        "pain",
        "medication",
        "seizure",
        "other"
    };
    
    [OneTimeSetUp]
    public async Task SetupAsync()
    {
        // Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", "localhost:8080");

        // instantiate db
        // _firestoreDb = new FirestoreDbBuilder
        // {
        //     ProjectId = "test-project-id",
        //     EmulatorDetection = EmulatorDetection.EmulatorOnly
        // }.Build();

        _patientEvaluationController = new PatientEvaluationController(_firestoreDb);
        // _factory = new WebApplicationFactory<Program>();
        // _client = _factory.CreateClient();
        _integrationTestDataController = new IntegrationTestDataController(_firestoreDb);

        await _integrationTestDataController.ClearFirestoreEmulatorDataAsync();

        // Session setup
        _johnSmithPatientPrivate = new PatientPrivate
        {
            Id = "patient-with-sessions-85a79df5-0cc9-4539-9b1e-1db2c1163fe1",
            FName = "John",
            LName = "Smith",
            Condition = "Autism",
            Phone = "555-012-3456",
            Age = 14,
            Weight = 45,
            Height = 145,
            Email = "asafsfas@gmail.com",
            DoctorPhoneNumber = "555-123-4567",
            GuardianPhoneNumber = "555-555-6666", // Guardian needed
            TherapistID = "default"
        };

        await IntegrationTestDataController.AddPatient(_johnSmithPatientPrivate);

        // integration session data

        Session session = new()
        {
            SessionID = "session-with-pre-eval",
            PatientID = _johnSmithPatientPrivate.Id,
            DateTaken = new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Location = "AB"
        };

        Session sessionWithNoEval = new()
        {
            SessionID = "sessionWithNoEval",
            PatientID = _johnSmithPatientPrivate.Id,
            DateTaken = new DateTime(2020, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Location = "ZZ"
        };

        await IntegrationTestDataController.AddSession(_johnSmithPatientPrivate, session);
        await IntegrationTestDataController.AddSession(_johnSmithPatientPrivate, sessionWithNoEval);


        _testEvaluationValid = new PatientEvaluation
        {
            SessionID = session.SessionID,
            EvaluationID = "evaluation-valid",
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

        await IntegrationTestDataController.AddEvaluation(_johnSmithPatientPrivate, _testEvaluationValid, session);
    }

    private readonly TestServer testServer;

    public PatientEvaluationControllerTest()
    {
        testServer = new TestServer(new WebHostBuilder()
            .ConfigureAppConfiguration((context, builder) => { builder.AddJsonFile("appsettings.Emulator.json"); })
            .UseStartup<StartupEmulator>());

        // instantiate db
        _firestoreDb = new FirestoreDbBuilder
        {
            ProjectId = "hippotherapy",
            EmulatorDetection = EmulatorDetection.EmulatorOnly
        }.Build();

        _client = testServer.CreateClient();
    }

    public void Dispose()
    {
        testServer.Dispose();
        _client.Dispose();
    }


    private PatientEvaluationController _patientEvaluationController;
    private readonly HttpClient _client;
    private WebApplicationFactory<Program> _factory;
    private PatientEvaluation _testEvaluationValid;
    private readonly FirestoreDb _firestoreDb;
    private IntegrationTestDataController _integrationTestDataController;
    private PatientPrivate _johnSmithPatientPrivate;


    [Test]
    public async Task TestThatCreatingPreEvaluationFailsWithNoAssociatedSession()
    {
        _testEvaluationValid.SessionID = "garbagseges";
        StringContent content = new(JsonConvert.SerializeObject(_testEvaluationValid), Encoding.UTF8,
            "application/json");

        HttpResponseMessage response =
            await _client.PostAsync(
                "http://localhost:5000/patientevaluation/patient-with-sessions-85a79df5-0cc9-4539-9b1e-1db2c1163fe1/submit-evaluation",
                content);

        Assert.IsNotNull(response);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }


    [Test]
    public async Task TestThatCreatingPostWithNoPreEvaluationFails()
    {
        _testEvaluationValid.SessionID = "sessionWithNoEval";
        _testEvaluationValid.EvalType = "post";
        _testEvaluationValid.EvaluationID = "something different";


        ObjectResult? response =
            await _patientEvaluationController.CreatePatientEvaluation(_testEvaluationValid,
                _johnSmithPatientPrivate.Id) as ObjectResult;

        Assert.IsNotNull(response);
        Console.WriteLine(_johnSmithPatientPrivate.Id);
        Console.WriteLine();
        Assert.That(response.StatusCode, Is.EqualTo(400));
    }


    [Test]
    [Order(1)]
    public async Task TestThatCreatingPreEvaluationForExistingEmptySessionWorks()
    {
        string generatedPatientD = Guid.NewGuid().ToString();
        PatientPrivate p = new()
        {
            Id = generatedPatientD,
            FName = "A",
            LName = "B",
            Phone = "123-456-7890",
            Age = 50,
            Email = "asafsfas@gmail.com",
            DoctorPhoneNumber = "555-123-4567",
            Condition = "something"
        };
        await IntegrationTestDataController.AddPatient(p);

        string generatedID = Guid.NewGuid().ToString();

        Session s = new()
        {
            PatientID = generatedPatientD,
            SessionID = generatedID,
            DateTaken = new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Location = "ZZ"
        };
        await IntegrationTestDataController.AddSession(p, s);

        PatientEvaluation e = new()
        {
            SessionID = generatedID,
            EvalType = "pre",
            Lumbar = 2,
            HipFlex = 1,
            HeadAnt = 0,
            HeadLat = -1,
            KneeFlex = -2,
            Pelvic = -1,
            PelvicTilt = 0,
            Thoracic = 1,
            Trunk = 1,
            TrunkInclination = 1,
            ElbowExtension = 0
        };

        ObjectResult? res =
            await _patientEvaluationController.CreatePatientEvaluation(e, generatedPatientD) as ObjectResult;

        Assert.IsNotNull(res);
        Assert.That(res.StatusCode, Is.EqualTo(200));
    }


    [Test]
    public async Task TestGetEvaluationByIdReturnsStatus200IfSuccessful()
    {
        // add it directly to firebase, and then try a retrieval based on it's ID
        DocumentReference? addedEval =
            await _firestoreDb.Collection(PatientEvaluationController.COLLECTION_NAME).AddAsync(_testEvaluationValid);


        ObjectResult? result = await _patientEvaluationController
                .GetEvaluationById(addedEval.GetSnapshotAsync().Result.ConvertTo<PatientEvaluation>().EvaluationID)
            as ObjectResult;

        Assert.NotNull(result);
        Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
    }


    [Test]
    public async Task TestGetEvaluationByIdReturnsStatus404IfNotFound()
    {
        const string evaluationUUID = "test-session-does-not-exist";
        ObjectResult? result = await _patientEvaluationController.GetEvaluationById(evaluationUUID) as ObjectResult;

        Assert.AreEqual(404, result.StatusCode);
    }


    [Test]
    public async Task TestThatCreatingPostEvaluationWithAPreEvaluationWorks()
    {
        string generatedPatientID = Guid.NewGuid().ToString();
        PatientPrivate p = new()
        {
            Id = generatedPatientID,
            FName = "A",
            LName = "B",
            Phone = "123-456-7890",
            Age = 50,
            Email = "asafsfas@gmail.com",
            DoctorPhoneNumber = "555-123-4567",
            Condition = "something"
        };
        await IntegrationTestDataController.AddPatient(p);

        string generatedSessionID = Guid.NewGuid().ToString();

        Session s = new()
        {
            PatientID = generatedPatientID,
            SessionID = generatedSessionID,
            DateTaken = new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Location = "ZZ"
        };
        await IntegrationTestDataController.AddSession(p, s);

        string generatedEvaluationID = Guid.NewGuid().ToString();
        PatientEvaluation e = new()
        {
            EvaluationID = generatedEvaluationID,
            SessionID = generatedSessionID,
            EvalType = "pre",
            Lumbar = 2,
            HipFlex = 1,
            HeadAnt = 0,
            HeadLat = -1,
            KneeFlex = -2,
            Pelvic = -1,
            PelvicTilt = 0,
            Thoracic = 1,
            Trunk = 1,
            TrunkInclination = 1,
            ElbowExtension = 0
        };
        await IntegrationTestDataController.AddEvaluation(p, e, s);

        e.EvalType = "post";
        e.EvaluationID = generatedEvaluationID;

        OkObjectResult? res =
            await _patientEvaluationController.CreatePatientEvaluation(e, generatedPatientID) as OkObjectResult;

        Assert.IsNotNull(res);
        Assert.That(res.StatusCode, Is.EqualTo(200));
    }

    // ----------Tests for GetAllEvaluationDataForGraph----------//
    [Test]
    public async Task TestGetAllEvaluationDataForGraphReturnsStatus200IfSuccessfulAndAllData()
    {
        string albertTwoevalsUUID = "albert-twoevals-d73178aaaf-erw71-eee193ab4711";
        // add patient with no sessions or evaluations
        PatientPrivate albertTwoevalsPatientPrivate = new()
        {
            Id = albertTwoevalsUUID,
            FName = "Albert",
            LName = "Twoevals",
            Condition = "Autism",
            Phone = "555-012-3456",
            Age = 14,
            Weight = 45,
            Height = 145,
            Email = "asafsfas@gmail.com",
            DoctorPhoneNumber = "555-123-4567",
            GuardianPhoneNumber = "555-555-6666", // Guardian needed
            TherapistID = "default"
        };

        string sessionAWithPrePostEvalsID = "albert-twoevals-test-sessionA_ae4051ae-9165-48ac-a00f-0cajr222c229d55";
        string evalA_PreID = "albert-twoevals-test-evalPreA_35131add-5ee6-481e-82datd-32b8d5e7f47f6";
        string evalA_PostID = "albert-twoevals-test-evalPostA_0328357f-6adc-447c-bafs4f-6c8a01cf739a9";

        // integration session data
        Session sessionA = new()
        {
            SessionID = sessionAWithPrePostEvalsID,
            PatientID = albertTwoevalsUUID,
            DateTaken = new DateTime(2022, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Location = "NA"
        };

        PatientEvaluation sessionAPreEval = new()
        {
            SessionID = sessionA.SessionID,
            EvaluationID = evalA_PreID,
            EvalType = "pre",
            HeadLat = 0,
            HeadAnt = 0,
            ElbowExtension = 1,
            HipFlex = 0,
            KneeFlex = 0,
            Lumbar = 0,
            Pelvic = 0,
            PelvicTilt = 0,
            Thoracic = 0,
            Trunk = 0,
            TrunkInclination = 0
        };

        PatientEvaluation sessionAPostEval = new()
        {
            SessionID = sessionA.SessionID,
            EvaluationID = evalA_PostID,
            EvalType = "post",
            HeadLat = 0,
            HeadAnt = 0,
            ElbowExtension = -1,
            HipFlex = 0,
            KneeFlex = -1,
            Lumbar = 0,
            Pelvic = 0,
            PelvicTilt = 0,
            Thoracic = 0,
            Trunk = 0,
            TrunkInclination = 0
        };

        string sessionBWithPreEvalID = "albert-twoevals-test-sessionB_ae4051ae-9165-48ac-a00f-0cajr222c229d55";
        string evalB_PreID = "albert-twoevals-test-evalPreB_35131add-5ee6-481e-82datd-32b8d5e7f47f6";

        // integration session data
        Session sessionB = new()
        {
            SessionID = sessionBWithPreEvalID,
            PatientID = albertTwoevalsUUID,
            DateTaken = new DateTime(2023, 1, 14, 0, 0, 0, DateTimeKind.Utc),
            Location = "NA"
        };

        PatientEvaluation sessionBPreEval = new()
        {
            SessionID = sessionB.SessionID,
            EvaluationID = evalB_PreID,
            EvalType = "pre",
            HeadLat = 2,
            HeadAnt = 2,
            ElbowExtension = 2,
            HipFlex = 2,
            KneeFlex = 2,
            Lumbar = 2,
            Pelvic = 2,
            PelvicTilt = 2,
            Thoracic = 2,
            Trunk = 2,
            TrunkInclination = 2
        };
        // Add patient 2 sessions and 3 evaluations
        await IntegrationTestDataController.AddPatient(albertTwoevalsPatientPrivate);
        await IntegrationTestDataController.AddSession(albertTwoevalsPatientPrivate, sessionA);
        await IntegrationTestDataController.AddEvaluation(albertTwoevalsPatientPrivate, sessionAPreEval, sessionA);
        await IntegrationTestDataController.AddEvaluation(albertTwoevalsPatientPrivate, sessionAPostEval, sessionA);
        await IntegrationTestDataController.AddSession(albertTwoevalsPatientPrivate, sessionB);
        await IntegrationTestDataController.AddEvaluation(albertTwoevalsPatientPrivate, sessionBPreEval, sessionB);

        ObjectResult? result =
            await _patientEvaluationController.GetAllEvaluationDataForGraph(albertTwoevalsUUID) as ObjectResult;

        // Should be status 200 - success
        Assert.True(result is ObjectResult);
        Assert.AreEqual(200, result.StatusCode);

        List<EvaluationGraphData>? resultContent = result.Value as List<EvaluationGraphData>;
        Assert.True(result.Value is List<EvaluationGraphData>);

        // Check values for session A pre evaluation
        Assert.AreEqual(evalA_PreID, resultContent[0].EvaluationID);
        Assert.AreEqual("pre", resultContent[0].EvalType);
        Assert.AreEqual(1, resultContent[0].ElbowExtension);
        Assert.AreEqual(0, resultContent[0].HipFlex);
        Assert.AreEqual(0, resultContent[0].KneeFlex);

        // Check values for session A post evaluation
        Assert.AreEqual(evalA_PostID, resultContent[1].EvaluationID);
        Assert.AreEqual("post", resultContent[1].EvalType);
        Assert.AreEqual(-1, resultContent[1].ElbowExtension);
        Assert.AreEqual(0, resultContent[1].HipFlex);
        Assert.AreEqual(-1, resultContent[1].KneeFlex);

        // Check values for session B pre evaluation
        Assert.AreEqual(evalB_PreID, resultContent[2].EvaluationID);
        Assert.AreEqual("pre", resultContent[2].EvalType);
        Assert.AreEqual(2, resultContent[2].ElbowExtension);
        Assert.AreEqual(2, resultContent[2].HipFlex);
        Assert.AreEqual(2, resultContent[2].KneeFlex);
    }


    [Test]
    public async Task TestGetAllEvaluationDataForGraphReturnsStatus404IfPatientNotFound()
    {
        const string patientUUID = "test-session-does-not-exist-afwaefwa9252";
        NotFoundResult? result =
            await _patientEvaluationController.GetAllEvaluationDataForGraph(patientUUID) as NotFoundResult;

        // Status 404 - patient should not exist
        Assert.AreEqual(404, result.StatusCode);
    }

    [Test]
    public async Task TestGetAllEvaluationDataForGraphReturnsStatus200IfFoundPatientButOnlyOneEvaluation()
    {
        string albertOneevalUUID = "albert-oneeval-d731784e-4ecb-4a2c-a571-eee193ab4711";
        // add patient with one session and evaluation
        PatientPrivate albertOneevalPatientPrivate = new()
        {
            Id = albertOneevalUUID,
            FName = "Albert",
            LName = "Oneeval",
            Condition = "Autism",
            Phone = "555-012-3456",
            Age = 14,
            Weight = 45,
            Height = 145,
            Email = "asafsfas@gmail.com",
            DoctorPhoneNumber = "555-123-4567",
            GuardianPhoneNumber = "555-555-6666", // Guardian needed
            TherapistID = "default"
        };

        string sessionAWithPrePostEvalsID = "test-sessionA_0c222c229d55";
        string evalA_PreID = "test-evalPreA_32b8d57f47f6";
        string evalA_PostID = "test-evalPostA_6c801cf739a9";

        // integration session data
        Session sessionA = new()
        {
            SessionID = sessionAWithPrePostEvalsID,
            PatientID = albertOneevalUUID,
            DateTaken = new DateTime(2022, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Location = "NA"
        };

        PatientEvaluation sessionAPreEval = new()
        {
            SessionID = sessionA.SessionID,
            EvaluationID = evalA_PreID,
            EvalType = "pre",
            HeadLat = 0,
            HeadAnt = 0,
            ElbowExtension = 1,
            HipFlex = 0,
            KneeFlex = 0,
            Lumbar = 0,
            Pelvic = 0,
            PelvicTilt = 0,
            Thoracic = 0,
            Trunk = 0,
            TrunkInclination = 0
        };


        await IntegrationTestDataController.AddPatient(albertOneevalPatientPrivate);
        await IntegrationTestDataController.AddSession(albertOneevalPatientPrivate, sessionA);
        await IntegrationTestDataController.AddEvaluation(albertOneevalPatientPrivate, sessionAPreEval, sessionA);

        // call method
        ObjectResult? result =
            await _patientEvaluationController.GetAllEvaluationDataForGraph(albertOneevalUUID) as ObjectResult;
        Assert.AreEqual(200, result.StatusCode);
    }

    [Test]
    public async Task TestGetAllEvaluationDataForGraphReturnsStatus204IfFoundPatientButNoEvaluationData()
    {
        string albertNoevalsUUID = "albert-noevals-d731784e-4ecb-4a2c-a571-eee193ab4711";
        // add patient with no sessions or evaluations
        PatientPrivate albertNoevalsPatientPrivate = new()
        {
            Id = albertNoevalsUUID,
            FName = "Albert",
            LName = "Noevals",
            Condition = "Autism",
            Phone = "555-012-3456",
            Age = 14,
            Weight = 45,
            Height = 145,
            Email = "asafsfas@gmail.com",
            DoctorPhoneNumber = "555-123-4567",
            GuardianPhoneNumber = "555-555-6666", // Guardian needed
            TherapistID = "default"
        };
        await IntegrationTestDataController.AddPatient(albertNoevalsPatientPrivate);

        // call method
        NoContentResult? result =
            await _patientEvaluationController.GetAllEvaluationDataForGraph(albertNoevalsUUID) as NoContentResult;

        // check result is no content
        Assert.AreEqual(204, result.StatusCode);
    }
    
    #region notes and tags

    [Test]
    public async Task TestThatTagsAreExtractedFromNotes()
    {
        string testText = $"test text #{tagList[0]} this should #{tagList[1]} to grab the tags";
        List<string> notesTagList = await _patientEvaluationController.GetEvaluationsTagsFromNotes(testText);
        Assert.IsTrue(notesTagList.Contains(tagList[0]));
        Assert.IsTrue(notesTagList.Contains(tagList[1]));
    }

    [Test]
    public async Task TestThatTiredTagIsExtractedFromNotes()
    {
        string testText = $"test text #{tagList[1]} this should work to grab the tags";
        List<string> notesTagList = await _patientEvaluationController.GetEvaluationsTagsFromNotes(testText);
        Assert.IsTrue(notesTagList.Contains(tagList[1]));
    }
    
    [Test]
    public async Task TestThatInjuredTagIsExtractedFromNotes()
    {
        string testText = $"test text #{tagList[2]} this should work to grab the tags";
        List<string> notesTagList = await _patientEvaluationController.GetEvaluationsTagsFromNotes(testText);
        Assert.IsTrue(notesTagList.Contains(tagList[2]));
    }
    
    [Test]
    public async Task TestThatUnwellTagIsExtractedFromNotes()
    {
        string testText = $"test text #{tagList[3]} this should work to grab the tags";
        List<string> notesTagList = await _patientEvaluationController.GetEvaluationsTagsFromNotes(testText);
        Assert.IsTrue(notesTagList.Contains(tagList[3]));
    }
    
    [Test]
    public async Task TestThatUncooperativeTagIsExtractedFromNotes()
    {
        string testText = $"test text #{tagList[4]} this should work to grab the tags";
        List<string> notesTagList = await _patientEvaluationController.GetEvaluationsTagsFromNotes(testText);
        Assert.IsTrue(notesTagList.Contains(tagList[4]));
    }
    
    [Test]
    public async Task TestThatWeatherTagIsExtractedFromNotes()
    {
        string testText = $"test text #{tagList[5]} this should work to grab the tags";
        List<string> notesTagList = await _patientEvaluationController.GetEvaluationsTagsFromNotes(testText);
        Assert.IsTrue(notesTagList.Contains(tagList[5]));
    }
    
    [Test]
    public async Task TestThaPainTagIsExtractedFromNotes()
    {
        string testText = $"test text #{tagList[6]} this should work to grab the tags";
        List<string> notesTagList = await _patientEvaluationController.GetEvaluationsTagsFromNotes(testText);
        Assert.IsTrue(notesTagList.Contains(tagList[6]));
    }
    
    [Test]
    public async Task TestThatMedicationTagIsExtractedFromNotes()
    {
        string testText = $"test text #{tagList[7]} this should work to grab the tags";
        List<string> notesTagList = await _patientEvaluationController.GetEvaluationsTagsFromNotes(testText);
        Assert.IsTrue(notesTagList.Contains(tagList[7]));
    }
    
    [Test]
    public async Task TestThatSeizureTagIsExtractedFromNotes()
    {
        string testText = $"test text #{tagList[8]} this should work to grab the tags";
        List<string> notesTagList = await _patientEvaluationController.GetEvaluationsTagsFromNotes(testText);
        Assert.IsTrue(notesTagList.Contains(tagList[8]));
    }
    [Test]
    public async Task TestThatOtherTagIsExtractedFromNotes()
    {
        string testText = $"test text #{tagList[9]} this should work to grab the tags";
        List<string> notesTagList = await _patientEvaluationController.GetEvaluationsTagsFromNotes(testText);
        Assert.IsTrue(notesTagList.Contains(tagList[9]));
    }
    
    
    [Test]
    public async Task TestThatDuplicateTagsAreNotCountedFromNotes()
    {
        string testText = $"test text #{tagList[1]} this should #{tagList[2]} to grab the tags #{tagList[1]} test";
        List<string> notesTagList = await _patientEvaluationController.GetEvaluationsTagsFromNotes(testText);
        Assert.IsTrue(notesTagList.Contains(tagList[1]));
        Assert.IsTrue(notesTagList.Contains(tagList[2]));
        Assert.That(notesTagList.Count, Is.EqualTo(2));
    }
    
    [Test]
    public async Task TestThatNotesWithNoTagsReturnsEmptyList()
    {
        string testText = $"test text {tagList[0]} this should {tagList[2]} to grab the tags {tagList[2]} test";
        List<string> notesTagList = await _patientEvaluationController.GetEvaluationsTagsFromNotes(testText);
        Assert.IsFalse(notesTagList.Contains(tagList[0]));
        Assert.IsFalse(notesTagList.Contains(tagList[2]));
        Assert.That(notesTagList.Count, Is.EqualTo(0));
    }
    
    [Test]
    public async Task TestThatEmptyNotesReturnsEmptyList()
    {
        string testText = "";
        List<string> notesTagList = await _patientEvaluationController.GetEvaluationsTagsFromNotes(testText);
        Assert.That(notesTagList.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task TestThatTagListWithHashAtEndExtracts()
    {
        string testText = $"test #{tagList[3]} Only first one should be extracted#";
        List<string> notesTagList = await _patientEvaluationController.GetEvaluationsTagsFromNotes(testText);
        Assert.That(notesTagList.Count, Is.EqualTo(1));
        Assert.IsTrue(notesTagList.Contains(tagList[3]));
    }
    
    [Test]
    public async Task TestThatEmptyTagInListDoesNotExtract()
    {
        string testText = $"test #{tagList[3]} Only first one should be extracted # this hash should be skipped";
        List<string> notesTagList = await _patientEvaluationController.GetEvaluationsTagsFromNotes(testText);
        Assert.That(notesTagList.Count, Is.EqualTo(1));
        Assert.IsTrue(notesTagList.Contains(tagList[3]));
    }
    
    [Test]
    public async Task TestThatConsecutiveTagsAreExtracted()
    {
        string testText = $"test #{tagList[3]}#{tagList[4]} this should work to grab both tags";
        List<string> notesTagList = await _patientEvaluationController.GetEvaluationsTagsFromNotes(testText);
        Assert.That(notesTagList.Count, Is.EqualTo(2));
        Assert.IsTrue(notesTagList.Contains(tagList[3]));
        Assert.IsTrue(notesTagList.Contains(tagList[4]));
    }

    [Test]
    public async Task TestThatConsecutiveEmptyTagsAreNotExtracted()
    {
        string testText = $"test ## No tags should be extracted";
        List<string> notesTagList = await  _patientEvaluationController.GetEvaluationsTagsFromNotes(testText);
        Assert.That(notesTagList.Count, Is.EqualTo(0));
    }

    #endregion
}