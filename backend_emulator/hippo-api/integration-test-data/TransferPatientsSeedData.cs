using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using HippoApi.Controllers;
using HippoApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace HippoApi.integration_test_data;

public class TransferPatientsSeedData
{
    private const string DefaultPassword = "Password1!";
    private static OwnerController _ownerController;
    private static IntegrationTestDataController _integrationTestDataController;
    private static FirestoreDb _firestoreDb;
    private static AuthController _authController;

    public static async Task SeedData(FirestoreDb firestoreDb)
    {
        _ownerController = new OwnerController(firestoreDb);
        _integrationTestDataController = new IntegrationTestDataController(firestoreDb);
        _firestoreDb = firestoreDb;
        _authController = new AuthController(_firestoreDb);

        // Register Owners

        OwnerRegistrationRequest ownerRegRequest1 = new()
        {
            Email = "owner@test.com",
            FName = "John",
            LName = "DoeOne",
            Password = "Password1!"
        };

        OwnerRegistrationRequest ownerRegRequest2 = new()
        {
            Email = "owner2@test.com",
            FName = "John",
            LName = "DoeTwo",
            Password = "Password1!"
        };

        ObjectResult owner1res = await _authController.RegisterOwner(ownerRegRequest1) as ObjectResult;
        // verify their email
        string link1 = await _authController.GetVerificationUrl(ownerRegRequest1.Email);
        HttpClient client = new();
        await client.GetAsync(link1);

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
    }
}