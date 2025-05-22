using Google.Cloud.Firestore;
using HippoApi.Controllers;
using HippoApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace HippoApi.integration_test_data;

public static class OwnerTherapistSeedData
{
    private static FirestoreDb _firestoreDb;
    private static AuthController _authController;
    private static IntegrationTestDataController _integrationTestDataController;

    public static async Task<Dictionary<string, string>> SeedOwnerInfoLoginAndTherapistListPage(FirestoreDb firestoreDb)
    {
        try
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
                Verified = true,
            };

            OwnerRegistrationRequest ownerRegRequest2 = new()
            {
                Email = "owner2@test.com",
                FName = "John",
                LName = "DoeTwo",
                Password = "Password1!",
                Verified = true,
            };


            OkObjectResult owner1res = await _authController.RegisterOwner(ownerRegRequest1) as OkObjectResult;
            OkObjectResult owner2res = await _authController.RegisterOwner(ownerRegRequest2) as OkObjectResult;

            string owner1Id = owner1res.Value.ToString();
            string owner2Id = owner2res.Value.ToString();

            var refObj = await _authController.GenerateReferral(new ReferralRequest
                { Email = "johnsmith1@test.com", OwnerId = owner1res.Value.ToString() });

            var refInfo = (refObj as OkObjectResult).Value as List<String>;

            // Register Therapists
            TherapistRegistrationRequest owner1TherapistRegRequest1 = new()
            {
                Email = "johnsmith1@test.com",
                FName = "John",
                LName = "SmithOne",
                OwnerId = owner1Id,
                Password = "Password1!",
                Verified = true,
                Referral = refInfo[1],
            };

            refObj = await _authController.GenerateReferral(new ReferralRequest
                { Email = "johnsmith2@test.com", OwnerId = owner1res.Value.ToString() });

            refInfo = (refObj as OkObjectResult).Value as List<String>;

            TherapistRegistrationRequest owner1TherapistRegRequest2 = new()
            {
                Email = "johnsmith2@test.com",
                FName = "John",
                LName = "SmithTwo",
                OwnerId = owner1Id,
                Password = "Password1!",
                Verified = true,
                Referral = refInfo[1],
            };

            await _authController.RegisterTherapist(owner1TherapistRegRequest1);
            Console.WriteLine("Successfully registered: " + owner1TherapistRegRequest1.Email + "with referral code" +
                              owner1TherapistRegRequest1.Referral);
            await _authController.RegisterTherapist(owner1TherapistRegRequest2);
            Console.WriteLine("Successfully registered: " + owner1TherapistRegRequest2.Email + "with referral code" +
                              owner1TherapistRegRequest2.Referral);

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
                TherapistID = "therapist-one",
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
                TherapistID = "therapist-one"
            };

            Therapist firstTherapist = new()
            {
                TherapistID = "therapist-one",
                Email = "firsttherapist@test.com",
                FName = "Ron",
                LName = "Johnson",
            };

            Therapist secondTherapist = new()
            {
                TherapistID = "therapist-two",
                Email = "secondtherapist@test.com",
                FName = "Dwight",
                LName = "Eisenhower",
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

            return new Dictionary<string, string>
            {
                { "owner1WithUniqueId", owner1Id },
                { "owner2WithUniqueId", owner2Id }
            };
        }
        catch (Exception e)
        {
        }

        return new Dictionary<string, string>();
    }
}