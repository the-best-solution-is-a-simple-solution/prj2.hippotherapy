using FirebaseAdmin.Auth;
using HippoApi.Controllers;
using HippoApi.Models;

namespace HippoApi.integration_test_data;

/// <summary>
///     Seed data for integration tests for the info tab in patient info page
/// </summary>
public class PatientInfoSeedData
{
    private const string DefaultPassword = "Password1!";
    private const string DefaultTherapistId = "default";
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

    /// <summary>
    ///     Seed 2 patients one with sessions and one without.
    ///     Patient John has session with pre and post, only pre and only post and none.
    /// </summary>
    public static async Task SeedPatientInfoSessionsTabTestData()
    {
        // Add owner and therapist
        Owner owner1 = new()
        {
            Email = "info-owner@test.com",
            OwnerId = "info-owner1-id",
            FName = "John",
            LName = "Owner"
        };

        Therapist therapist1 = new()
        {
            Email = "info-therapist1@test.com",
            TherapistID = DefaultTherapistId,
            FName = "John",
            LName = "Therapist"
        };

        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        UserRecordArgs ownerArgs = new()
        {
            Email = "info-owner@test.com",
            Password = DefaultPassword,
            Uid = owner1.OwnerId,
            EmailVerified = true
        };
        UserRecordArgs therapistArgs = new()
        {
            Email = "info-therapist1@test.com",
            Password = DefaultPassword,
            Uid = therapist1.TherapistID,
            EmailVerified = true
        };
        await auth.CreateUserAsync(ownerArgs);
        await auth.CreateUserAsync(therapistArgs);
        await IntegrationTestDataController.AddOwner(owner1);
        await IntegrationTestDataController.AddTherapist(therapist1, owner1.OwnerId);


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
            TherapistID = therapist1.TherapistID
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
            TherapistID = therapist1.TherapistID
        };

        // UUID's generated from https://www.uuidgenerator.net/
        sessionAWithPrePostEvalsID = "test-sessionA_e42a56a1-7abc-4b41-a48c-d2c6fd4bb482";
        evalA_PreID = "test-evalPreA_1f2-ea55-479e-ba37-e5e31d1f6aa3";
        evalA_PostID = "test-evalPostA_097af-ce43-4844-aa11-3161d934316";

        // integration session data
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
            DateTaken = new DateTime(2020, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Location = "PA"
        };


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

    public static async Task SeedPatientExportInfo()
    {
        // Add owner and therapist
        Owner owner1 = new()
        {
            Email = "owner1@test.com",
            OwnerId = "owner1-id",
            FName = "John",
            LName = "Owner"
        };

        Owner owner2 = new()
        {
            Email = "owner2@test.com",
            OwnerId = "owner2-id",
            FName = "John",
            LName = "OwnerTwo"
        };

        Owner owner3 = new()
        {
            Email = "owner3@test.com",
            OwnerId = "owner3-id",
            FName = "John",
            LName = "ownernodata"
        };

        Therapist therapist1owner1 = new()
        {
            Email = "therapist1o1@test.com",
            TherapistID = "therapist1o1-id",
            FName = "John",
            LName = "Therapist"
        };
        Therapist therapist2owner1 = new()
        {
            Email = "therapist2o1@test.com",
            TherapistID = "therapist2o1-id",
            FName = "John",
            LName = "Therapisttwo"
        };
        Therapist therapist1owner2 = new()
        {
            Email = "therapist1o2@test.com",
            TherapistID = "therapist1o2-id",
            FName = "John",
            LName = "ownertwo"
        };

        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        UserRecordArgs owner1Args = new()
        {
            Email = owner1.Email,
            Password = DefaultPassword,
            Uid = owner1.OwnerId,
            EmailVerified = true
        };
        UserRecordArgs owner2Args = new()
        {
            Email = owner2.Email,
            Password = DefaultPassword,
            Uid = owner2.OwnerId,
            EmailVerified = true
        };

        UserRecordArgs owner3Args = new()
        {
            Email = owner3.Email,
            Password = DefaultPassword,
            Uid = owner3.OwnerId,
            EmailVerified = true
        };

        UserRecordArgs therapist1Args = new()
        {
            Email = therapist1owner1.Email,
            Password = DefaultPassword,
            Uid = therapist1owner1.TherapistID,
            EmailVerified = true
        };
        UserRecordArgs therapist2Args = new()
        {
            Email = therapist2owner1.Email,
            Password = DefaultPassword,
            Uid = therapist2owner1.TherapistID,
            EmailVerified = true
        };
        UserRecordArgs therapist3Args = new()
        {
            Email = therapist1owner2.Email,
            Password = DefaultPassword,
            Uid = therapist1owner2.TherapistID,
            EmailVerified = true
        };

        await auth.CreateUserAsync(owner1Args);
        await auth.CreateUserAsync(owner2Args);
        await auth.CreateUserAsync(owner3Args);

        await auth.CreateUserAsync(therapist1Args);
        await auth.CreateUserAsync(therapist2Args);
        await auth.CreateUserAsync(therapist3Args);

        await IntegrationTestDataController.AddOwner(owner1);
        await IntegrationTestDataController.AddOwner(owner2);
        await IntegrationTestDataController.AddOwner(owner3);
        await IntegrationTestDataController.AddTherapist(therapist1owner1, owner1.OwnerId);
        await IntegrationTestDataController.AddTherapist(therapist2owner1, owner1.OwnerId);
        await IntegrationTestDataController.AddTherapist(therapist1owner2, owner2.OwnerId);

        patientWithSessionsID = "patient-with-sessions-0cc9-4539-9b1e-1db2c1163fe1";
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
            TherapistID = therapist1owner1.TherapistID
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
            GuardianPhoneNumber = "555-555-6666", // Guardian needed
            TherapistID = therapist2owner1.TherapistID
        };

