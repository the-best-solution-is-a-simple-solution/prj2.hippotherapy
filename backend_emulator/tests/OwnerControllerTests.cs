using FirebaseAdmin.Auth;
using Google.Api.Gax;
using Google.Cloud.Firestore;
using HippoApi;
using HippoApi.Controllers;
using HippoApi.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace tests;

[TestFixture]
public class OwnerControllerTests : IDisposable
{
    [SetUp]
    public async Task SetupAsync()
    {
        await _integrationTestDataController.ClearFirebaseAuthenticationEmulatorDataAsync();
        await _integrationTestDataController.ClearFirestoreEmulatorDataAsync();

        _mapOwnerIds = await SeedOwnerInfoLoginAndTherapistListPage();
    }

    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        await _integrationTestDataController.ClearFirestoreEmulatorDataAsync();
        await _integrationTestDataController.ClearFirebaseAuthenticationEmulatorDataAsync();
    }

    private readonly OwnerController _ownerController;
    private readonly IntegrationTestDataController _integrationTestDataController;
    private readonly FirestoreDb _firestoreDb;
    private Dictionary<string, string> _mapOwnerIds;

    private readonly TestServer testServer;
    private const string DefaultPassword = "Password1!";
    private static AuthController _authController;

    public OwnerControllerTests()
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

        // instantiate controller and collection
        _integrationTestDataController = new IntegrationTestDataController(_firestoreDb);
        _ownerController = new OwnerController(_firestoreDb);
        _authController = new AuthController(_firestoreDb);
        _integrationTestDataController = new IntegrationTestDataController(_firestoreDb);
    }

    public async Task<Dictionary<string, string>> SeedOwnerInfoLoginAndTherapistListPage()
    {
        // Register Owners
        OwnerRegistrationRequest ownerRegRequest1 = new()
        {
            Email = "owner@test.com",
            FName = "John",
            LName = "DoeOne",
            Password = "Password1!",
            Verified = true
        };

        OwnerRegistrationRequest ownerRegRequest2 = new()
        {
            Email = "owner2@test.com",
            FName = "John",
            LName = "DoeTwo",
            Password = "Password1!",
            Verified = true
        };

        ObjectResult owner1res = await _authController.RegisterOwner(ownerRegRequest1) as ObjectResult;

        ObjectResult owner2res = await _authController.RegisterOwner(ownerRegRequest2) as ObjectResult;

        string owner1Id = owner1res.Value.ToString();
        string owner2Id = owner2res.Value.ToString();

        // Register Therapists
        // TherapistRegistrationRequest owner1TherapistRegRequest1 = new()
        // {
        //     Email = "johnsmith1@test.com",
        //     FName = "John",
        //     LName = "SmithOne",
        //     OwnerId = owner1Id,
        //     Password = "Password1!"
        // };
        //
        // TherapistRegistrationRequest owner1TherapistRegRequest2 = new()
        // {
        //     Email = "johnsmith2@test.com",
        //     FName = "John",
        //     LName = "SmithTwo",
        //     OwnerId = owner1Id,
        //     Password = "Password1!"
        // };

        Therapist therapist1 = new()
        {
            TherapistID = "therapist-one",
            Email = "firsttherapist@test.com",
            FName = "Ron",
            LName = "Johnson"
        };

        Therapist therapist2 = new()
        {
            TherapistID = "therapist-two",
            Email = "secondtherapist@test.com",
            FName = "Dwight",
            LName = "Eisenhower"
        };

        // this one should not show up, it is an out of domain therapist for automated tests only
        Therapist nonDomainTherapist = new()
        {
            TherapistID = "therapist-something-else",
            Email = "outsidetherapist@test.com",
            FName = "John",
            LName = "SmithNonDomain"
        };

        // Therapist therapist1 = new()
        // {
        //     Email = "johnsmith1@test.com",
        //     FName = "John",
        //     LName = "SmithOne",
        // }; 
        //
        // Therapist therapist2 = new()
        // {
        //     Email = "johnsmith2@test.com",
        //     FName = "John",
        //     LName = "SmithTwo",
        // };

        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        UserRecordArgs therapist1Args = new()
        {
            Email = therapist1.Email,
            Password = DefaultPassword,
            Uid = therapist1.TherapistID,
            EmailVerified = true
        };
        UserRecordArgs therapist2Args = new()
        {
            Email = therapist2.Email,
            Password = DefaultPassword,
            Uid = therapist2.TherapistID,
            EmailVerified = true
        };
        await auth.CreateUserAsync(therapist1Args);
        await auth.CreateUserAsync(therapist2Args);
        await IntegrationTestDataController.AddTherapist(therapist1, owner1Id);
        await IntegrationTestDataController.AddTherapist(therapist2, owner1Id);

        // await _authController.RegisterTherapist(owner1TherapistRegRequest1);
        // verify their email
        // string link2 = await _authController.GetVerificationUrl("johnsmith1@test.com");
        // await client.GetAsync(link2);

        // await _authController.RegisterTherapist(owner1TherapistRegRequest2);

        PatientPrivate therapist1Patient1 = new()
        {
            Id = "therapist-1-patient-1",
            FName = "Jane",
            LName = "DoeOne",
            Condition = "ADHD",
            Phone = "123-456-1565",
            Age = 32,
            Weight = 150,
            Height = 220,
            Email = "janedoe1@test.com",
            DoctorPhoneNumber = "555-123-4567",
            GuardianPhoneNumber = "555-555-6666", // Guardian not needed, but still added
            TherapistID = therapist1.TherapistID
        };

        PatientPrivate therapist1Patient2 = new()
        {
            Id = "therapist-1-patient-2",
            FName = "Jane",
            LName = "DoeTwo",
            Condition = "Autism",
            Phone = "123-456-1565",
            Age = 40,
            Weight = 150,
            Height = 200,
            Email = "janedoe2@test.com",
            DoctorPhoneNumber = "555-123-4567",
            GuardianPhoneNumber = "555-555-6666", // Guardian not needed, but still added
            TherapistID = therapist1.TherapistID
        };


        // await IntegrationTestDataController.AddTherapist(therapist1, owner1Id);
        // await IntegrationTestDataController.AddTherapist(therapist2, owner1Id);


        await IntegrationTestDataController.AddTherapist(nonDomainTherapist, owner2Id);

        await IntegrationTestDataController.AddPatient(therapist1Patient1);
        await IntegrationTestDataController.AddPatient(therapist1Patient2);

        return new Dictionary<string, string>
        {
            { "owner1WithUniqueId", owner1Id },
            { "owner2WithUniqueId", owner2Id }
        };
    }

    [Test]
    public async Task TestGetOwnerById()
    {
        string id = _mapOwnerIds["owner2WithUniqueId"];
        IActionResult? actionResult = await _ownerController.GetOwnerById(id);

        Assert.NotNull(actionResult);

        ObjectResult? result = actionResult as ObjectResult;
        Owner? owner = result.Value as Owner;
        Assert.That(result.Value, Is.TypeOf<Owner>());

        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual("owner2@test.com", owner.Email);
    }


    [Test]
    public async Task TestGetTherapistsByOwnerId()
    {
        string id = _mapOwnerIds["owner1WithUniqueId"];
        IActionResult? actionResult = await _ownerController.GetTherapistsByOwnerId(id);
        Assert.NotNull(actionResult);

        ObjectResult? result = actionResult as ObjectResult;

        List<Therapist>? therapists = result.Value as List<Therapist>;

        Assert.AreEqual(200, result.StatusCode);
        Assert.IsTrue(2 <= therapists.Count);

        Assert.IsTrue(therapists.Any(x => x.Email == "firsttherapist@test.com"));
        Assert.IsTrue(therapists.Any(x => x.Email == "secondtherapist@test.com"));
    }


    [Test]
    public async Task TestThatMovingOnePatientSucceeds()
    {
        string id = _mapOwnerIds["owner1WithUniqueId"];
        Console.WriteLine($"ownerId {id}");
        ObjectResult? res = await _ownerController
                .ReassignPatientsToDifferentTherapist(id, "therapist-one"
                    , "therapist-two", new List<string>
                    {
                        "therapist-1-patient-1"
                    })
            as ObjectResult;

        Assert.That(res, Is.Not.Null);
        Assert.That(res.StatusCode, Is.EqualTo(200));

        List<DocumentReference> checkNewTherapistsPatientsDocRefs = await _firestoreDb
            .Collection(PatientController.COLLECTION_NAME)
            .ListDocumentsAsync()
            .ToListAsync();


        Assert.That(checkNewTherapistsPatientsDocRefs, Is.Not.Null);

        List<Patient> checkNewTherapistsPatients = new();

        checkNewTherapistsPatientsDocRefs.ForEach(async x =>
        {
            try
            {
                DocumentSnapshot? y = await x.GetSnapshotAsync();
                checkNewTherapistsPatients.Add(y.ConvertTo<Patient>());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });

        Assert.IsTrue(checkNewTherapistsPatients.All(patient =>
            patient.TherapistID == "therapist-two"));
    }


    [Test]
    public async Task TestThatMovingMultiplePatientSucceeds()
    {
        string id = _mapOwnerIds["owner1WithUniqueId"];
        ObjectResult? res = await _ownerController
                .ReassignPatientsToDifferentTherapist(id, "therapist-one"
                    , "therapist-two", [
                        "therapist-1-patient-1",
                        "therapist-1-patient-2"
                    ])
            as ObjectResult;

        Console.WriteLine(res?.Value);

        Assert.That(res, Is.Not.Null);
        Assert.That(res.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

        List<DocumentReference> therapistsPatientsDocRefs = await _firestoreDb
            .Collection(PatientController.COLLECTION_NAME)
            .ListDocumentsAsync()
            .ToListAsync();

        List<Patient> checkNewTherapistsPatients = new();

        foreach (DocumentReference docRef in therapistsPatientsDocRefs)
            try
            {
                DocumentSnapshot? snapshot = await docRef.GetSnapshotAsync();
                checkNewTherapistsPatients.Add(snapshot.ConvertTo<Patient>());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        // therapistsPatientsDocRefs.ToList().ForEach(async x =>
        // {
        //     try
        //     {
        //         var y = await x.GetSnapshotAsync();
        //         checkNewTherapistsPatients.Add(y.ConvertTo<Patient>());
        //     }
        //     catch (Exception e)
        //     {
        //         Console.WriteLine(e);
        //     }
        // });

        Assert.That(checkNewTherapistsPatients, Is.Not.Null);


        // all reference new therapist
        Assert.IsTrue(checkNewTherapistsPatients.All(patient => patient.TherapistID == "therapist-two"));

        // no references to old therapist
        Assert.IsFalse(checkNewTherapistsPatients.Any(patient => patient.TherapistID == "therapist-one"));
    }


    [Test]
    public async Task TestThatUnauthorizedOwnerMovingToTherapistNotInTheirDomainFails()
    {
        string id = _mapOwnerIds["owner1WithUniqueId"];
        ObjectResult? res = await _ownerController
                .ReassignPatientsToDifferentTherapist(id, "therapist-one"
                    , "therapist-something-else", new List<string>
                    {
                        "therapist-1-patient-1",
                        "therapist-1-patient-2"
                    })
            as ObjectResult;

        Assert.That(res, Is.Not.Null);
        Assert.That(res.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));

        List<DocumentReference> checkTherapistsPatientsDocRefs = await _firestoreDb
            .Collection(PatientController.COLLECTION_NAME)
            .ListDocumentsAsync()
            .ToListAsync();

        Assert.That(checkTherapistsPatientsDocRefs, Is.Not.Null);

        List<Patient> checkTherapistsPatients = new();

        foreach (DocumentReference docRef in checkTherapistsPatientsDocRefs)
            try
            {
                DocumentSnapshot? y = await docRef.GetSnapshotAsync();
                checkTherapistsPatients.Add(y.ConvertTo<Patient>());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        Assert.IsFalse(checkTherapistsPatients.Any(patient =>
            patient.TherapistID == "therapist-something-else"));


        // check old one still has them
        Assert.IsTrue(checkTherapistsPatients.All(patient => patient.TherapistID == "therapist-one"));
    }


    [Test]
    public async Task TestThatUnauthorizedOwnerMovingFromTherapistNotInTheirDomainFails()
    {
        string id = _mapOwnerIds["owner1WithUniqueId"];
        ObjectResult? res = await _ownerController
                .ReassignPatientsToDifferentTherapist(id, "therapist-something-else"
                    , "therapist-one", new List<string>
                    {
                        "therapist-1-patient-1",
                        "therapist-1-patient-2"
                    })
            as ObjectResult;

        Assert.That(res, Is.Not.Null);
        Assert.That(res.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));

        List<DocumentReference> checkTherapistsPatientsDocRefs = await _firestoreDb
            .Collection(PatientController.COLLECTION_NAME)
            .ListDocumentsAsync()
            .ToListAsync();

        List<Patient> checkTherapistsPatients = new();
        foreach (DocumentReference snapshot in checkTherapistsPatientsDocRefs)
            try
            {
                DocumentSnapshot? docRef = await snapshot.GetSnapshotAsync();
                checkTherapistsPatients.Add(docRef.ConvertTo<Patient>());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        // check old therapist has no changes,
        // and that the new therapist does not have any
        bool hasPatientFromTherapistOne = false;
        bool hasPatientSomethingElseTherapist = false;
        foreach (Patient patient in checkTherapistsPatients)
            if (patient.TherapistID == "therapist-one")
                hasPatientFromTherapistOne = true;

        Assert.True(hasPatientFromTherapistOne);
    }


    [Test]
    public async Task TestThatMovingNonExistentPatientFails()
    {
        string id = _mapOwnerIds["owner1WithUniqueId"];
        ObjectResult? res = await _ownerController
                .ReassignPatientsToDifferentTherapist(id, "therapist-one"
                    , "therapist-two", new List<string>
                    {
                        "therapist-1-patient-1",
                        "I do not exist in the system"
                    })
            as ObjectResult;

        Assert.That(res, Is.Not.Null);
        Assert.That(res.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));


        // check that no operations were actually carried out
        List<DocumentReference> checkTherapistsPatientsDocRefs = await _firestoreDb
            .Collection(PatientController.COLLECTION_NAME)
            .ListDocumentsAsync()
            .ToListAsync();

        List<Patient> checkTherapistsPatients = new();

        foreach (DocumentReference snapshot in checkTherapistsPatientsDocRefs)
            try
            {
                DocumentSnapshot? docRef = await snapshot.GetSnapshotAsync();
                checkTherapistsPatients.Add(docRef.ConvertTo<Patient>());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        // checkTherapistsPatientsDocRefs.ForEach(async x =>
        // {
        //     try
        //     {
        //         var docRef = await x.GetSnapshotAsync();
        //         checkTherapistsPatients.Add(docRef.ConvertTo<Patient>());
        //     }
        //     catch (Exception e)
        //     {
        //         Console.WriteLine(e.Message);
        //     }
        // });

        Assert.That(checkTherapistsPatients, Is.Not.Null);

        // check that the first patient is still in the old therapist
        Assert.IsTrue(checkTherapistsPatients.All(patient => patient.TherapistID == "therapist-one"));


        // check that no operations were actually carried out
        // check that it did not go through
        Assert.IsFalse(checkTherapistsPatients.Any(patient => patient.TherapistID == "therapist-two"));
    }

    public void Dispose()
    {
        testServer.Dispose();
    }
}