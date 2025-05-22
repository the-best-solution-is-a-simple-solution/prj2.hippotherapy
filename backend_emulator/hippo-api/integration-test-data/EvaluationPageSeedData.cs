using Google.Cloud.Firestore;
using HippoApi.Controllers;
using HippoApi.Models;
using HippoApi.models.custom_responses;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace HippoApi.integration_test_data;

public class EvaluationPageSeedData
{
    private const string DefaultPassword = "Password1!";
    private const string DefaultTherapistId = "default-id";
    private static FirestoreDb _firestoreDb;
    private static AuthController _authController;
    private static IntegrationTestDataController _integrationTestDataController;
    private static string therapist1Id;

    private static string patientWithSessionsID;
    private static string patientWithSessionsID2;
    private static string patientWithNoSessionsID;

    private static string sessionAWithPrePostEvalsID;
    private static string evalA_PreID;
    private static string evalA_PostID;

    private static string sessionBWithPreEvalID;
    private static string evalB_PreID;

    private static string sessionCWithPostEvalID;
    private static string evalC_PostID;


    private static string sessionDWithNoEvals;

    public static async Task SeedEvaluationPageData(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
        _integrationTestDataController = new IntegrationTestDataController(firestoreDb);
        await SeedOwnersTherapistsPatients(firestoreDb);
        await SeedPatientInfoTestData();
    }

    public static async Task SeedCachedEvaluationData(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
        _authController = new AuthController(_firestoreDb);
        _integrationTestDataController = new IntegrationTestDataController(firestoreDb);
        await Task.Delay(2);
        await SeedCachedData();
    }

    private static async Task SeedCachedData()
    {
        OwnerRegistrationRequest ownerRegRequest1 = new()
        {
            Email = "owner@test.com",
            FName = "John",
            LName = "DoeOne",
            Password = "Password1!",
            Verified = true
        };
        ObjectResult owner1Res = await _authController.RegisterOwner(ownerRegRequest1) as ObjectResult;

        string owner1Id = owner1Res.Value.ToString();

        IActionResult refObj = await _authController.GenerateReferral(new ReferralRequest
            { Email = "johnsmith1@test.com", OwnerId = owner1Res.Value.ToString() });
        List<string>? refInfo = (refObj as OkObjectResult).Value as List<string>;

        // Register Therapists
        TherapistRegistrationRequest owner1TherapistRegRequest1 = new()
        {
            Email = "johnsmith1@test.com",
            FName = "John",
            LName = "SmithOne",
            OwnerId = owner1Id,
            Password = "Password1!",
            Verified = true,
            Referral = refInfo?[1] ?? "true"
        };

        ObjectResult? therapist1Res =
            await _authController.RegisterTherapist(owner1TherapistRegRequest1) as ObjectResult;
        string json = JsonConvert.SerializeObject(therapist1Res.Value);
        RegistrationResponse response = JsonConvert.DeserializeObject<RegistrationResponse>(json);
        therapist1Id = response?.uid ?? "s";

        string localPatientWithSessionsId = Guid.NewGuid().ToString();
        string localSessionDWithNoEvals = Guid.NewGuid().ToString();


        // Add a few patients and sessions with evaluations
        PatientPrivate johnSmithPatientPrivate = new()
        {
            Id = localPatientWithSessionsId,
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
            TherapistID = therapist1Id
        };

        await IntegrationTestDataController.AddPatient(johnSmithPatientPrivate);

        Session sessionD = new()
        {
            SessionID = localSessionDWithNoEvals,
            PatientID = localPatientWithSessionsId,
            DateTaken = new DateTime(2020, 12, 31, 4, 20, 09, DateTimeKind.Utc),
            Location = "PA"
        };

        await IntegrationTestDataController.AddSession(johnSmithPatientPrivate, sessionD);

        // seeding a cached pre-evaluation
        Dictionary<string, dynamic> c = new()
        {
            { PatientEvaluationController.SESSION_ID, localSessionDWithNoEvals },
            { PatientEvaluationController.EVAL_TYPE, "pre" },
            {
                PatientEvaluationController.FORM_DATA, new Dictionary<string, dynamic>
                {
                    { "thoracic", -2 },
                    { "lumbar", 2 }
                }
            }
        };
        await _firestoreDb.Collection(PatientEvaluationController.CACHED_EVAL_COLLECTION_NAME).AddAsync(c);
    }

