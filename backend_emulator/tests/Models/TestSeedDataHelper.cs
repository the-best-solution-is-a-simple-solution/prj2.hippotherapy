using Google.Cloud.Firestore;
using HippoApi.Controllers;
using HippoApi.Models;

namespace tests.Models;

public class TestSeedDataHelper
{
    private const string EmailDomain = "@test.com";
    private readonly FirestoreDb _firestoreDb;


    public TestSeedDataHelper(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    public Therapist GetTestTherapist(string therapistId, string emailName, string firstName,
        string lastName)
    {
        Therapist therapist = new()
        {
            TherapistID = therapistId,
            Email = emailName + EmailDomain,
            FName = firstName,
            LName = lastName
        };
        return therapist;
    }

    public Owner GetTestOwner(string ownerId, string emailName, string firstName,
        string lastName)
    {
        Owner owner = new()
        {
            OwnerId = ownerId,
            Email = emailName + EmailDomain,
            FName = firstName,
            LName = lastName
        };
        return owner;
    }

    /// <summary>
    ///     Make a patient setting the values provided
    /// </summary>
    public PatientPrivate GetTestPatient(string therapistId, string patientId, string firstName, string lastName)
    {
        PatientPrivate patient = new()
        {
            Id = patientId,
            TherapistID = therapistId,
            FName = firstName,
            LName = lastName,
            Deleted = false,
            Phone = "987-654-321",
            GuardianPhoneNumber = "987-654-322",
            DoctorPhoneNumber = "987-654-323",
            Age = 30,
            Weight = 20,
            Height = 200,
            Condition = "TestCondition",
            Email = $"{patientId}@test.com"
        };
        return patient;
    }

    /// <summary>
    ///     Makes a test evaluation with the default value for every posture
    /// </summary>
    public PatientEvaluation GetEvaluation(string sessionId, string evaluationId, string evalType, int defaultValue)
    {
        PatientEvaluation eval = new()
        {
            SessionID = sessionId,
            EvaluationID = evaluationId,
            EvalType = evalType,
            HeadLat = defaultValue,
            HeadAnt = defaultValue,
            ElbowExtension = defaultValue,
            HipFlex = defaultValue,
            KneeFlex = defaultValue,
            Lumbar = defaultValue,
            Pelvic = defaultValue,
            PelvicTilt = defaultValue,
            Thoracic = defaultValue,
            Trunk = defaultValue,
            TrunkInclination = defaultValue
        };
        return eval;
    }

    /// <summary>
    ///     Creates a patient db record using the id in it
    /// </summary>
    public async Task CreatePatient(PatientPrivate patient)
    {
        try
        {
            await _firestoreDb.Collection(PatientController.COLLECTION_NAME)
                .Document(patient.Id).SetAsync(patient);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error adding patient: {patient.FName} {patient.LName}");
            Console.WriteLine(e);
        }
    }


    /// <summary>
    ///     Creates an owner record in firebase auth, and in firestore db.
    ///     Also adds the owner role
    /// </summary>
    /// <param name="owner">Data to use for Owner</param>
    /// <param name="isVerified">Set if the user's email is verified.</param>
    /// <returns>True if it succeeded, otherwise false</returns>
    public async Task CreateOwner(Owner owner)
    {
        try
        {
            // create the object in firestore db
            await _firestoreDb.Collection(OwnerController.COLLECTION_NAME)
                .Document(owner.OwnerId).SetAsync(owner);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error adding owner: {owner.OwnerId}");
            Console.WriteLine(e);
        }
    }

    /// <summary>
    ///     Creates an owner record in firebase auth, and in firestore db.
    ///     Also adds the therapist role
    /// </summary>
    public async Task CreateTherapist(string ownerId, Therapist therapist)
    {
        try
        {
            // create the object in firestore db
            await _firestoreDb.Collection(OwnerController.COLLECTION_NAME)
                .Document(ownerId)
                .Collection(TherapistController.COLLECTION_NAME)
                .Document(therapist.TherapistID)
                .SetAsync(therapist);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error adding therapist: {therapist.TherapistID} to owner: {ownerId}");
            Console.WriteLine(e);
        }
    }
}