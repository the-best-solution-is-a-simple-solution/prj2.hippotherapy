using Google.Api.Gax;
using Google.Cloud.Firestore;
using HippoApi.Controllers;
using HippoApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace tests;

[TestFixture]
public class ArchiveControllerTests
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

        _integrationTestDataController = new IntegrationTestDataController(_firestoreDb);

        await _integrationTestDataController.ClearFirestoreEmulatorDataAsync();

        _integrationTestDataController = new IntegrationTestDataController(_firestoreDb);
        _archiveController = new ArchiveController(_firestoreDb);
        _patientController = new PatientController(_firestoreDb);
    }


    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        await _integrationTestDataController.ClearFirestoreEmulatorDataAsync();
    }

    [SetUp]
    public async Task SetUpAsync()
    {
        CollectionReference? collectionRef = _firestoreDb.Collection("patients-private");
        QuerySnapshot? snapshot = await collectionRef.GetSnapshotAsync();
        foreach (DocumentSnapshot? doc in snapshot.Documents) await doc.Reference.DeleteAsync();
    }

    private ArchiveController _archiveController;
    private PatientController _patientController;
    private FirestoreDb _firestoreDb;
    private IntegrationTestDataController _integrationTestDataController;

    [Test]
    public async Task TestDeletePatientAnonymizesPatient()
    {
        // Arrange
        PatientPrivate patient = new()
        {
            FName = "John",
            LName = "Doe",
            Phone = "123-456-7890",
            Age = 34,
            Weight = 75.5,
            Height = 180,
            Email = "john.doe@example.com",
            DoctorPhoneNumber = "987-654-3210",
            Condition = "Cerebral Palsy"
        };

        // Add patient to the patients-private collection
        DocumentReference? patientAdded = await _firestoreDb.Collection(PatientController.COLLECTION_NAME)
            .AddAsync(patient);
        string patientId = patientAdded.Id;

        // Delete the patient 
        NoContentResult? response = await _archiveController.DeletePatient(patientId) as NoContentResult;

        // Assert No Content
        Assert.IsNotNull(response);
        Assert.That(response.StatusCode, Is.EqualTo(204), "Expected status code 204 No Content");

        // Check that patient is still in the collection but anonymized
        DocumentSnapshot? snapshot = await _firestoreDb.Collection(PatientController.COLLECTION_NAME)
            .Document(patientId)
            .GetSnapshotAsync();
        Assert.IsTrue(snapshot.Exists, "Patient should still exist in the collection");
        PatientPrivate? patientAfter = snapshot.ConvertTo<PatientPrivate>();

        // integration anonymization
        Assert.That(patientAfter.FName, Does.StartWith("Anon_"), "FName should be an anonymized name");
        Assert.That(patientAfter.FName.Length, Is.EqualTo(10), "FName should be 'Anon_' + 5 chars");
        Assert.IsNull(patientAfter.LName, "LName should be null");
        Assert.IsNull(patientAfter.Phone, "Phone should be null");
        Assert.IsNull(patientAfter.Email, "Email should be null");
        Assert.IsNull(patientAfter.DoctorPhoneNumber, "DoctorPhoneNumber should be null");
        Assert.IsTrue(patientAfter.Deleted, "Deleted flag should be true");

        // Assert non private fields remain unchanged
        Assert.That(patientAfter.Age, Is.EqualTo(34), "Age should remain unchanged");
        Assert.That(patientAfter.Weight, Is.EqualTo(75.5), "Weight should remain unchanged");
        Assert.That(patientAfter.Height, Is.EqualTo(180), "Height should remain unchanged");
        Assert.That(patientAfter.Condition, Is.EqualTo("Cerebral Palsy"), "Condition should remain unchanged");
    }


    [Test]
    public async Task TestGetArchivedPatientListByTherapistIdReturnsAllPatients()
    {
        string therapistId = "test-therapist";
        PatientPrivate patient1 = new()
        {
            FName = "John",
            LName = "Doe",
            Phone = "123-456-7890",
            Age = 34,
            Weight = 75.5,
            Height = 180,
            Email = "john.doe@example.com",
            DoctorPhoneNumber = "987-654-3210",
            Condition = "Cerebral Palsy",
            TherapistID = therapistId,
            ArchivalDate = DateTime.UtcNow.AddDays(-10).ToString("o")
        };
        PatientPrivate patient2 = new()
        {
            FName = "Jane",
            LName = "Doe",
            Phone = "123-456-7890",
            Age = 34,
            Weight = 75.5,
            Height = 180,
            Email = "jane.doe@example.com",
            DoctorPhoneNumber = "987-654-3210",
            Condition = "Cerebral Palsy",
            TherapistID = therapistId,
            ArchivalDate = DateTime.UtcNow.AddDays(-5).ToString("o")
        };
        PatientPrivate patient3 = new()
        {
            FName = "Bob",
            LName = "Doe",
            Phone = "123-456-7890",
            Age = 34,
            Weight = 75.5,
            Height = 180,
            Email = "bob.doe@example.com",
            DoctorPhoneNumber = "987-654-3210",
            Condition = "Cerebral Palsy",
            TherapistID = "different-therapist",
            ArchivalDate = DateTime.UtcNow.AddDays(-1).ToString("o")
        };

        CollectionReference? collectionRef = _firestoreDb.Collection(PatientController.COLLECTION_NAME);
        await collectionRef.AddAsync(patient1);
        await collectionRef.AddAsync(patient2);
        await collectionRef.AddAsync(patient3);

        IActionResult result = await _archiveController.GetArchivedPatientListByTherapistId(therapistId);
        Assert.IsTrue(result is OkObjectResult, "Expected an OkObjectResult");
        OkObjectResult okResult = (OkObjectResult)result;
        List<PatientPrivate>? returnedPatients = (List<PatientPrivate>)okResult.Value;

        Assert.IsNotNull(returnedPatients, "Expected a non-null list of patients");
        Assert.That(returnedPatients.Count, Is.EqualTo(2), "Expected exactly 2 patients");
        Assert.That(returnedPatients.All(p => p.TherapistID == therapistId), "All patients should match therapist ID");
        Assert.That(returnedPatients.Any(p => p.FName == "John" && p.LName == "Doe"), "Expected John Doe");
        Assert.That(returnedPatients.Any(p => p.FName == "Jane" && p.LName == "Doe"), "Expected Jane Doe");
        Assert.That(returnedPatients.All(p => p.FName != "Bob"), "Bob should not be included");
    }

    [Test]
    public async Task TestRestorePatientUpdatesArchivalDateToFuture()
    {
        // Create archived patient
        PatientPrivate patient = new()
        {
            FName = "John",
            LName = "Doe",
            Phone = "123-456-7890",
            Age = 34,
            Weight = 75.5,
            Height = 180,
            Email = "john.doe@example.com",
            DoctorPhoneNumber = "987-654-3210",
            Condition = "Cerebral Palsy",
            ArchivalDate = DateTime.UtcNow.AddDays(-10).ToString("o")
        };

        // Add patient to patients-private
        DocumentReference? patientAdded = await _firestoreDb.Collection("patients-private").AddAsync(patient);
        string patientId = patientAdded.Id;

        // Restore the patient
        OkObjectResult? response = await _archiveController.RestorePatient(patientId) as OkObjectResult;

        // Assert response is successful
        Assert.IsNotNull(response, "Expected an OkObjectResult");
        Assert.That(response.StatusCode, Is.EqualTo(200), "Expected status code 200 OK");

        // Check that patient is still in patients-private with updated ArchivalDate
        DocumentSnapshot? snapshot =
            await _firestoreDb.Collection("patients-private").Document(patientId).GetSnapshotAsync();
        Assert.IsTrue(snapshot.Exists, "Patient should still exist in patients-private");
        PatientPrivate? restoredPatient = snapshot.ConvertTo<PatientPrivate>();

        // Assert patient is not null and has a future ArchivalDate
        Assert.IsNotNull(restoredPatient, "Restored patient should not be null");
        Assert.That(restoredPatient.Id, Is.EqualTo(patientId), "Patient ID should match");
        DateTime restoredArchivalDate = DateTime.Parse(restoredPatient.ArchivalDate);
        Assert.That(restoredArchivalDate, Is.GreaterThan(DateTime.UtcNow), "ArchivalDate should be in the future");
    }

    [Test]
    public async Task TestGetArchivedPatientListByTherapistIdReturnsEmptyListForNoMatches()
    {
        // Get archived patients for a therapist with no matches
        IActionResult? result = await _archiveController.GetArchivedPatientListByTherapistId("test-therapist");

        // Expect an OkObjectResult with an empty list
        Assert.IsTrue(result is OkObjectResult, "Expected an OkObjectResult");
        OkObjectResult? okResult = (OkObjectResult)result;
        List<PatientPrivate>? returnedPatients = (List<PatientPrivate>)okResult.Value;

        // Assert the list is non-null and empty
        Assert.IsNotNull(returnedPatients, "Expected a non-null list of patients");
        Assert.That(returnedPatients.Count, Is.EqualTo(0), "Expected an empty list");
    }

    [Test]
    public async Task TestSessionsAndEvaluationsRemainAfterPatientAnonymization()
    {
        // Add patient as archived
        PatientPrivate patient = new()
        {
            FName = "John",
            LName = "Doe",
            Phone = "123-456-7890",
            Age = 34,
            Weight = 75.5,
            Height = 180,
            Email = "john.doe@example.com",
            DoctorPhoneNumber = "987-654-3210",
            Condition = "Cerebral Palsy",
            ArchivalDate = DateTime.UtcNow.AddDays(-10).ToString("o")
        };
        DocumentReference? patientRef =
            await _firestoreDb.Collection(PatientController.COLLECTION_NAME).AddAsync(patient);
        string patientId = patientRef.Id;

        // Add session
        Session session = new(patientId, "NY", DateTime.UtcNow);
        DocumentReference? sessionRef =
            await _firestoreDb.Collection(SessionController.COLLECTION_NAME).AddAsync(session);
        string sessionId = sessionRef.Id;

        // Add evaluation
        PatientEvaluation evaluation = new()
        {
            SessionID = sessionId,
            EvalType = "pre",
            Lumbar = 1,
            HipFlex = 0,
            HeadAnt = 0,
            HeadLat = 0,
            KneeFlex = 0,
            Pelvic = 0,
            PelvicTilt = 0,
            Thoracic = 0,
            Trunk = 0,
            TrunkInclination = 0,
            ElbowExtension = 0
        };
        await _firestoreDb.Collection("evaluations").AddAsync(evaluation);

        // Delete (anonymize) patient
        IActionResult response = await _archiveController.DeletePatient(patientId);
        Assert.IsTrue(response is NoContentResult, "Expected a NoContentResult");
        Assert.That(((NoContentResult)response).StatusCode, Is.EqualTo(204), "Expected status code 204 No Content");

        // Ensure patient is still in patients-private but anonymized
        DocumentSnapshot? patientSnapshot =
            await _firestoreDb.Collection("patients-private").Document(patientId).GetSnapshotAsync();
        Assert.IsTrue(patientSnapshot.Exists, "Patient should still exist in patients-private");
        PatientPrivate? patientAfter = patientSnapshot.ConvertTo<PatientPrivate>();
        Assert.That(patientAfter.FName, Does.StartWith("Anon_"), "FName should be anonymized");
        Assert.That(patientAfter.FName.Length, Is.EqualTo(10), "FName should be 'Anon_' + 5 chars");
        Assert.IsNull(patientAfter.LName, "LName should be null");
        Assert.IsTrue(patientAfter.Deleted, "Deleted flag should be true");

        // Session remains in db
        QuerySnapshot? sessionsSnapshot = await _firestoreDb.Collection("sessions")
            .WhereEqualTo("PatientID", patientId)
            .GetSnapshotAsync();
        Assert.That(sessionsSnapshot.Count, Is.EqualTo(1), "Expected one session for this patient");
        DocumentSnapshot? sessionDoc = sessionsSnapshot.Documents.Single();
        Session? retrievedSession = sessionDoc.ConvertTo<Session>();
        Assert.That(retrievedSession.PatientID, Is.EqualTo(patientId), "Session PatientID should match");
        Assert.That(retrievedSession.SessionID, Is.EqualTo(sessionId), "Session ID should match");
        Assert.That(retrievedSession.Location, Is.EqualTo("NY"), "Session Location should be NY");

        // Evaluation remains in db
        QuerySnapshot? evalsSnapshot = await _firestoreDb.Collection("evaluations")
            .WhereEqualTo("SessionID", sessionId)
            .GetSnapshotAsync();
        Assert.That(evalsSnapshot.Count, Is.EqualTo(1), "Expected one evaluation for this session");
        DocumentSnapshot? evalDoc = evalsSnapshot.Documents.Single();
        PatientEvaluation? retrievedEval = evalDoc.ConvertTo<PatientEvaluation>();
        Assert.That(retrievedEval.SessionID, Is.EqualTo(sessionId), "Evaluation SessionID should match");
        Assert.That(retrievedEval.EvalType, Is.EqualTo("pre"), "Evaluation EvalType should be 'pre'");
    }

    [Test]
    public async Task TestRestoreDeletedPatientFails()
    {
        PatientPrivate patient = new()
            { FName = "Jane", ArchivalDate = DateTime.UtcNow.AddDays(-10).ToString("o"), Deleted = false };
        DocumentReference? patientRef =
            await _firestoreDb.Collection(PatientController.COLLECTION_NAME).AddAsync(patient);
        await _archiveController.DeletePatient(patientRef.Id); // Sets Deleted = true

        BadRequestObjectResult? response =
            await _archiveController.RestorePatient(patientRef.Id) as BadRequestObjectResult;
        Assert.IsNotNull(response);
        Assert.That(response.StatusCode, Is.EqualTo(400));
        Assert.That((string)response.Value, Is.EqualTo("Cannot restore a permanently deleted patient."));
    }
}