        PatientPrivate otherOwnerPatientPrivate = new()
        {
            Id = "other-owner-patient-id",
            FName = "ownertwo",
            LName = "Ownertwolast",
            Condition = "Stroke",
            Phone = "555-012-3456",
            Age = 25,
            Weight = 75,
            Height = 180,
            Email = "fasdf@rocketmail.net",
            DoctorPhoneNumber = "555-123-4567",
            GuardianPhoneNumber = "555-555-6666", // Guardian needed
            TherapistID = therapist1owner2.TherapistID
        };

        // UUID's generated from https://www.uuidgenerator.net/
        sessionAWithPrePostEvalsID = "test-sessionA_e42a56a1-7abc-4b41-a48c-d2c6fd4bb482";
        evalA_PreID = "test-evalPreA_1f2-ea55-479e-ba37-e5e31d1f6aa3";
        evalA_PostID = "test-evalPostA_097af-ce43-4844-aa11-3161d934316";

        // integration session data
        Session sessionA = new()
        {
            SessionID = sessionAWithPrePostEvalsID,
            PatientID = patientWithSessionsID,
            DateTaken = DateTime.Parse("11-20-2021"),
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
        evalB_PreID = "test-evalB_125b4668-6ee3-460-41ca84ea6c5f";

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

        sessionCWithPostEvalID = "test-sessionC-01819e41-4b81-44-4cf2f26edfe2";
        evalC_PostID = "test-evalCPost-c05a4bac-04b6-4661-f972e333c9";

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

        Session sessionAOtherOwner = new()
        {
            SessionID = "test-sessionA-owner2",
            PatientID = otherOwnerPatientPrivate.Id,
            DateTaken = DateTime.Parse("11-20-2021"),
            Location = "MA"
        };

        PatientEvaluation sessionAOtherOwnerPreEval = new()
        {
            SessionID = sessionAOtherOwner.SessionID,
            EvaluationID = "evaluation-other-owner",
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

        // Add Patients to db
        await IntegrationTestDataController.AddPatient(johnSmithPatientPrivate);
        await IntegrationTestDataController.AddPatient(aliceTailorPatientPrivate);
        await IntegrationTestDataController.AddPatient(otherOwnerPatientPrivate);

        // Add session to db
        // Create with explicit document ID
        await IntegrationTestDataController.AddSession(johnSmithPatientPrivate, sessionA);
        await IntegrationTestDataController.AddSession(johnSmithPatientPrivate, sessionB);
        await IntegrationTestDataController.AddSession(aliceTailorPatientPrivate, sessionC);
        await IntegrationTestDataController.AddSession(otherOwnerPatientPrivate, sessionAOtherOwner);

        await IntegrationTestDataController.AddEvaluation(johnSmithPatientPrivate, sessionAPreEval, sessionA);
        await IntegrationTestDataController.AddEvaluation(johnSmithPatientPrivate, sessionAPostEval, sessionA);
        await IntegrationTestDataController.AddEvaluation(aliceTailorPatientPrivate, sessionCPreEval, sessionC);
        await IntegrationTestDataController.AddEvaluation(aliceTailorPatientPrivate, sessionCPostEval, sessionC);
        await IntegrationTestDataController.AddEvaluation(otherOwnerPatientPrivate, sessionAOtherOwnerPreEval,
            sessionAOtherOwner);
    }


    /// <summary>
    ///     Seed 2 patients
    /// </summary>
    public static async Task SeedPatientListTestData()
    {
        // Add owner and therapist
        Owner owner1 = new()
        {
            Email = "o1@test.com",
            OwnerId = "list-owner1-id",
            FName = "John",
            LName = "Owner"
        };

        Therapist therapist1 = new()
        {
            Email = "t1@test.com",
            TherapistID = "list-therapist1-id",
            FName = "John",
            LName = "Therapist"
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
        await auth.CreateUserAsync(ownerArgs);
        await auth.CreateUserAsync(therapistArgs);
        await IntegrationTestDataController.AddOwner(owner1);
        await IntegrationTestDataController.AddTherapist(therapist1, owner1.OwnerId);

        PatientPrivate patient1 = new()
        {
            Id = "patient-zeb-id-adfwada",
            FName = "Zebideer",
            LName = "Russ",
            Condition = "ADHD",
            Phone = "123-456-1565",
            Age = 32,
            Weight = 150,
            Height = 220,
            Email = "szabo5144@saskpolytech.ca",
            DoctorPhoneNumber = "555-123-4567",
            GuardianPhoneNumber = "555-555-6666", // Guardian not needed, but still added
            TherapistID = therapist1.TherapistID
        };

        PatientPrivate patient2 = new()
        {
            Id = "patient-adam-jest-id-fdhgfdhf",
            FName = "Aabha",
            LName = "Singh",
            Condition = "Paralysis",
            Phone = "456-945-1565",
            Age = 59,
            Weight = 60,
            Height = 120,
            Email = "szabo5144@saskpolytech.ca",
            DoctorPhoneNumber = "555-123-4567",
            TherapistID = therapist1.TherapistID
        };

        await IntegrationTestDataController.AddPatient(patient1);
        await IntegrationTestDataController.AddPatient(patient2);
    }

    /// <summary>
    ///     Seed 3 Patients and their evaluations<br />
    ///     John NoEvals: Has no Patient Evaluations Linked to him<br />
    ///     John OneEval: Has One Patient Evaluation Linked to him<br />
    ///     John TwoEvals: Has Two Patient Evaluations Linked to him
    /// </summary>
    public static async Task SeedPatientsForEvaluationGraph()
    {
        // Add owner and therapist
        Owner owner1 = new()
        {
            Email = "info-owner@test.com",
            OwnerId = "info-owner1-id",
            FName = "John",
            LName = "Owner"
        };

        Therapist therapist1 = new()
        {
            Email = "info-therapist1@test.com", // IMPORTANT to set this the same as in patient info seed data therapist
            TherapistID = "graph-therapist1-id",
            FName = "John",
            LName = "Therapist"
        };

        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        UserRecordArgs ownerArgs = new()
        {
            Email = "info-owner@test.com",
            Password = DefaultPassword,
            Uid = owner1.OwnerId,
            EmailVerified = true
        };
        UserRecordArgs therapistArgs = new()
        {
            Email = "info-therapist1@test.com",
            Password = DefaultPassword,
            Uid = therapist1.TherapistID,
            EmailVerified = true
        };
        await auth.CreateUserAsync(ownerArgs);
        await auth.CreateUserAsync(therapistArgs);
        await IntegrationTestDataController.AddOwner(owner1);
        await IntegrationTestDataController.AddTherapist(therapist1, owner1.OwnerId);

        PatientPrivate johnNoEvals = new()
        {
            Id = "patient-john-no-evals-fdsfadfda",
            FName = "John",
            LName = "NoEvals",
            Condition = "Paralysis",
            Phone = "123-456-1565",
            Age = 12,
            Weight = 40,
            Height = 70,
            Email = "john.noEvals@saskpolytech.ca",
            DoctorPhoneNumber = "555-123-4567",
            TherapistID = therapist1.TherapistID
        };

        PatientPrivate johnOneEval = new()
        {
            Id = "patient-john-one-eval-fdsfadfda",
            FName = "John",
            LName = "OneEval",
            Condition = "Paralysis",
            Phone = "123-456-1565",
            Age = 12,
            Weight = 40,
            Height = 70,
            Email = "john.noEvals@saskpolytech.ca",
            DoctorPhoneNumber = "555-123-4567",
            TherapistID = therapist1.TherapistID
        };

        PatientPrivate johnTwoEvals = new()
        {
            Id = "patient-john-two-evals-fdsfadfda",
            FName = "John",
            LName = "TwoEvals",
            Condition = "Paralysis",
            Phone = "123-456-1565",
            Age = 12,
            Weight = 40,
            Height = 70,
            Email = "john.noEvals@saskpolytech.ca",
            DoctorPhoneNumber = "555-123-4567",
            TherapistID = therapist1.TherapistID
        };

        await IntegrationTestDataController.AddPatient(johnNoEvals);
        await IntegrationTestDataController.AddPatient(johnOneEval);
        await IntegrationTestDataController.AddPatient(johnTwoEvals);

        /*
         * Evaluations
         */

        PatientEvaluation johnOneEvalA = new()
        {
            EvaluationID = "john-one-eval-a-fdsfadfda",
            SessionID = "john-one-eval-a-fdsfadfda",
            ElbowExtension = 0,
            HeadLat = 1,
            HeadAnt = 1,
            Lumbar = 2,
            HipFlex = -2,
            Pelvic = -1,
            KneeFlex = 0,
            PelvicTilt = 0,
            Thoracic = 0,
            Trunk = 2,
            TrunkInclination = -2,
            EvalType = "post"
        };

        PatientEvaluation johnTwoEvalsA = new()
        {
            EvaluationID = "john-two-evals-a-fdsfadfda",
            SessionID = "john-two-evals-a-fdsfadfda",
            ElbowExtension = 0,
            HeadLat = 1,
            HeadAnt = 1,
            Lumbar = 2,
            HipFlex = -2,
            Pelvic = -1,
            KneeFlex = 0,
            PelvicTilt = 0,
            Thoracic = 0,
            Trunk = 2,
            TrunkInclination = -2,
            EvalType = "post"
        };

        PatientEvaluation johnTwoEvalsB = new()
        {
            EvaluationID = "john-two-evals-b-fdsfadfda",
            SessionID = "john-two-evals-b-fdsfadfda",
            ElbowExtension = 0,
            HeadLat = 1,
            HeadAnt = 1,
            Lumbar = 2,
            HipFlex = -2,
            Pelvic = -1,
            KneeFlex = 0,
            PelvicTilt = 0,
            Thoracic = 0,
            Trunk = 2,
            TrunkInclination = -2,
            EvalType = "post"
        };

        Session sessionOne = new()
        {
            Location = "CA",
            PatientID = johnOneEval.Id,
            DateTaken = DateTime.Parse("11-20-2023")
        };

        Session sessionTwo = new()
        {
            Location = "CA",
            PatientID = johnOneEval.Id,
            DateTaken = DateTime.Parse("11-22-2023")
        };


        // Don't need one for johnNoEvals
        await IntegrationTestDataController.AddEvaluation(johnOneEval, johnOneEvalA, sessionOne);
        await IntegrationTestDataController.AddEvaluation(johnTwoEvals, johnTwoEvalsA, sessionTwo);
        await IntegrationTestDataController.AddEvaluation(johnTwoEvals, johnTwoEvalsB, sessionTwo);
    }

    /// <summary>
    ///     Seeds 3 patients all with firstname 'Albert' with data to showcase the
    ///     Patient Info Page - Graph Tab
    /// </summary>
    public static async Task SeedPatientInfoPageGraphTabTestData()
    {
        // Add owner and therapist
        Owner owner1 = new()
        {
            Email = "owner@test.com",
            OwnerId = "info-owner1-id",
            FName = "John",
            LName = "Owner"
        };

        Therapist therapist1 = new()
        {
            Email = "info-therapist1@test.com",
            TherapistID = "info-therapist1-id",
            FName = "John",
            LName = "Therapist"
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
        await auth.CreateUserAsync(ownerArgs);
        await auth.CreateUserAsync(therapistArgs);
        await IntegrationTestDataController.AddOwner(owner1);
        await IntegrationTestDataController.AddTherapist(therapist1, owner1.OwnerId);


        string albertNoevals = "albert-noevals-893c8ec8-a66d-41bf-9672-bef97a34f06e";

        // Add a few patients and sessions with evaluations
        PatientPrivate albertNoevalsPatientPrivate = new()
        {
            Id = albertNoevals,
            FName = "Albert",
            LName = "Noevals",
            Condition = "Autism",
            Phone = "555-012-3456",
            Age = 14,
            Weight = 45,
            Height = 145,
            Email = "asafsfas@gmail.com",
            DoctorPhoneNumber = "555-123-4567",
            GuardianPhoneNumber = "555-555-6666",
            TherapistID = therapist1.TherapistID
        };

        // Add Albert Noevals data (nothing!)
        await IntegrationTestDataController.AddPatient(albertNoevalsPatientPrivate);

        // UUID's generated from https://www.uuidgenerator.net/
        string albertOneeval = "albert-oneeval-d56722ab-5f60-4417-8fb8-c55a11d7af06";
        string albertOneevalSessionAWithPreEvalUUID = "albert-oneeval-test-sessionA_e42a56a1";
        string albertOneevalEvalA_PreID = "albert-oneeval-test-evalPreA_1f2-ea55asdf";


        PatientPrivate albertOneevalPatientPrivate = new()
        {
            Id = albertOneeval,
            FName = "Albert",
            LName = "Oneeval",
            Condition = "Autism",
            Phone = "555-012-3456",
            Age = 14,
            Weight = 45,
            Height = 145,
            Email = "asafsfas@gmail.com",
            DoctorPhoneNumber = "555-123-4567",
            GuardianPhoneNumber = "555-555-6666",
            TherapistID = therapist1.TherapistID
        };

        // integration session data
        Session albertOneevalSessionA = new()
        {
            SessionID = albertOneevalSessionAWithPreEvalUUID,
            PatientID = albertOneeval,
            DateTaken = new DateTime(2022, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Location = "NA"
        };

        PatientEvaluation albertOneevalSessionAPreEval = new()
        {
            SessionID = albertOneevalSessionA.SessionID,
            EvaluationID = albertOneevalEvalA_PreID,
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


        // Add albert Oneeval's data
        await IntegrationTestDataController.AddPatient(albertOneevalPatientPrivate);
        await IntegrationTestDataController.AddSession(albertOneevalPatientPrivate, albertOneevalSessionA);
        await IntegrationTestDataController.AddEvaluation(albertOneevalPatientPrivate, albertOneevalSessionAPreEval,
            albertOneevalSessionA);

        // Add albert Sixsessions's data (six sessions, 11 evaluations
        string albertSixsessions = "albert-sixsessions-3ae9a4da-52e8-4563-9227-a3a24aa99f8a";

        PatientPrivate albertSixsessionsPatientPrivate = new()
        {
            Id = albertSixsessions,
            FName = "Albert",
            LName = "Sixsessions",
            Condition = "Autism",
            Phone = "555-012-3456",
            Age = 14,
            Weight = 45,
            Height = 145,
            Email = "asafsfas@gmail.com",
            DoctorPhoneNumber = "555-123-4567",
            GuardianPhoneNumber = "555-555-6666",
            TherapistID = therapist1.TherapistID
        };

        // integration session1 data
        // UUID's generated from https://www.uuidgenerator.net/
        string albertSixsessionsSession1UUID = "albert-sixsessions-test-s1_e42a56a1awf";
        string albertSixsessionsSession1PreUUID = "albert-sixsessions-test-s1pre_1f2-ea55asdfawdfayyh";
        string albertSixsessionsSession1PostUUID = "albert-sixsessions-test-s1post_1f2-ea55asdfawdfayyh";

        Session albertSixsessionsSession1 = new()
        {
            SessionID = albertSixsessionsSession1UUID,
            PatientID = albertSixsessions,
            DateTaken = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Location = "NA"
        };

        PatientEvaluation albertSixsessionsSession1PreEval = new()
        {
            SessionID = albertSixsessionsSession1.SessionID,
            EvaluationID = albertSixsessionsSession1PreUUID,
            EvalType = "pre",
            HeadLat = -1,
            HeadAnt = -1,
            ElbowExtension = -1,
            HipFlex = -1,
            KneeFlex = -1,
            Lumbar = -1,
            Pelvic = -2,
            PelvicTilt = -2,
            Thoracic = -2,
            Trunk = -2,
            TrunkInclination = -2
        };

        PatientEvaluation albertSixsessionsSession1PostEval = new()
        {
            SessionID = albertSixsessionsSession1.SessionID,
            EvaluationID = albertSixsessionsSession1PostUUID,
            EvalType = "post",
            HeadLat = 0,
            HeadAnt = 0,
            ElbowExtension = 1,
            HipFlex = 1,
            KneeFlex = 0,
            Lumbar = 0,
            Pelvic = -2,
            PelvicTilt = -2,
            Thoracic = -2,
            Trunk = -2,
            TrunkInclination = -2
        };

        // integration session2 data
        string albertSixsessionsSession2UUID = "albert-sixsessions-test-s2_e42a56a1awf";
        string albertSixsessionsSession2PreUUID = "albert-sixsessions-test-s2pre_asdf-gnnklgnker";
        string albertSixsessionsSession2PostUUID = "albert-sixsessions-test-s2post_alfk-lntewa";

        Session albertSixsessionsSession2 = new()
        {
            SessionID = albertSixsessionsSession2UUID,
            PatientID = albertSixsessions,
            DateTaken = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            Location = "NA"
        };

        PatientEvaluation albertSixsessionsSession2PreEval = new()
        {
            SessionID = albertSixsessionsSession2.SessionID,
            EvaluationID = albertSixsessionsSession2PreUUID,
            EvalType = "pre",
            HeadLat = 1,
            HeadAnt = 1,
            ElbowExtension = 1,
            HipFlex = 1,
            KneeFlex = 1,
            Lumbar = 1,
            Pelvic = -1,
            PelvicTilt = -1,
            Thoracic = -1,
            Trunk = -1,
            TrunkInclination = -1
        };

        PatientEvaluation albertSixsessionsSession2PostEval = new()
        {
            SessionID = albertSixsessionsSession2.SessionID,
            EvaluationID = albertSixsessionsSession2PostUUID,
            EvalType = "post",
            HeadLat = 2,
            HeadAnt = 2,
            ElbowExtension = 2,
            HipFlex = 2,
            KneeFlex = 2,
            Lumbar = 2,
            Pelvic = -2,
            PelvicTilt = -2,
            Thoracic = -2,
            Trunk = -2,
            TrunkInclination = -2
        };

        // integration session3 data
        string albertSixsessionsSession3UUID = "albert-sixsessions-test-s3_e42a56asfa1awf";
        string albertSixsessionsSession3PreUUID = "albert-sixsessions-test-s3pre_asdf-gnnfklgnker";
        string albertSixsessionsSession3PostUUID = "albert-sixsessions-test-s3post_alfk-lnfdstewa";

        Session albertSixsessionsSession3 = new()
        {
            SessionID = albertSixsessionsSession3UUID,
            PatientID = albertSixsessions,
            DateTaken = new DateTime(2024, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            Location = "NA"
        };

        PatientEvaluation albertSixsessionsSession3PreEval = new()
        {
            SessionID = albertSixsessionsSession3.SessionID,
            EvaluationID = albertSixsessionsSession3PreUUID,
            EvalType = "pre",
            HeadLat = 1,
            HeadAnt = 1,
            ElbowExtension = 1,
            HipFlex = 2,
            KneeFlex = 2,
            Lumbar = -1,
            Pelvic = -1,
            PelvicTilt = 0,
            Thoracic = 0,
            Trunk = 2,
            TrunkInclination = 2
        };

        PatientEvaluation albertSixsessionsSession3PostEval = new()
        {
            SessionID = albertSixsessionsSession3.SessionID,
            EvaluationID = albertSixsessionsSession3PostUUID,
            EvalType = "post",
            HeadLat = 2,
            HeadAnt = 2,
            ElbowExtension = 1,
            HipFlex = 2,
            KneeFlex = 1,
            Lumbar = 1,
            Pelvic = 0,
            PelvicTilt = 0,
            Thoracic = 0,
            Trunk = 2,
            TrunkInclination = 0
        };

        // integration session4 data
        string albertSixsessionsSession4UUID = "albert-sixsessions-test-s4_e42a56asfa1awf";
        string albertSixsessionsSession4PreUUID = "albert-sixsessions-test-s4pre_asdf-gnnfklgnker";
        string albertSixsessionsSession4PostUUID = "albert-sixsessions-test-s4post_alfk-lnfdstewa";

        Session albertSixsessionsSession4 = new()
        {
            SessionID = albertSixsessionsSession4UUID,
            PatientID = albertSixsessions,
            DateTaken = new DateTime(2024, 4, 6, 0, 0, 0, DateTimeKind.Utc),
            Location = "NA"
        };

        PatientEvaluation albertSixsessionsSession4PreEval = new()
        {
            SessionID = albertSixsessionsSession4.SessionID,
            EvaluationID = albertSixsessionsSession4PreUUID,
            EvalType = "pre",
            HeadLat = 2,
            HeadAnt = 2,
            ElbowExtension = 0,
            HipFlex = 0,
            KneeFlex = 1,
            Lumbar = 1,
            Pelvic = 0,
            PelvicTilt = 0,
            Thoracic = 0,
            Trunk = 0,
            TrunkInclination = 0
        };

        PatientEvaluation albertSixsessionsSession4PostEval = new()
        {
            SessionID = albertSixsessionsSession4.SessionID,
            EvaluationID = albertSixsessionsSession4PostUUID,
            EvalType = "post",
            HeadLat = 1,
            HeadAnt = 1,
            ElbowExtension = 0,
            HipFlex = 0,
            KneeFlex = 2,
            Lumbar = 1,
            Pelvic = 0,
            PelvicTilt = 0,
            Thoracic = 2,
            Trunk = 0,
            TrunkInclination = 0
        };

        // integration session5 data
        string albertSixsessionsSession5UUID = "albert-sixsessions-test-s5_e42a56aasfa1awf";
        string albertSixsessionsSession5PreUUID = "albert-sixsessions-test-s5pre_asdf-gnnffewklgnker";
        string albertSixsessionsSession5PostUUID = "albert-sixsessions-test-s5post_alfk-lnfajjdstewa";

        Session albertSixsessionsSession5 = new()
        {
            SessionID = albertSixsessionsSession5UUID,
            PatientID = albertSixsessions,
            DateTaken = new DateTime(2024, 5, 6, 0, 0, 0, DateTimeKind.Utc),
            Location = "NA"
        };

        PatientEvaluation albertSixsessionsSession5PreEval = new()
        {
            SessionID = albertSixsessionsSession5.SessionID,
            EvaluationID = albertSixsessionsSession5PreUUID,
            EvalType = "pre",
            HeadLat = 2,
            HeadAnt = 1,
            ElbowExtension = 1,
            HipFlex = 1,
            KneeFlex = 0,
            Lumbar = 0,
            Pelvic = 0,
            PelvicTilt = 0,
            Thoracic = 0,
            Trunk = 0,
            TrunkInclination = 0
        };

        PatientEvaluation albertSixsessionsSession5PostEval = new()
        {
            SessionID = albertSixsessionsSession5.SessionID,
            EvaluationID = albertSixsessionsSession5PostUUID,
            EvalType = "post",
            HeadLat = 2,
            HeadAnt = 2,
            ElbowExtension = 2,
            HipFlex = 1,
            KneeFlex = 1,
            Lumbar = -1,
            Pelvic = -1,
            PelvicTilt = -1,
            Thoracic = 0,
            Trunk = 0,
            TrunkInclination = 0
        };

        // integration session6 data
        string albertSixsessionsSession6UUID = "albert-sixsessions-test-s6_e42a56aasfa1awf";
        string albertSixsessionsSession6PreUUID = "albert-sixsessions-test-s6pre_asdf-gnnffewklgnker";

        Session albertSixsessionsSession6 = new()
        {
            SessionID = albertSixsessionsSession6UUID,
            PatientID = albertSixsessions,
            DateTaken = new DateTime(2024, 5, 28, 0, 0, 0, DateTimeKind.Utc),
            Location = "NA"
        };

        PatientEvaluation albertSixsessionsSession6PreEval = new()
        {
            SessionID = albertSixsessionsSession6.SessionID,
            EvaluationID = albertSixsessionsSession6PreUUID,
            EvalType = "pre",
            HeadLat = -1,
            HeadAnt = -1,
            ElbowExtension = 0,
            HipFlex = 0,
            KneeFlex = 1,
            Lumbar = 1,
            Pelvic = -1,
            PelvicTilt = 0,
            Thoracic = 0,
            Trunk = 0,
            TrunkInclination = 0
        };
        
        PatientPrivate bethanyLarson = new()
        {
            Id = "patient-bethany-larson-fdsfadfda",
            FName = "Bethany",
            LName = "Larson",
            Condition = "Autism",
            Phone = "123-456-1565",
            Age = 13,
            Weight = 40,
            Height = 60,
            Email = "bethany.larson@saskpolytech.ca",
            DoctorPhoneNumber = "555-123-4567",
            TherapistID = therapist1.TherapistID
        };
        
        PatientPrivate astonHahn = new()
        {
            Id = "patient-aston-hahn-fdsfadfda",
            FName = "Aston",
            LName = "Hahn",
            Condition = "Autism",
            Phone = "123-456-1565",
            Age = 13,
            Weight = 40,
            Height = 60,
            Email = "aston.hahn@saskpolytech.ca",
            DoctorPhoneNumber = "555-123-4567",
            TherapistID = therapist1.TherapistID
        };

        await IntegrationTestDataController.AddPatient(bethanyLarson);
        await IntegrationTestDataController.AddPatient(astonHahn);

        // Add albert Oneeval's data
        await IntegrationTestDataController.AddPatient(albertSixsessionsPatientPrivate);

        // Add sessions
        await IntegrationTestDataController.AddSession(albertSixsessionsPatientPrivate, albertSixsessionsSession1);
        await IntegrationTestDataController.AddSession(albertSixsessionsPatientPrivate, albertSixsessionsSession2);
        await IntegrationTestDataController.AddSession(albertSixsessionsPatientPrivate, albertSixsessionsSession3);
        await IntegrationTestDataController.AddSession(albertSixsessionsPatientPrivate, albertSixsessionsSession4);
        await IntegrationTestDataController.AddSession(albertSixsessionsPatientPrivate, albertSixsessionsSession5);
        await IntegrationTestDataController.AddSession(albertSixsessionsPatientPrivate, albertSixsessionsSession6);

        // Add evaluations
        await IntegrationTestDataController.AddEvaluation(albertSixsessionsPatientPrivate,
            albertSixsessionsSession1PreEval, albertSixsessionsSession1);
        await IntegrationTestDataController.AddEvaluation(albertSixsessionsPatientPrivate,
            albertSixsessionsSession1PostEval, albertSixsessionsSession1);

        await IntegrationTestDataController.AddEvaluation(albertSixsessionsPatientPrivate,
            albertSixsessionsSession2PreEval, albertSixsessionsSession2);
        await IntegrationTestDataController.AddEvaluation(albertSixsessionsPatientPrivate,
            albertSixsessionsSession2PostEval, albertSixsessionsSession2);

        await IntegrationTestDataController.AddEvaluation(albertSixsessionsPatientPrivate,
            albertSixsessionsSession3PreEval, albertSixsessionsSession3);
        await IntegrationTestDataController.AddEvaluation(albertSixsessionsPatientPrivate,
            albertSixsessionsSession3PostEval, albertSixsessionsSession3);

        await IntegrationTestDataController.AddEvaluation(albertSixsessionsPatientPrivate,
            albertSixsessionsSession4PreEval, albertSixsessionsSession4);
        await IntegrationTestDataController.AddEvaluation(albertSixsessionsPatientPrivate,
            albertSixsessionsSession4PostEval, albertSixsessionsSession4);

        await IntegrationTestDataController.AddEvaluation(albertSixsessionsPatientPrivate,
            albertSixsessionsSession5PreEval, albertSixsessionsSession5);
        await IntegrationTestDataController.AddEvaluation(albertSixsessionsPatientPrivate,
            albertSixsessionsSession5PostEval, albertSixsessionsSession5);

        await IntegrationTestDataController.AddEvaluation(albertSixsessionsPatientPrivate,
            albertSixsessionsSession6PreEval, albertSixsessionsSession6);
    }
}