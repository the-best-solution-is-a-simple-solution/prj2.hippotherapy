using System.Net;
using System.Text;
using Google.Api.Gax;
using Google.Cloud.Firestore;
using HippoApi;
using HippoApi.Controllers;
using HippoApi.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using tests.Models;

namespace tests;

[TestFixture]
public class PatientControllerTests : IDisposable
{
    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        await _integrationTestData.ClearFirestoreEmulatorDataAsync();
        await SeedData(_firestoreDb);
    }

    [SetUp]
    public async Task SetUpPerTestAsync()
    {
        CollectionReference? collectionRef = _firestoreDb.Collection(PatientController.COLLECTION_NAME);
        QuerySnapshot? snapshot = await collectionRef.GetSnapshotAsync();
        foreach (DocumentSnapshot? doc in snapshot.Documents) await doc.Reference.DeleteAsync();

        // Seed consistent data
        await collectionRef.AddAsync(new PatientPrivate
        {
            TherapistID = therapist1o1.TherapistID,
            FName = "John",
            LName = "Doe",
            Phone = "123-456-7890",
            Age = 34,
            Weight = 75.5,
            Height = 180,
            Email = "john.doe@example.com",
            DoctorPhoneNumber = "987-654-3210",
            Condition = "Cerebral Palsy",
            ArchivalDate = DateTime.UtcNow.AddYears(1).ToString("o"),
            Deleted = false
        });
        await collectionRef.AddAsync(new PatientPrivate
        {
            TherapistID = therapist2o1.TherapistID,
            FName = "Mike",
            LName = "Flutter",
            Phone = "306-974-2038",
            Age = 15,
            Weight = 50.5,
            Height = 140,
            Email = "asdfg@gmail.com",
            DoctorPhoneNumber = "987-654-3210",
            GuardianPhoneNumber = "444-444-4444",
            Condition = "Autism",
            ArchivalDate = DateTime.UtcNow.AddYears(1).ToString("o"),
            Deleted = false
        });
        await collectionRef.AddAsync(new PatientPrivate
        {
            TherapistID = therapist1o2.TherapistID,
            FName = "Ownertwopatient",
            LName = "Flutter",
            Phone = "306-974-2038",
            Age = 15,
            Weight = 50.5,
            Height = 140,
            Email = "owner2patient@gmail.com",
            DoctorPhoneNumber = "987-654-3210",
            GuardianPhoneNumber = "444-444-4444",
            Condition = "Other Condition",
            ArchivalDate = DateTime.UtcNow.AddYears(1).ToString("o"),
            Deleted = false
        });
    }


    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        _client.Dispose();
        await _integrationTestData.ClearFirestoreEmulatorDataAsync();
    }

    private readonly TestServer _testServer;
    private static TestSeedDataHelper _helper;
    private readonly PatientController _patientController;
    private readonly HttpClient _client;
    private readonly FirestoreDb _firestoreDb;
    private readonly IntegrationTestDataController _integrationTestData;

    private Owner owner1;
    private Owner owner2;
    private Therapist therapist1o1;
    private Therapist therapist2o1;
    private Therapist therapist1o2;

    public PatientControllerTests()
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
        _patientController = new PatientController(_firestoreDb);
        _integrationTestData = new IntegrationTestDataController(_firestoreDb);
        _helper = new TestSeedDataHelper(_firestoreDb);
    }

    public void Dispose()
    {
        _testServer.Dispose();
    }

    /// <summary>
    ///     Seed data for owners and therapists
    /// </summary>
    private async Task SeedData(FirestoreDb firestoreDb)
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
    }


    [Test]
    public async Task TestThatPostAddsNewPatient()
    {
        // Create a new valid patient object to post
        PatientPrivate expectedPatient = new()
        {
            TherapistID = "t1-id",
            FName = "Billy",
            LName = "Bob Joe Junior",
            Condition = "Autism",
            Phone = "123-456-7890",
            Age = 25,
            Email = "billy.bob@yahoo.com",
            DoctorPhoneNumber = "987-654-3210",
            Weight = 70,
            Height = 175,
            GuardianPhoneNumber = "123-456-7890"
        };

        StringContent content = new(JsonConvert.SerializeObject(expectedPatient), Encoding.UTF8, "application/json");

        // Send POST request to create the patient
        HttpResponseMessage response =
            await _client.PostAsync($"/patient/submit-patient/{expectedPatient.TherapistID}", content);

        // Check if the response status is OK
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        // Parse the response to get the ID of the created patient
        string responseBody = await response.Content.ReadAsStringAsync();
        CreatePatientResponse createdPatient = JsonConvert.DeserializeObject<CreatePatientResponse>(responseBody);

        Assert.IsNotNull(createdPatient);
        Assert.IsNotNull(createdPatient.PatientId);
        Assert.IsNotEmpty(createdPatient.PatientId);


        // Retrieve the patient from Firestore by the ID to ensure it's stored correctly
        CollectionReference? patientRef = _firestoreDb.Collection("patients-private");
        //PatientPrivate actualPatient = await patientRef.Document(createdPatient.Id).GetSnapshotAsync().ContinueWith(task => task.Result.ConvertTo<PatientPrivate>());
        QuerySnapshot? snapshot = await patientRef.GetSnapshotAsync();
        PatientPrivate? actualPatient = snapshot.Documents
            .First(doc => doc.ConvertTo<PatientPrivate>().Id.Equals(createdPatient.PatientId))
            .ConvertTo<PatientPrivate>();

        // Verify that the patient exists in Firestore and matches the data sent
        Assert.IsTrue(actualPatient != null);
        Assert.AreEqual(expectedPatient.FName, actualPatient.FName);
        Assert.AreEqual(expectedPatient.LName, actualPatient.LName);
        Assert.AreEqual(expectedPatient.Condition, actualPatient.Condition);
        Assert.AreEqual(expectedPatient.Phone, actualPatient.Phone);
        Assert.AreEqual(expectedPatient.Age, actualPatient.Age);
        Assert.AreEqual(expectedPatient.Email, actualPatient.Email);
        Assert.AreEqual(expectedPatient.DoctorPhoneNumber, actualPatient.DoctorPhoneNumber);
        Assert.AreEqual(expectedPatient.Weight, actualPatient.Weight);
        Assert.AreEqual(expectedPatient.Height, actualPatient.Height);
        Assert.AreEqual(expectedPatient.GuardianPhoneNumber, actualPatient.GuardianPhoneNumber);
    }


    [Test]
    public async Task TestThatPostAddsMinorWithPhoneNumber()
    {
        // Create a new valid patient object to post
        PatientPrivate expectedPatient = new()
        {
            TherapistID = "t1-id",
            FName = "Young",
            LName = "Lad",
            Condition = "Kid",
            Phone = "123-456-7890",
            Age = 12,
            Email = "kidrock@yahoo.com",
            DoctorPhoneNumber = "987-654-3210",
            Weight = 22.22,
            Height = 111.11,
            GuardianPhoneNumber = "123-456-7890"
        };

        StringContent content = new(JsonConvert.SerializeObject(expectedPatient), Encoding.UTF8, "application/json");

        // Send POST request to create the patient
        HttpResponseMessage response =
            await _client.PostAsync($"/patient/submit-patient/{expectedPatient.TherapistID}", content);

        // Check if the response status is OK
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        // Parse the response to get the ID of the created patient
        string responseBody = await response.Content.ReadAsStringAsync();
        CreatePatientResponse createdPatient = JsonConvert.DeserializeObject<CreatePatientResponse>(responseBody);

        Assert.IsNotNull(createdPatient);
        Assert.IsNotNull(createdPatient.PatientId);
        Assert.IsNotEmpty(createdPatient.PatientId);


        // Retrieve the patient from Firestore by the ID to ensure it's stored correctly
        CollectionReference? patientRef = _firestoreDb.Collection("patients-private");
        //PatientPrivate actualPatient = await patientRef.Document(createdPatient.Id).GetSnapshotAsync().ContinueWith(task => task.Result.ConvertTo<PatientPrivate>());
        QuerySnapshot? snapshot = await patientRef.GetSnapshotAsync();
        PatientPrivate? actualPatient = snapshot.Documents
            .First(doc => doc.ConvertTo<PatientPrivate>().Id.Equals(createdPatient.PatientId))
            .ConvertTo<PatientPrivate>();

        // Verify that the patient exists in Firestore and matches the data sent
        Assert.IsTrue(actualPatient != null);
        Assert.AreEqual(expectedPatient.FName, actualPatient.FName);
        Assert.AreEqual(expectedPatient.LName, actualPatient.LName);
        Assert.AreEqual(expectedPatient.Condition, actualPatient.Condition);
        Assert.AreEqual(expectedPatient.Phone, actualPatient.Phone);
        Assert.AreEqual(expectedPatient.Age, actualPatient.Age);
        Assert.AreEqual(expectedPatient.Email, actualPatient.Email);
        Assert.AreEqual(expectedPatient.DoctorPhoneNumber, actualPatient.DoctorPhoneNumber);
        Assert.AreEqual(expectedPatient.Weight, actualPatient.Weight);
        Assert.AreEqual(expectedPatient.Height, actualPatient.Height);
        Assert.AreEqual(expectedPatient.GuardianPhoneNumber, actualPatient.GuardianPhoneNumber);
    }


    [Test]
    public async Task TestThatAddingMinorPatientWithoutGuardianNumberFails()
    {
        // Create a new valid patient object to post
        PatientPrivate expectedPatient = new()
        {
            TherapistID = "t1-id",
            FName = "Young",
            LName = "Lad",
            Condition = "Kid",
            Phone = "123-456-7890",
            Age = 12,
            Email = "kidrock@yahoo.com",
            DoctorPhoneNumber = "987-654-3210",
            Weight = 22.22,
            Height = 111.11,
            GuardianPhoneNumber = null
        };

        StringContent content = new(JsonConvert.SerializeObject(expectedPatient), Encoding.UTF8, "application/json");

        // Send POST request to create the patient
        HttpResponseMessage response =
            await _client.PostAsync($"/patient/submit-patient/{expectedPatient.TherapistID}", content);

        // Check that the response status is BadRequest
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [Test]
    public async Task TestThatAddingAdultPatientWithoutGuardianNumberSucceeds()
    {
        // Create a new valid patient object to post
        PatientPrivate expectedPatient = new()
        {
            TherapistID = "t1-id",
            FName = "Old",
            LName = "Man",
            Condition = "Very old",
            Phone = "123-456-7890",
            Age = 99,
            Email = "adultrock@yahoo.com",
            DoctorPhoneNumber = "987-654-3210",
            Weight = 222,
            Height = 199.99,
            GuardianPhoneNumber = null
        };

        StringContent content = new(JsonConvert.SerializeObject(expectedPatient), Encoding.UTF8, "application/json");

        // Send POST request to create the patient
        HttpResponseMessage response =
            await _client.PostAsync($"/patient/submit-patient/{expectedPatient.TherapistID}", content);

        // Check that the response status is BadRequest
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        // Parse the response to get the ID of the created patient
        string responseBody = await response.Content.ReadAsStringAsync();
        CreatePatientResponse createdPatient = JsonConvert.DeserializeObject<CreatePatientResponse>(responseBody);

        Assert.IsNotNull(createdPatient);
        Assert.IsNotNull(createdPatient.PatientId);
        Assert.IsNotEmpty(createdPatient.PatientId);


        // Retrieve the patient from Firestore by the ID to ensure it's stored correctly
        CollectionReference? patientRef = _firestoreDb.Collection("patients-private");
        //PatientPrivate actualPatient = await patientRef.Document(createdPatient.Id).GetSnapshotAsync().ContinueWith(task => task.Result.ConvertTo<PatientPrivate>());
        QuerySnapshot? snapshot = await patientRef.GetSnapshotAsync();
        PatientPrivate? actualPatient = snapshot.Documents
            .First(doc => doc.ConvertTo<PatientPrivate>().Id.Equals(createdPatient.PatientId))
            .ConvertTo<PatientPrivate>();

        // Verify that the patient exists in Firestore and matches the data sent
        Assert.IsTrue(actualPatient != null);
        Assert.AreEqual(expectedPatient.FName, actualPatient.FName);
        Assert.AreEqual(expectedPatient.LName, actualPatient.LName);
        Assert.AreEqual(expectedPatient.Condition, actualPatient.Condition);
        Assert.AreEqual(expectedPatient.Phone, actualPatient.Phone);
        Assert.AreEqual(expectedPatient.Age, actualPatient.Age);
        Assert.AreEqual(expectedPatient.Email, actualPatient.Email);
        Assert.AreEqual(expectedPatient.DoctorPhoneNumber, actualPatient.DoctorPhoneNumber);
        Assert.AreEqual(expectedPatient.Weight, actualPatient.Weight);
        Assert.AreEqual(expectedPatient.Height, actualPatient.Height);
        Assert.AreEqual(expectedPatient.GuardianPhoneNumber, actualPatient.GuardianPhoneNumber);
    }


    [Test]
    public async Task TestGetPatientByIdReturnsPatient()
    {
        // Create a patient with valid properties
        PatientPrivate patient = new()
        {
            FName = "Jane",
            LName = "Doe",
            Condition = "Healthy",
            Phone = "321-654-9870",
            Age = 28,
            Email = "jane.doe@example.com",
            DoctorPhoneNumber = "123-987-6543",
            Weight = 65,
            Height = 160,
            GuardianPhoneNumber = "321-654-9870",
            ArchivalDate = DateTime.UtcNow.AddYears(1).ToString("o"),
            Deleted = false
        };
        CollectionReference? patientRef = _firestoreDb.Collection("patients-private");
        DocumentReference? patientDoc = await patientRef.AddAsync(patient);
        string? patientId = patientDoc.Id;

        // Retrieve the patient by its ID
        OkObjectResult? result = await _patientController.GetPatientById(patientId) as OkObjectResult;

        // Verify that the patient is returned with the correct information
        Assert.IsNotNull(result);
        PatientPrivate? retrievedPatient = result.Value as PatientPrivate;
        Assert.IsNotNull(retrievedPatient);
        Assert.AreEqual("Jane", retrievedPatient.FName);
        Assert.AreEqual("Doe", retrievedPatient.LName);
        Assert.AreEqual("Healthy", retrievedPatient.Condition);
        Assert.AreEqual("321-654-9870", retrievedPatient.Phone);
    }


    [Test]
    public async Task TestGetPatientByValidIDButNoMatchReturnsANotFoundError()
    {
        ObjectResult? result = await _patientController.GetPatientById("This is an invalid ID") as ObjectResult;
        // Verify that I did get a result
        Assert.IsNotNull(result);

        // Verify that it is a 404 error
        Assert.AreEqual(404, result.StatusCode);
    }


    [Test]
    public async Task TestThatMinorTurning18CanRemoveGuardianPhoneNumber()
    {
        PatientPrivate minorPatient = new()
        {
            FName = "John",
            LName = "Doe",
            Condition = "Healthy",
            Phone = "555-555-5555",
            Age = 17,
            Email = "john.doe@example.com",
            DoctorPhoneNumber = "555-555-5555",
            Weight = 75.5,
            Height = 180.4,
            GuardianPhoneNumber = "123-547-3564",
            ArchivalDate = DateTime.UtcNow.AddYears(1).ToString("o"),
            Deleted = false
        };

        CollectionReference? patientRef = _firestoreDb.Collection("patients-private");
        DocumentReference? patientDoc = await patientRef.AddAsync(minorPatient);
        string? patientId = patientDoc.Id;

        PatientPrivate updatedPatient = new()
        {
            Id = patientDoc.Id,
            FName = "John",
            LName = "Doe",
            Condition = "Healthy",
            Phone = "555-555-5555",
            Age = 18,
            Email = "john.doe@example.com",
            DoctorPhoneNumber = "555-555-5555",
            Weight = 75.5,
            Height = 180.4,
            ArchivalDate = DateTime.UtcNow.AddYears(1).ToString("o"), // Keep active
            Deleted = false
        };

        StringContent content = new(JsonConvert.SerializeObject(updatedPatient), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await _client.PutAsync($"/patient/{patientId}", content);

        string responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Status: {response.StatusCode}, Body: {responseBody}"); // Debug
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        DocumentSnapshot? snapshot = await patientRef.Document(patientId).GetSnapshotAsync();
        PatientPrivate? updatedPatientFromDb = snapshot.ConvertTo<PatientPrivate>();
        Assert.AreEqual(18, updatedPatientFromDb.Age);
        Assert.IsNull(updatedPatientFromDb.GuardianPhoneNumber);
    }


    [Test]
    public async Task TestUpdatePatientSuccessfullyUpdates()
    {
        // Create a new valid patient object
        PatientPrivate originalValidPatient = new()
        {
            FName = "John",
            LName = "Doe",
            Condition = "Healthy",
            Phone = "555-555-5555",
            Age = 30,
            Email = "john.doe@example.com",
            DoctorPhoneNumber = "555-555-5555",
            Weight = 75.5,
            Height = 180.4,
            ArchivalDate = DateTime.UtcNow.AddYears(1).ToString("o"),
            Deleted = false
        };

        CollectionReference? patientRef = _firestoreDb.Collection("patients-private");
        DocumentReference? patientDoc = await patientRef.AddAsync(originalValidPatient);
        string? patientId = patientDoc.Id;

        // Prepare an update
        PatientPrivate updatedPatient = new()
        {
            FName = "John",
            LName = "Doe",
            Condition = "Updated Condition",
            Phone = "555-555-5555",
            Age = 31, // Changed age
            Email = "john.doe@example.com",
            DoctorPhoneNumber = "555-555-5555",
            Weight = 68.99, // Changed weight
            Height = 222.22, // also got taller
            ArchivalDate = DateTime.UtcNow.AddYears(1).ToString("o"),
            Deleted = false
        };

        StringContent content = new(JsonConvert.SerializeObject(updatedPatient), Encoding.UTF8, "application/json");

        // PUT request to update the patient
        HttpResponseMessage response = await _client.PutAsync($"/patient/{patientId}", content);

        // Check that the response status is OK
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        // Retrieve the updated patient from Firestore
        DocumentSnapshot? snapshot = await patientRef.Document(patientId).GetSnapshotAsync();
        PatientPrivate? updatedPatientFromDb = snapshot.ConvertTo<PatientPrivate>();

        // Verify that the patient is updated in Firestore
        Assert.AreEqual("Updated Condition", updatedPatientFromDb.Condition);
        Assert.AreEqual(31, updatedPatientFromDb.Age);
        Assert.AreEqual(68.99, updatedPatientFromDb.Weight);
        Assert.AreEqual(222.22, updatedPatientFromDb.Height);
    }


    [Test]
    public async Task TestUpdatePatientWithNoChangesReturnsNoUpdate()
    {
        // Create a new patient
        PatientPrivate patient = new()
        {
            FName = "Jane",
            LName = "Doe",
            Condition = "Healthy",
            Phone = "555-555-5555",
            Age = 28,
            Email = "jane.doe@example.com",
            DoctorPhoneNumber = "555-555-5555",
            Weight = 65,
            Height = 160,
            GuardianPhoneNumber = "555-555-5555",
            ArchivalDate = DateTime.UtcNow.AddYears(1).ToString("o"),
            Deleted = false
        };

        CollectionReference? patientRef = _firestoreDb.Collection("patients-private");
        DocumentReference? patientDoc = await patientRef.AddAsync(patient);
        string? patientId = patientDoc.Id;

        // Send a PUT request with the same data
        StringContent content = new(JsonConvert.SerializeObject(patient), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await _client.PutAsync($"/patient/{patientId}", content);

        // Assert that the response returns "No changes detected"
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        string responseBody = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(responseBody.Contains($"No changes detected for {patient.FName} {patient.LName}"));
    }


    [Test]
    public async Task TestUpdatePatientWithNonExistentIdReturnsNotFound()
    {
        // Use a non-existent patient ID
        const string nonExistentId = "Non-sense ID";

        PatientPrivate patient = new()
        {
            FName = "Non",
            LName = "Existent",
            Condition = "Unknown",
            Phone = "000-000-0000",
            Age = 100,
            Email = "non.existent@example.com",
            DoctorPhoneNumber = "000-000-0000",
            Weight = 100,
            Height = 180,
            GuardianPhoneNumber = "000-000-0000",
            ArchivalDate = DateTime.UtcNow.AddYears(1).ToString("o"),
            Deleted = false
        };

        StringContent content = new(JsonConvert.SerializeObject(patient), Encoding.UTF8, "application/json");

        // Send PUT request to update the non-existent patient
        HttpResponseMessage response =
            await _client.PutAsync($"/patient/{nonExistentId}", content);

        // Verify that the response is a NotFound
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        string responseBody = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(responseBody.Contains($"Patient with {nonExistentId} not found"));
    }


    [Test]
    public async Task TestUpdatePatientWithInvalidDataReturnsBadRequest()
    {
        // Create an invalid patient with missing required fields
        PatientPrivate patient = new()
        {
            FName = "Invalid",
            LName = "", // Missing last name
            Condition = "Invalid data",
            Phone = "555-555-5555",
            Age = 30,
            Email = "invalid.email@example.com",
            DoctorPhoneNumber = "555-555-5555",
            Weight = 75,
            Height = 180,
            ArchivalDate = DateTime.UtcNow.AddYears(1).ToString("o"),
            Deleted = false
        };

        StringContent content = new(JsonConvert.SerializeObject(patient), Encoding.UTF8, "application/json");

        // Send PUT request with invalid data
        HttpResponseMessage response = await _client.PutAsync("/patient/123", content);

        // Check that a BadRequest response is returned due to invalid data
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [Test]
    public async Task TestUpdatePatientWithNullFieldDeletesData()
    {
        // Create a patient
        PatientPrivate patient = new()
        {
            FName = "John",
            LName = "Doe",
            Condition = "Healthy",
            Phone = "555-555-5555",
            Age = 30,
            Email = "john.doe@example.com",
            DoctorPhoneNumber = "555-555-5555",
            Weight = 75,
            Height = 180,
            GuardianPhoneNumber = "555-555-5555",
            ArchivalDate = DateTime.UtcNow.AddYears(1).ToString("o"),
            Deleted = false
        };

        CollectionReference? patientRef = _firestoreDb.Collection("patients-private");
        DocumentReference? patientDoc = await patientRef.AddAsync(patient);
        string? patientId = patientDoc.Id;

        // Prepare an update with a null field (to delete the field)
        PatientPrivate updatedPatient = new()
        {
            FName = "John",
            LName = "Doe",
            Condition = "Healthy",
            Phone = "555-555-5555",
            Age = 30,
            Email = "john.doe@example.com",
            DoctorPhoneNumber = "555-555-5555",
            Weight = 75,
            Height = 180,
            GuardianPhoneNumber = null
        };

        StringContent content = new(JsonConvert.SerializeObject(updatedPatient), Encoding.UTF8, "application/json");

        //Send PUT request to update the patient
        HttpResponseMessage response = await _client.PutAsync($"/patient/{patientId}", content);

        // Check that the response status is OK
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        // Retrieve the updated patient and verify GuardianPhoneNumber is deleted
        DocumentSnapshot? snapshot = await patientRef.Document(patientId).GetSnapshotAsync();
        PatientPrivate? updatedPatientFromDb = snapshot.ConvertTo<PatientPrivate>();

        Assert.IsNull(updatedPatientFromDb.GuardianPhoneNumber);
    }


    [Test]
    public async Task TestArchivePatientSetsArchivalDate()
    {
        PatientPrivate patient = new()
        {
            FName = "John",
            LName = "Doe",
            Condition = "Healthy",
            Phone = "555-1234",
            Age = 30,
            Email = "john.doe@example.com",
            DoctorPhoneNumber = "555-9876",
            Weight = 75,
            Height = 180,
            GuardianPhoneNumber = "555-4321",
            ArchivalDate = DateTime.UtcNow.AddYears(1).ToString("o"),
            Deleted = false
        };
        CollectionReference? patientRef = _firestoreDb.Collection("patients-private");
        DocumentReference? patientDoc = await patientRef.AddAsync(patient);
        string? patientId = patientDoc.Id;

        HttpResponseMessage response = await _client.DeleteAsync($"/patient/{patientId}");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        string responseBody = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(responseBody.Contains("Patient archived successfully"));

        DocumentSnapshot? archivedPatient = await patientRef.Document(patientId).GetSnapshotAsync();
        Assert.IsTrue(archivedPatient.Exists);
        PatientPrivate? patientData = archivedPatient.ConvertTo<PatientPrivate>();
        Assert.That(DateTime.Parse(patientData.ArchivalDate), Is.LessThanOrEqualTo(DateTime.UtcNow));
    }


    [Test]
    public async Task TestDeleteNonExistingPatientReturns404()
    {
        string? nonExistingPatientId = "non-existing-id";

        // Send DELETE request for a patient that doesn't exist
        HttpResponseMessage? response =
            await _client.DeleteAsync($"/patient/{nonExistingPatientId}");

        // Assert that the response is BadRequest (status code 400)
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        // Check the response message
        string? responseBody = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(responseBody.Contains("Patient was not found."));
    }
}