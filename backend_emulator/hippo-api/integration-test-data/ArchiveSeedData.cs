using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using HippoApi.Controllers;
using HippoApi.Models;

namespace HippoApi.integration_test_data;

public static class ArchiveSeedData
{
    private const string DefaultPassword = "Password1!";
    private static FirestoreDb _firestoreDb;
    private static AuthController _authController;
    private static IntegrationTestDataController _integrationTestDataController;


    public static async Task SeedArchiveData(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
        _authController = new AuthController(_firestoreDb);
        _integrationTestDataController = new IntegrationTestDataController(_firestoreDb);

        using HttpClient client = new();

        // Add owner and therapist
        Owner owner1 = new()
        {
            Email = "archive.owner@test.com",
            OwnerId = "archve-owner-id",
            FName = "John",
            LName = "Owner"
        };

        Therapist therapist1 = new()
        {
            Email = "archive@test.com",
            FName = "Archive",
            LName = "Therapist",
            Country = "United States",
            City = "Austin",
            Street = "123 Therapy Lane",
            PostalCode = "78701",
            Phone = "+1-512-555-0123",
            Profession = "Physical Therapist",
            Major = "Rehabilitation Sciences",
            YearsExperienceInHippotherapy = 5,
            TherapistID = "archve-therapist-id"
        };

        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        UserRecordArgs ownerArgs = new()
        {
            Email = owner1.Email,
            Password = DefaultPassword,
            Uid = owner1.OwnerId,
            EmailVerified = true
        };
        UserRecordArgs therapistArgs = new()
        {
            Email = therapist1.Email,
            Password = DefaultPassword,
            Uid = therapist1.TherapistID,
            EmailVerified = true
        };


        // Check for existing therapist by email in Firebase Auth first
        string therapistId = therapist1.TherapistID;
        UserRecord existingTherapist;
        try
        {
            existingTherapist = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(therapist1.Email);
            therapistId = existingTherapist.Uid;
            Console.WriteLine($"Found existing therapist in Firebase Auth with UID: {therapistId}");
        }
        catch (FirebaseAuthException)
        {
            await auth.CreateUserAsync(ownerArgs);
            await auth.CreateUserAsync(therapistArgs);
            await IntegrationTestDataController.AddOwner(owner1);
            await IntegrationTestDataController.AddTherapist(therapist1, owner1.OwnerId);

            //     // Therapist doesn’t exist in Firebase Auth, proceed to register
            //     TherapistRegistrationRequest archiveTherapistRegRequest = new()
            //     {
            //         Email = "archive@test.com",
            //         FName = "Archive",
            //         LName = "Therapist",
            //         Password = "Password1!",
            //         Country = "United States",
            //         City = "Austin",
            //         Street = "123 Therapy Lane",
            //         PostalCode = "78701",
            //         Phone = "+1-512-555-0123",
            //         Profession = "Physical Therapist",
            //         Major = "Rehabilitation Sciences",
            //         YearsExperienceInHippotherapy = 5
            //     };
            //     IActionResult therapistResult = await _authController.RegisterTherapist(archiveTherapistRegRequest);
            //     if (therapistResult is ObjectResult therapistOkResult)
            //     {
            //         therapistId = therapistOkResult.Value?.ToString();
            //         string link = await _authController.GetVerificationUrl(archiveTherapistRegRequest.Email);
            //         await client.GetAsync(link);
            //         Console.WriteLine($"Registered new therapist with UID: {therapistId}");
            //     }
            //     else
            //     {
            //         throw new Exception($"Unexpected therapist registration result: {therapistResult}");
            //     }
            // }
            //
            // // Add or update therapist in Firestore at top level
            // DocumentReference? therapistRef =
            //     _firestoreDb.Collection(TherapistController.COLLECTION_NAME).Document(therapistId);
            //
            // DocumentSnapshot? therapistSnapshot = await therapistRef.GetSnapshotAsync();
            // Therapist archiveTherapist = new()
            // {
            //     TherapistID = therapistId,
            //     Email = "archive@test.com",
            //     FName = "Archive",
            //     LName = "Therapist",
            //     Country = "Canada",
            //     City = "Saskatoon",
            //     Street = "123 Therapy Lane",
            //     PostalCode = "S0K1W0",
            //     Phone = "+1-512-555-0123",
            //     Profession = "Physical Therapist",
            //     Major = "Rehabilitation Sciences",
            //     YearsExperienceInHippotherapy = 5
            // };
            //
            // if (!therapistSnapshot.Exists)
            // {
            //     await therapistRef.SetAsync(archiveTherapist);
            //     Console.WriteLine($"Added therapist to Firestore with UID: {therapistId}");
            // }
            // else
            // {
            //     // Update existing therapist, merging new data
            //     await therapistRef.SetAsync(archiveTherapist, SetOptions.MergeAll);
            //     Console.WriteLine($"Updated therapist in Firestore with UID: {therapistId}");
        }

        // Seed patients with the therapistId (existing or new)
        PatientPrivate[] patients = new[]
        {
            new PatientPrivate
            {
                Id = "archive-patient-1",
                FName = "Bob",
                LName = "Boberton",
                Condition = "ADHD",
                Phone = "123-456-1565",
                Age = 32,
                Weight = 150,
                Height = 220,
                Email = "bob.boberton@test.com",
                DoctorPhoneNumber = "555-123-4567",
                GuardianPhoneNumber = "555-555-6666",
                TherapistID = therapistId,
                ArchivalDate = DateTime.UtcNow.AddDays(-1).ToString("o"),
                Deleted = false
            },
            new PatientPrivate
            {
                Id = "archive-patient-2",
                FName = "Amy",
                LName = "Adamson",
                Condition = "ADHD",
                Phone = "123-456-1565",
                Age = 32,
                Weight = 150,
                Height = 220,
                Email = "amy.adamson@test.com",
                DoctorPhoneNumber = "555-123-4567",
                GuardianPhoneNumber = "555-555-6666",
                TherapistID = therapistId,
                ArchivalDate = DateTime.UtcNow.AddYears(1).ToString("o"),
                Deleted = false
            },
            new PatientPrivate
            {
                Id = "archive-patient-3",
                FName = "Bill",
                LName = "Billington",
                Condition = "ADHD",
                Phone = "123-456-1565",
                Age = 32,
                Weight = 150,
                Height = 220,
                Email = "bill.billington@test.com",
                DoctorPhoneNumber = "555-123-4567",
                GuardianPhoneNumber = "555-555-6666",
                TherapistID = therapistId,
                ArchivalDate = DateTime.UtcNow.AddDays(-1).ToString("o"),
                Deleted = false
            },
            new PatientPrivate
            {
                Id = "patient-with-sessions-0cc9-4539-9b1e-1db2c1163fe1",
                FName = "John",
                LName = "Smith",
                Condition = "Autism",
                Phone = "555-012-3456",
                Age = 14,
                Weight = 45,
                Height = 145,
                Email = "john.smith@test.com",
                DoctorPhoneNumber = "555-123-4567",
                GuardianPhoneNumber = "555-555-6666",
                TherapistID = therapistId,
                ArchivalDate = DateTime.UtcNow.AddDays(-1).ToString("o"),
                Deleted = false
            },
            new PatientPrivate
            {
                Id = "do-not-use",
                FName = "Anchor",
                LName = "Patient",
                Condition = "Autism",
                Phone = "555-012-3456",
                Age = 14,
                Weight = 45,
                Height = 145,
                Email = "do.not@use.com",
                DoctorPhoneNumber = "555-123-4567",
                GuardianPhoneNumber = "555-555-6666",
                TherapistID = therapistId,
                ArchivalDate = DateTime.UtcNow.AddDays(1).ToString("o"),
                Deleted = false
            }
        };

        foreach (PatientPrivate patient in patients)
        {
            DocumentReference? patientRef = _firestoreDb.Collection("patients-private").Document(patient.Id);
            DocumentSnapshot? patientSnapshot = await patientRef.GetSnapshotAsync();
            if (!patientSnapshot.Exists)
            {
                await IntegrationTestDataController.AddPatient(patient);
                Console.WriteLine($"Added patient: {patient.FName} {patient.LName}");
            }
            else
            {
                await patientRef.SetAsync(patient, SetOptions.Overwrite);
                Console.WriteLine($"Updated patient: {patient.FName} {patient.LName}");
            }
        }

        PatientPrivate johnSmithPatient = patients[3];
        Session sessionA = new()
        {
            SessionID = "test-sessionA_e42a56a1-7abc-4b41-a48c-d2c6fd4bb482",
            PatientID = johnSmithPatient.Id,
            DateTaken = new DateTime(2022, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Location = "NA"
        };

        PatientEvaluation sessionAPreEval = new()
        {
            SessionID = sessionA.SessionID,
            EvaluationID = "test-evalPreA_1f2-ea55-479e-ba37-e5e31d1f6aa3",
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
            EvaluationID = "test-evalPostA_097af-ce43-4844-aa11-3161d934316",
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

        DocumentReference? sessionRef = _firestoreDb.Collection("sessions").Document(sessionA.SessionID);
        DocumentSnapshot? sessionSnapshot = await sessionRef.GetSnapshotAsync();
        if (!sessionSnapshot.Exists)
        {
            await IntegrationTestDataController.AddSession(johnSmithPatient, sessionA);
            Console.WriteLine("Added session for John Smith");
        }
        else
        {
            await sessionRef.SetAsync(sessionA, SetOptions.Overwrite);
            Console.WriteLine("Updated session for John Smith");
        }

        DocumentReference? preEvalRef = _firestoreDb.Collection("evaluations").Document(sessionAPreEval.EvaluationID);
        DocumentSnapshot? preEvalSnapshot = await preEvalRef.GetSnapshotAsync();
        if (!preEvalSnapshot.Exists)
        {
            await IntegrationTestDataController.AddEvaluation(johnSmithPatient, sessionAPreEval, sessionA);
            Console.WriteLine("Added pre-evaluation for John Smith");
        }
        else
        {
            await preEvalRef.SetAsync(sessionAPreEval, SetOptions.Overwrite);
            Console.WriteLine("Updated pre-evaluation for John Smith");
        }

        DocumentReference? postEvalRef = _firestoreDb.Collection("evaluations").Document(sessionAPostEval.EvaluationID);
        DocumentSnapshot? postEvalSnapshot = await postEvalRef.GetSnapshotAsync();
        if (!postEvalSnapshot.Exists)
        {
            await IntegrationTestDataController.AddEvaluation(johnSmithPatient, sessionAPostEval, sessionA);
            Console.WriteLine("Added post-evaluation for John Smith");
        }
        else
        {
            await postEvalRef.SetAsync(sessionAPostEval, SetOptions.Overwrite);
            Console.WriteLine("Updated post-evaluation for John Smith");
        }
    }
}