    public static async Task SeedOwnersTherapistsPatients(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
        _authController = new AuthController(_firestoreDb);
        _integrationTestDataController = new IntegrationTestDataController(_firestoreDb);

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

        IActionResult refObj = await _authController.GenerateReferral(new ReferralRequest
            { Email = "johnsmith1@test.com", OwnerId = owner1res.Value.ToString() });
        List<string>? refInfo = (refObj as OkObjectResult).Value as List<string>;

        // Register Therapists
        TherapistRegistrationRequest owner1TherapistRegRequest1 = new()
        {
            Email = "johnsmith1@test.com",
            FName = "John",
            LName = "SmithOne",
            OwnerId = owner1Id,
            Password = "Password1!",
            Verified = true,
            Referral = refInfo[1]
        };

        refObj = await _authController.GenerateReferral(new ReferralRequest
            { Email = "johnsmith2@test.com", OwnerId = owner1res.Value.ToString() });
        refInfo = (refObj as OkObjectResult).Value as List<string>;

        TherapistRegistrationRequest owner1TherapistRegRequest2 = new()
        {
            Email = "johnsmith2@test.com",
            FName = "John",
            LName = "SmithTwo",
            OwnerId = owner1Id,
            Password = "Password1!",
            Verified = true,
            Referral = refInfo[1]
        };

        ObjectResult? therapist1Res =
            await _authController.RegisterTherapist(owner1TherapistRegRequest1) as ObjectResult;
        string json = JsonConvert.SerializeObject(therapist1Res.Value);
        RegistrationResponse response = JsonConvert.DeserializeObject<RegistrationResponse>(json);
        therapist1Id = response.uid;

        await _authController.RegisterTherapist(owner1TherapistRegRequest2);

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
            TherapistID = therapist1Id
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
            TherapistID = therapist1Id
        };

        Therapist firstTherapist = new()
        {
            TherapistID = "therapist-one",
            Email = "firsttherapist@test.com",
            FName = "Ron",
            LName = "Johnson"
        };

        Therapist secondTherapist = new()
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


        await IntegrationTestDataController.AddTherapist(firstTherapist, owner1Id);
        await IntegrationTestDataController.AddTherapist(secondTherapist, owner1Id);


        await IntegrationTestDataController.AddTherapist(nonDomainTherapist, owner2Id);

