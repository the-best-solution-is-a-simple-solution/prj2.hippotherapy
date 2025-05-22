using Google.Api.Gax;
using Google.Cloud.Firestore;
using HippoApi.Controllers;
using HippoApi.Models;
using Microsoft.AspNetCore.Mvc;
using tests.Models;

namespace tests;

[TestFixture]
public class ExportControllerTests
{
    [OneTimeSetUp]
    public async Task SetupAsync()
    {
        Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", "localhost:8080");

        // instantiate db
        _firestoreDb = new FirestoreDbBuilder
        {
            ProjectId = "test-project-id",
            EmulatorDetection = EmulatorDetection.EmulatorOnly
        }.Build();

        // Initialize variables
        _helper = new TestSeedDataHelper(_firestoreDb);
        _integrationTestDataController = new IntegrationTestDataController(_firestoreDb);
        _exportController = new ExportController(_firestoreDb);

        // Clear db
        await _integrationTestDataController.ClearFirestoreEmulatorDataAsync();

        // Seed db
        await SeedData();
    }

    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        await _integrationTestDataController.ClearFirestoreEmulatorDataAsync();
    }

    private ExportController _exportController;
    private FirestoreDb _firestoreDb;
    private IntegrationTestDataController _integrationTestDataController;
    private TestSeedDataHelper _helper;

    private Owner owner1;
    private Owner owner2;
    private Therapist therapist1o1;
    private Therapist therapist2o1;
    private Therapist therapist1o2;

    /// <summary>
    ///     Seed data for owners and therapists
    /// </summary>
    private async Task SeedData()
    {
        owner1 = _helper.GetTestOwner("export-owner1-id", "exporto1", "ownerone", "exportpage");
        owner2 = _helper.GetTestOwner("export-owner2-id", "exporto2", "ownertwo", "onetherapist");
        therapist1o1 = _helper.GetTestTherapist("export-t1o1-id", "exportt1o1", "etherapist", "etherapistlast");
        therapist2o1 = _helper.GetTestTherapist("export-t2o1-id", "exportt2o1", "etherapisttwo", "etherapisttwolast");
        therapist1o2 = _helper.GetTestTherapist("export-t1o2-id", "exportt1o2", "etherapisttwo", "etherapisttwolast");

        // Add to database using custom set Ids
        await _helper.CreateOwner(owner1);
        await _helper.CreateOwner(owner2);
        await _helper.CreateTherapist(owner1.OwnerId, therapist1o1);
        await _helper.CreateTherapist(owner1.OwnerId, therapist2o1);
        await _helper.CreateTherapist(owner2.OwnerId, therapist1o2);

        // Switched to putting all seed data in this class
        // await _integrationTestDataController.SeedPatientExportPageTestData();
        await SeedPatientExportInfo();
    }

    /// <summary>
    ///     Seed patient, session and evaluation data
    /// </summary>
    public async Task SeedPatientExportInfo()
    {
        string patientWithSessionsID = "patient-with-sessions-0cc9-4539-9b1e-1db2c1163fe1";
        string patientWithNoSessionsID = "patient-no-sessions-0cc9-4539-9b1e-1db2c1163fe1";

        // Add a few patients and sessions with evaluations
        PatientPrivate johnSmithPatientPrivate = new()
        {
            Id = patientWithSessionsID,
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
            TherapistID = therapist1o1.TherapistID
        };

        PatientPrivate aliceTailorPatientPrivate = new()
        {
            Id = patientWithNoSessionsID,
            FName = "Alice",
            LName = "Tailor",
            Condition = "Cerebral Palsy",
            Phone = "555-012-3456",
            Age = 25,
            Weight = 75,
            Height = 180,
            Email = "bibi@rocketmail.net",
            DoctorPhoneNumber = "555-123-4567",
            GuardianPhoneNumber = "555-555-6666", // Guardian needed
            TherapistID = therapist2o1.TherapistID
        };

        // UUID's generated from https://www.uuidgenerator.net/
        string sessionAWithPrePostEvalsID = "test-sessionA_e42a56a1-7abc-4b41-a48c-d2c6fd4bb482";
        string evalA_PreID = "test-evalPreA_1f2-ea55-479e-ba37-e5e31d1f6aa3";
        string evalA_PostID = "test-evalPostA_097af-ce43-4844-aa11-3161d934316";

        // integration session data
        Session sessionA = new()
        {
            SessionID = sessionAWithPrePostEvalsID,
            PatientID = patientWithSessionsID,
            DateTaken = DateTime.Parse("11-20-1990"),
            Location = "NA"
        };

        PatientEvaluation sessionAPreEval = new()
        {
            SessionID = sessionA.SessionID,
            EvaluationID = evalA_PreID,
            EvalType = "pre",
            HeadLat = 1,
            HeadAnt = 1,
            ElbowExtension = 1,
            HipFlex = 1,
            KneeFlex = 1,
            Lumbar = 0,
            Pelvic = 0,
            PelvicTilt = 0,
            Thoracic = -1,
            Trunk = -1,
            TrunkInclination = 0
        };

        PatientEvaluation sessionAPostEval = new()
        {
            SessionID = sessionA.SessionID,
            EvaluationID = evalA_PostID,
            EvalType = "post",
            HeadLat = 1,
            HeadAnt = 1,
            ElbowExtension = -1,
            HipFlex = 0,
            KneeFlex = -1,
            Lumbar = 1,
            Pelvic = 0,
            PelvicTilt = 0,
            Thoracic = 0,
            Trunk = 0,
            TrunkInclination = 0
        };

        // Session B with a pre and post eval
        string sessionCWithPreEvalsID = "test-sessionB_8f791aee-c24817-a804-a4695b93e279";
        string evalB_PreID = "test-evalB_125b4668-6ee3-460-41ca84ea6c5f";

        Session sessionB = new()
        {
            SessionID = sessionCWithPreEvalsID,
            PatientID = patientWithNoSessionsID,
            DateTaken = DateTime.Parse("11-20-2023"),
            Location = "CA"
        };

        PatientEvaluation sessionCPreEval = new()
        {
            SessionID = sessionB.SessionID,
            EvaluationID = evalB_PreID,
            EvalType = "pre",
            HeadLat = 1,
            HeadAnt = 1,
            ElbowExtension = 1,
            HipFlex = -1,
            KneeFlex = -1,
            Lumbar = -1,
            Pelvic = -1,
            PelvicTilt = -1,
            Thoracic = 0,
            Trunk = 0,
            TrunkInclination = 0
        };

        string sessionCWithPostEvalID = "test-sessionC-01819e41-4b81-44-4cf2f26edfe2";
        string evalC_PostID = "test-evalCPost-c05a4bac-04b6-4661-f972e333c9";

        Session sessionC = new()
        {
            SessionID = sessionCWithPostEvalID,
            PatientID = patientWithSessionsID,
            DateTaken = DateTime.Parse("11-20-2022"),
            Location = "LA"
        };

        PatientEvaluation sessionCPostEval = new()
        {
            SessionID = sessionC.SessionID,
            EvaluationID = evalC_PostID,
            EvalType = "post",
            HeadLat = 1,
            HeadAnt = 2,
            ElbowExtension = -1,
            HipFlex = 1,
            KneeFlex = -1,
            Lumbar = 0,
            Pelvic = 0,
            PelvicTilt = 0,
            Thoracic = 0,
            Trunk = 1,
            TrunkInclination = 0
        };

        // Add Patients to db
        await IntegrationTestDataController.AddPatient(johnSmithPatientPrivate);
        await IntegrationTestDataController.AddPatient(aliceTailorPatientPrivate);

        // Add session to db
        // Create with explicit document ID
        await IntegrationTestDataController.AddSession(johnSmithPatientPrivate, sessionA);
        await IntegrationTestDataController.AddSession(johnSmithPatientPrivate, sessionB);
        await IntegrationTestDataController.AddSession(aliceTailorPatientPrivate, sessionC);

        await IntegrationTestDataController.AddEvaluation(johnSmithPatientPrivate, sessionAPreEval, sessionA);
        await IntegrationTestDataController.AddEvaluation(johnSmithPatientPrivate, sessionAPostEval, sessionA);
        await IntegrationTestDataController.AddEvaluation(aliceTailorPatientPrivate, sessionCPreEval, sessionC);
        await IntegrationTestDataController.AddEvaluation(aliceTailorPatientPrivate, sessionCPostEval, sessionC);
    }

    [Test]
    public async Task TestExportControllerReturnsRecordsThatMatchesMultipleFilters()
    {
        OkObjectResult? result =
            await _exportController.GetRecords(owner1.OwnerId, patientName: "John Smith", condition: "Autism",
                    location: "NA") as
                OkObjectResult;
        Assert.AreEqual(200, result.StatusCode);
        List<List<string>> listOfRecords = result.Value as List<List<string>>;

        Assert.AreEqual(2, listOfRecords.Count);

        foreach (List<string> records in listOfRecords)
        {
            Assert.AreEqual("John Smith", records[0]);
            Assert.AreEqual("Autism", records[4]);
            Assert.AreEqual("NA", records[6]);
        }
    }

    [Test]
    public async Task TestExportControllerReturnsNoRecordsForNonMatchingFiltersStatus204()
    {
        NoContentResult? result =
            await _exportController.GetRecords(owner1.OwnerId, patientName: "asdasdasd", condition: "asdasdasd",
                    location: "asdasd") as
                NoContentResult;
        Assert.AreEqual(204, result.StatusCode);
    }

    [Test]
    public async Task TestExportControllerReturnsNoRecordsForOtherOwnerWithNoRecords()
    {
        NoContentResult? result = await _exportController.GetRecords(owner2.OwnerId) as NoContentResult;
        Assert.AreEqual(204, result.StatusCode);
    }

    [Test]
    public async Task TestExportControllerReturnsRecordsThatMatchesName()
    {
        OkObjectResult? result =
            await _exportController.GetRecords(owner1.OwnerId, patientName: "John Smith") as OkObjectResult;
        Assert.AreEqual(200, result.StatusCode);
        List<List<string>> listOfRecords = result.Value as List<List<string>>;

        Assert.AreEqual(2, listOfRecords.Count);

        foreach (List<string> records in listOfRecords) Assert.AreEqual("John Smith", records[0]);
    }

    [Test]
    public async Task TestExportControllerReturnsRecordsThatMatchesCondition()
    {
        OkObjectResult? result =
            await _exportController.GetRecords(owner1.OwnerId, condition: "Cerebral Palsy") as OkObjectResult;
        Assert.AreEqual(200, result.StatusCode);
        List<List<string>> listOfRecords = result.Value as List<List<string>>;

        Assert.AreEqual(2, listOfRecords.Count);

        foreach (List<string> records in listOfRecords) Assert.AreEqual("Cerebral Palsy", records[4]);
    }

    [Test]
    public async Task TestExportControllerReturnsRecordsThatMatchesLocation()
    {
        OkObjectResult? result = await _exportController.GetRecords(owner1.OwnerId, "NA") as OkObjectResult;
        Console.WriteLine(result.Value);
        Assert.AreEqual(200, result.StatusCode);
        List<List<string>> listOfRecords = result.Value as List<List<string>>;

        Assert.AreEqual(2, listOfRecords.Count);

        foreach (List<string> records in listOfRecords) Assert.AreEqual("NA", records[6]);
    }


    [Test]
    public async Task TestExportControllerReturnsRecordsThatMatchesBetweenDateTime()
    {
        OkObjectResult? result =
            await _exportController.GetRecords(owner1.OwnerId, dateTime: "2022-11-20,2022-11-21") as OkObjectResult;
        Assert.AreEqual(200, result.StatusCode);
        List<List<string>> listOfRecords = result.Value as List<List<string>>;

        Assert.AreEqual(2, listOfRecords.Count);

        foreach (List<string> records in listOfRecords)
            Assert.IsTrue(records[5] == "2022-11-20" ||
                          records[5] == "2022-11-21" ||
                          records[5] == "11/20/2022" ||
                          records[5] == "11/21/2022");
    }

    [Test]
    // integration that GetUniqueNames returns a list of unique names
    public async Task TestGetUniqueNamesReturnsOkResultWithUniqueNames()
    {
        OkObjectResult? result = await _exportController.GetUniqueNames(owner1.OwnerId) as OkObjectResult;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        List<string>? uniqueNames = result.Value as List<string>;
        Assert.IsNotNull(uniqueNames);
        Assert.True(uniqueNames.Count == 2);
        Assert.Contains("John Smith", uniqueNames);
        Assert.Contains("Alice Tailor", uniqueNames);
        foreach (string? name in uniqueNames) Console.WriteLine(name);
    }

    [Test]
    public async Task TestGetUniqueNamesReturnsNotFoundResultForDifferentOwnerWithNoPatients()
    {
        IActionResult result = await _exportController.GetUniqueNames(owner2.OwnerId);
        Assert.IsInstanceOf<NotFoundResult>(result);
    }

    [Test]
    public async Task TestGetUniqueConditionsReturnsOkResultWithUniqueConditions()
    {
        OkObjectResult? result = await _exportController.GetUniqueConditions(owner1.OwnerId) as OkObjectResult;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(200, result.StatusCode);
        List<string>? uniqueConditions = result.Value as List<string>;
        Assert.IsNotNull(uniqueConditions);
        Assert.AreEqual(2, uniqueConditions.Count);
        Assert.Contains("Autism", uniqueConditions);
        Assert.Contains("Cerebral Palsy", uniqueConditions);
    }

    [Test]
    public async Task TestGetUniqueLocationsAcrossAllSessions()
    {
        OkObjectResult? result = await _exportController.GetUniqueLocations(owner1.OwnerId) as OkObjectResult;
        List<string> locations = result.Value as List<string>;
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual(3, locations.Count);
        Assert.Contains("NA", locations);
        Assert.Contains("CA", locations);
        Assert.Contains("LA", locations);
    }


    [Test]
    public async Task TestGetLowestYearAcrossAllSessions()
    {
        OkObjectResult? result = await _exportController.GetLowestYear(owner1.OwnerId) as OkObjectResult;
        Assert.AreEqual(200, result.StatusCode);
        int lowestYear = (int)result.Value;
        Assert.That(lowestYear, Is.EqualTo(1990));
    }
}