        await IntegrationTestDataController.AddPatient(therapist1Patient1);
        await IntegrationTestDataController.AddPatient(therapist1Patient2);
    }

    /// <summary>
    ///     Seed 2 patients one with sessions and one without.
    ///     Patient John has session with pre and post, only pre and only post and none.
    /// </summary>
    public static async Task SeedPatientInfoTestData()
    {
        patientWithSessionsID = "patient-with-sessions-0cc9-4539-9b1e-1db2c1163fe1";
        patientWithSessionsID2 = "patient-with-sesisons-cbdfeb74-6089-4552-9aec-605a2e6879bb";
        patientWithNoSessionsID = "patient-no-sessions-0cc9-4539-9b1e-1db2c1163fe1";

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
            TherapistID = therapist1Id
        };

        PatientPrivate aliceTailorPatientPrivate = new()
        {
            Id = patientWithNoSessionsID,
            FName = "Alice",
            LName = "Tailor",
            Condition = "Celebral Palsy",
            Phone = "555-012-3456",
            Age = 25,
            Weight = 75,
            Height = 180,
            Email = "bibi@rocketmail.net",
            DoctorPhoneNumber = "555-123-4567",
            TherapistID = therapist1Id
        };

        // UUID's generated from https://www.uuidgenerator.net/
        sessionAWithPrePostEvalsID = "test-sessionA_e42a56a1-7abc-4b41-a48c-d2c6fd4bb482";
        evalA_PreID = "test-evalPreA_1f2-ea55-479e-ba37-e5e31d1f6aa3";
        evalA_PostID = "test-evalPostA_097af-ce43-4844-aa11-3161d934316";

        // Test session data
        Session sessionA = new()
        {
            SessionID = sessionAWithPrePostEvalsID,
            PatientID = patientWithSessionsID,
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

        // Session B with only a pre eval
        sessionBWithPreEvalID = "test-sessionB_8f791aee-c24817-a804-a4695b93e279";
        evalB_PreID = "test-evalB_125b4668-6ee3-460-41ca84ea6c5f";

        Session sessionB = new()
        {
            SessionID = sessionBWithPreEvalID,
            PatientID = patientWithSessionsID,
            DateTaken = new DateTime(2022, 01, 10, 0, 0, 0, DateTimeKind.Utc),
            Location = "CA"
        };

        PatientEvaluation sessionBPreEval = new()
        {
            SessionID = sessionB.SessionID,
            EvaluationID = evalB_PreID,
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

        sessionCWithPostEvalID = "test-sessionC-01819e41-4b81-44-4cf2f26edfe2";
        evalC_PostID = "test-evalCPost-c05a4bac-04b6-4661-f972e333c9";

        Session sessionC = new()
        {
            SessionID = sessionCWithPostEvalID,
            PatientID = patientWithSessionsID,
            DateTaken = new DateTime(2022, 02, 02, 0, 0, 0, DateTimeKind.Utc),
            Location = "LA"
        };

        PatientEvaluation sessionCPostEval = new()
        {
            SessionID = sessionC.SessionID,
            EvaluationID = evalC_PostID,
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

        sessionDWithNoEvals = "test-sessionC_964a114b-e810-40-5bb07f46aada";

        Session sessionD = new()
        {
            SessionID = sessionDWithNoEvals,
            PatientID = patientWithSessionsID,
            DateTaken = new DateTime(2020, 12, 31, 4, 20, 09, DateTimeKind.Utc),
            Location = "PA"
        };


        // seeding a cached pre-evaluation
        Dictionary<string, dynamic> c = new()
        {
            { PatientEvaluationController.SESSION_ID, sessionDWithNoEvals },
            { PatientEvaluationController.EVAL_TYPE, "pre" },
            {
                PatientEvaluationController.FORM_DATA, new Dictionary<string, dynamic>
                {
                    { "thoracic", -2 },
                    { "lumbar", 2 }
                }
            }
        };
        await _firestoreDb.Collection(PatientEvaluationController.CACHED_EVAL_COLLECTION_NAME).AddAsync(c);

        // Add Patients to db
        await IntegrationTestDataController.AddPatient(johnSmithPatientPrivate);
        await IntegrationTestDataController.AddPatient(aliceTailorPatientPrivate);

        // Add session to db
        // Create with explicit document ID
        await IntegrationTestDataController.AddSession(johnSmithPatientPrivate, sessionA);
        await IntegrationTestDataController.AddSession(johnSmithPatientPrivate, sessionB);
        await IntegrationTestDataController.AddSession(johnSmithPatientPrivate, sessionC);
        await IntegrationTestDataController.AddSession(johnSmithPatientPrivate, sessionD);

        // Add evaluations to db
        await IntegrationTestDataController.AddEvaluation(johnSmithPatientPrivate, sessionAPreEval, sessionA);
        await IntegrationTestDataController.AddEvaluation(johnSmithPatientPrivate, sessionAPostEval, sessionA);
        await IntegrationTestDataController.AddEvaluation(johnSmithPatientPrivate, sessionBPreEval, sessionB);
        await IntegrationTestDataController.AddEvaluation(johnSmithPatientPrivate, sessionCPostEval, sessionC);
    }
}