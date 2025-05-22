using FirebaseAdmin.Auth;
using Google.Api.Gax;
using Google.Cloud.Firestore;
using HippoApi;
using HippoApi.Controllers;
using HippoApi.Models;
using HippoApi.models.custom_responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using tests.Models;
using OkObjectResult = Microsoft.AspNetCore.Mvc.OkObjectResult;

namespace tests;

[TestFixture]
public class AuthControllerTests
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
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
        // instantiate controller and collection
        _integrationTestDataController = new IntegrationTestDataController(_firestoreDb);
        _authController = new AuthController(_firestoreDb);

        // Clear existing therapists
        await _integrationTestDataController.ClearFirebaseAuthenticationEmulatorDataAsync();
        await _integrationTestDataController.ClearFirestoreEmulatorDataAsync();

        // generate referrals
        validReferral = new ReferralRequest { Email = "therapist@mail.com", OwnerId = "owner" };
        invalidReferral = new ReferralRequest { Email = "wrong@fo", OwnerId = "owner" };
    }

    [TearDown]
    public async Task TearDown()
    {
        await _integrationTestDataController.ClearFirebaseAuthenticationEmulatorDataAsync();
        await _integrationTestDataController.ClearFirestoreEmulatorDataAsync();
        await ClearTherapistsCollection();
    }

    [SetUp]
    public async Task Setup()
    {
        _authController = new AuthController(_firestoreDb);
    }

    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        // Clear existing therapists
        await _integrationTestDataController.ClearFirebaseAuthenticationEmulatorDataAsync();
        await _integrationTestDataController.ClearFirestoreEmulatorDataAsync();
        _testServer.Dispose();
        _client.Dispose();
    }

    private TestServer _testServer;
    private IntegrationTestDataController _integrationTestDataController;
    private AuthController _authController;
    private HttpClient _client;
    private FirestoreDb _firestoreDb;
    private ReferralRequest invalidReferral;
    private ReferralRequest validReferral;

    private async Task ClearTherapistsCollection()
    {
        CollectionReference? collection = _firestoreDb.Collection("therapists");
        QuerySnapshot? snapshot = await collection.GetSnapshotAsync();
        foreach (DocumentSnapshot? doc in snapshot.Documents) await doc.Reference.DeleteAsync();
    }


    // Method to generate unique emails for tests
    private string GenerateUniqueEmail(int length = 20)
    {
        string uniquePart = Guid.NewGuid().ToString("N").Substring(0, length - 12);
        return uniquePart + "@example.com";
    }

    [Test]
    public async Task GenerateReferralInvalidEmailFormat()
    {
        IActionResult? referralObj = await _authController.GenerateReferral(invalidReferral);

        // should throw error that email is invalid
        Assert.That(referralObj, Is.TypeOf<NotFoundObjectResult>());
    }

    // SENDING THE EMAIL AND GETTING THE CORRECT FORMAT AND PROPER CLICKING LINK
    [Test]
    public async Task RegisterTherapist_MissingFirstName_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "john.doe@example.com",
            Password = "Password1!",
            FName = "",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("FName"), Is.True, "ModelState should contain an error for FName.");
            Assert.That(((string[])errors["FName"])[0], Is.EqualTo("First name is required."),
                "Incorrect error message for FName.");
        }
    }

    [Test]
    public async Task RegisterTherapist_FName_ExceedsMaxLength_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = new string('A', 21), // Exceeds 20 characters
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("FName"), Is.True, "ModelState should contain an error for FName.");
            Assert.That(((string[])errors["FName"])[0], Is.EqualTo("First name must be at most 20 characters."),
                "Incorrect error message for FName.");
        }
    }

    [Test]
    public async Task RegisterTherapist_FName_AtMaxLength_ReturnsOk()
    {
        string email = GenerateUniqueEmail();
        OkObjectResult? refObj =
            await _authController.GenerateReferral(new ReferralRequest { Email = email, OwnerId = "12345" }) as
                OkObjectResult;
        List<string>? refInfo = refObj.Value as List<string>;

        TherapistRegistrationRequest validRequest = new()
        {
            Email = email,
            Password = "Password1!",
            FName = new string('A', 20), // Exactly 20 characters
            LName = "Doe",
            Verified = true,
            Referral = refInfo[1]
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }

    [Test]
    public async Task RegisterTherapist_FName_BelowMaxLength_ReturnsOk()
    {
        string email = GenerateUniqueEmail();
        OkObjectResult? refObj =
            await _authController.GenerateReferral(new ReferralRequest { Email = email, OwnerId = "12345" }) as
                OkObjectResult;
        List<string>? refInfo = refObj.Value as List<string>;

        TherapistRegistrationRequest validRequest = new()
        {
            Email = email,
            Password = "Password1!",
            FName = new string('A', 19), // 19 characters
            LName = "Doe",
            Verified = true,
            Referral = refInfo[1]
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }

    [Test]
    public async Task RegisterTherapist_FName_InvalidCharacters_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "John123", // Contains numbers
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("FName"), Is.True, "ModelState should contain an error for FName.");
            Assert.That(((string[])errors["FName"])[0], Is.EqualTo("First name must contain only letters."),
                "Incorrect error message for FName.");
        }
    }

    [Test]
    public async Task RegisterTherapist_MissingLastName_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "jane.doe@example.com",
            Password = "Password1!",
            FName = "Jane",
            LName = ""
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("LName"), Is.True, "ModelState should contain an error for LName.");
            Assert.That(((string[])errors["LName"])[0], Is.EqualTo("Last name is required."),
                "Incorrect error message for LName.");
        }
    }

    [Test]
    public async Task RegisterTherapist_LName_ExceedsMaxLength_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "John",
            LName = new string('B', 21) // Exceeds 20 characters
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("LName"), Is.True, "ModelState should contain an error for LName.");
            Assert.That(((string[])errors["LName"])[0], Is.EqualTo("Last name must be at most 20 characters."),
                "Incorrect error message for LName.");
        }
    }

    [Test]
    public async Task RegisterTherapist_LName_AtMaxLength_ReturnsOk()
    {
        string email = GenerateUniqueEmail();
        OkObjectResult? refObj =
            await _authController.GenerateReferral(new ReferralRequest { Email = email, OwnerId = "12345" }) as
                OkObjectResult;
        List<string>? refInfo = refObj.Value as List<string>;

        TherapistRegistrationRequest validRequest = new()
        {
            Email = email,
            Password = "Password1!",
            FName = "John",
            LName = new string('B', 20), // exactly 20 characters
            Verified = true,
            Referral = refInfo[1]
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }

    [Test]
    public async Task RegisterTherapist_LName_BelowMaxLength_ReturnsOk()
    {
        string email = GenerateUniqueEmail();
        OkObjectResult? refObj =
            await _authController.GenerateReferral(new ReferralRequest { Email = email, OwnerId = "12345" }) as
                OkObjectResult;
        List<string>? refInfo = refObj.Value as List<string>;

        TherapistRegistrationRequest validRequest = new()
        {
            Email = email,
            Password = "Password1!",
            FName = "John",
            LName = new string('B', 19), // 19 characters
            Verified = true,
            Referral = refInfo[1]
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }

    [Test]
    public async Task RegisterTherapist_LName_InvalidCharacters_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "John",
            LName = "Doe123" // Contains numbers
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("LName"), Is.True, "ModelState should contain an error for LName.");
            Assert.That(((string[])errors["LName"])[0], Is.EqualTo("Last name must contain only letters."),
                "Incorrect error message for LName.");
        }
    }

    [Test]
    public async Task RegisterTherapist_Country_ExceedsMaxLength_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Country = new string('C', 21) // Exceeds 20 characters
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Country"), Is.True, "ModelState should contain an error for Country.");
            Assert.That(((string[])errors["Country"])[0], Is.EqualTo("Country must not exceed 20 characters."),
                "Incorrect error message for Country.");
        }
    }

    [Test]
    public async Task RegisterTherapist_Country_InvalidCharacters_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Country = "US@A" // Contains invalid character '@'
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Country"), Is.True, "ModelState should contain an error for Country.");
            Assert.That(((string[])errors["Country"])[0],
                Is.EqualTo("Country must contain only letters and spaces."),
                "Incorrect error message for Country.");
        }
    }

    [Test]
    public async Task RegisterTherapist_Country_AtMaxLength_ReturnsOk()
    {
        string email = GenerateUniqueEmail();
        OkObjectResult? refObj =
            await _authController.GenerateReferral(new ReferralRequest { Email = email, OwnerId = "12345" }) as
                OkObjectResult;
        List<string>? refInfo = refObj.Value as List<string>;

        TherapistRegistrationRequest validRequest = new()
        {
            Email = email,
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Country = new string('C', 20), // Exactly 20 characters
            Verified = true,
            Referral = refInfo[1]
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }

    [Test]
    public async Task RegisterTherapist_City_ExceedsMaxLength_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            City = new string('C', 21) // Exceeds 20 characters
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("City"), Is.True, "ModelState should contain an error for City.");
            Assert.That(((string[])errors["City"])[0], Is.EqualTo("City must not exceed 20 characters."),
                "Incorrect error message for City.");
        }
    }

    [Test]
    public async Task RegisterTherapist_City_InvalidCharacters_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            City = "New York1" // Contains number '1'
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("City"), Is.True, "ModelState should contain an error for City.");
            Assert.That(((string[])errors["City"])[0], Is.EqualTo("City must contain only letters and spaces."),
                "Incorrect error message for City.");
        }
    }

    [Test]
    public async Task RegisterTherapist_City_AtMaxLength_ReturnsOk()
    {
        string email = GenerateUniqueEmail();
        OkObjectResult? refObj =
            await _authController.GenerateReferral(new ReferralRequest { Email = email, OwnerId = "12345" }) as
                OkObjectResult;
        List<string>? refInfo = refObj.Value as List<string>;

        TherapistRegistrationRequest validRequest = new()
        {
            Email = email,
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            City = new string('C', 20), // Exactly 20 characters
            Verified = true,
            Referral = refInfo[1]
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }

    [Test]
    public async Task RegisterTherapist_Street_ExceedsMaxLength_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Street = new string('S', 21) // Exceeds 20 characters
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Street"), Is.True, "ModelState should contain an error for Street.");
            Assert.That(((string[])errors["Street"])[0], Is.EqualTo("Street must not exceed 20 characters."),
                "Incorrect error message for Street.");
        }
    }

    [Test]
    public async Task RegisterTherapist_Street_InvalidCharacters_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Street = "123 Main St!@" // Contains invalid characters '!@'
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Street"), Is.True, "ModelState should contain an error for Street.");
            Assert.That(((string[])errors["Street"])[0],
                Is.EqualTo("Street must contain only letters, numbers, spaces, and common punctuation."),
                "Incorrect error message for Street.");
        }
    }

    [Test]
    public async Task RegisterTherapist_PostalCode_InvalidFormat_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            PostalCode = "12345" // Invalid format
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("PostalCode"), Is.True,
                "ModelState should contain an error for PostalCode.");
            Assert.That(((string[])errors["PostalCode"])[0],
                Is.EqualTo("Postal code should be in the form L#L #L#."),
                "Incorrect error message for PostalCode.");
        }
    }

    [Test]
    public async Task RegisterTherapist_PostalCode_ValidFormat_ReturnsOk()
    {
        string email = GenerateUniqueEmail();
        OkObjectResult? refObj =
            await _authController.GenerateReferral(new ReferralRequest { Email = email, OwnerId = "12345" }) as
                OkObjectResult;
        List<string>? refInfo = refObj.Value as List<string>;

        TherapistRegistrationRequest validRequest = new()
        {
            Email = email,
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            PostalCode = "K1A 0B1", // Valid format
            Verified = true,
            Referral = refInfo[1]
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }

    [Test]
    public async Task RegisterTherapist_Phone_InvalidFormat_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Phone = "123-ABC-7890" // Invalid format
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Phone"), Is.True, "ModelState should contain an error for Phone.");
            Assert.That(((string[])errors["Phone"])[0],
                Is.EqualTo("Please enter a valid phone number (e.g., +1-555-555-5555)."),
                "Incorrect error message for Phone.");
        }
    }

    [Test]
    public async Task RegisterTherapist_Phone_ValidFormat_ReturnsOk()
    {
        string email = GenerateUniqueEmail();
        OkObjectResult? refObj =
            await _authController.GenerateReferral(new ReferralRequest { Email = email, OwnerId = "12345" }) as
                OkObjectResult;
        List<string>? refInfo = refObj.Value as List<string>;

        TherapistRegistrationRequest validRequest = new()
        {
            Email = email,
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Phone = "+1-555-555-5555", // Valid format
            Verified = true,
            Referral = refInfo[1]
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }


    [Test]
    public async Task RegisterTherapist_Street_AtMaxLength_ReturnsOk()
    {
        string email = GenerateUniqueEmail();
        OkObjectResult? refObj =
            await _authController.GenerateReferral(new ReferralRequest { Email = email, OwnerId = "12345" }) as
                OkObjectResult;
        List<string>? refInfo = refObj.Value as List<string>;

        TherapistRegistrationRequest validRequest = new()
        {
            Email = email,
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Street = new string('S', 20), // Exactly 20 characters
            Verified = true,
            Referral = refInfo[1]
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }

    [Test]
    public async Task RegisterTherapist_Profession_ExceedsMaxLength_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Profession = new string('P', 26) // Exceeds 25 characters
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Profession"), Is.True,
                "ModelState should contain an error for Profession.");
            Assert.That(((string[])errors["Profession"])[0],
                Is.EqualTo("Profession must not exceed 25 characters."), "Incorrect error message for Profession.");
        }
    }

    [Test]
    public async Task RegisterTherapist_Profession_InvalidCharacters_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Profession = "Phys!cal Therapist" // Contains '!'
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Profession"), Is.True,
                "ModelState should contain an error for Profession.");
            Assert.That(((string[])errors["Profession"])[0],
                Is.EqualTo("Profession must contain only letters and spaces."),
                "Incorrect error message for Profession.");
        }
    }

    [Test]
    public async Task RegisterTherapist_Profession_AtMaxLength_ReturnsOk()
    {
        string email = GenerateUniqueEmail();
        OkObjectResult? refObj =
            await _authController.GenerateReferral(new ReferralRequest { Email = email, OwnerId = "12345" }) as
                OkObjectResult;
        List<string>? refInfo = refObj.Value as List<string>;

        TherapistRegistrationRequest validRequest = new()
        {
            Email = email,
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Profession = new string('P', 25), // Exactly 25 characters
            Verified = true,
            Referral = refInfo[1]
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }

    [Test]
    public async Task RegisterTherapist_Major_ExceedsMaxLength_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Major = new string('M', 26) // Exceeds 25 characters
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Major"), Is.True, "ModelState should contain an error for Major.");
            Assert.That(((string[])errors["Major"])[0], Is.EqualTo("Major must not exceed 25 characters."),
                "Incorrect error message for Major.");
        }
    }

    [Test]
    public async Task RegisterTherapist_Major_InvalidCharacters_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Major = "Psychol0gy" // Contains '0'
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Major"), Is.True, "ModelState should contain an error for Major.");
            Assert.That(((string[])errors["Major"])[0], Is.EqualTo("Major must contain only letters and spaces."),
                "Incorrect error message for Major.");
        }
    }

    [Test]
    public async Task RegisterTherapist_Major_AtMaxLength_ReturnsOk()
    {
        string email = GenerateUniqueEmail();
        OkObjectResult? refObj =
            await _authController.GenerateReferral(new ReferralRequest { Email = email, OwnerId = "12345" }) as
                OkObjectResult;
        List<string>? refInfo = refObj.Value as List<string>;

        TherapistRegistrationRequest validRequest = new()
        {
            Email = email,
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Major = new string('M', 25), // Exactly 25 characters
            Verified = true,
            Referral = refInfo[1]
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }

    [Test]
    public async Task RegisterTherapist_YearsExperience_BelowMin_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            YearsExperienceInHippotherapy = -1 // Below 0
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("YearsExperienceInHippotherapy"), Is.True,
                "ModelState should contain an error for YearsExperienceInHippotherapy.");
            Assert.That(((string[])errors["YearsExperienceInHippotherapy"])[0],
                Is.EqualTo("Years of experience must be an integer between 0 and 100."),
                "Incorrect error message for YearsExperienceInHippotherapy.");
        }
    }

    [Test]
    public async Task RegisterTherapist_YearsExperience_AboveMax_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            YearsExperienceInHippotherapy = 101 // Above 100
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("YearsExperienceInHippotherapy"), Is.True,
                "ModelState should contain an error for YearsExperienceInHippotherapy.");
            Assert.That(((string[])errors["YearsExperienceInHippotherapy"])[0],
                Is.EqualTo("Years of experience must be an integer between 0 and 100."),
                "Incorrect error message for YearsExperienceInHippotherapy.");
        }
    }

    [Test]
    public async Task RegisterTherapist_YearsExperience_AtMin_ReturnsOk()
    {
        string email = GenerateUniqueEmail();
        OkObjectResult? refObj =
            await _authController.GenerateReferral(new ReferralRequest { Email = email, OwnerId = "12345" }) as
                OkObjectResult;
        List<string>? refInfo = refObj.Value as List<string>;

        TherapistRegistrationRequest validRequest = new()
        {
            Email = email,
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            YearsExperienceInHippotherapy = 0, // Exactly 0
            Verified = true,
            Referral = refInfo[1]
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }

    [Test]
    public async Task RegisterTherapist_YearsExperience_AtMax_ReturnsOk()
    {
        string email = GenerateUniqueEmail();
        OkObjectResult? refObj =
            await _authController.GenerateReferral(new ReferralRequest { Email = email, OwnerId = "12345" }) as
                OkObjectResult;
        List<string>? refInfo = refObj.Value as List<string>;

        TherapistRegistrationRequest validRequest = new()
        {
            Email = email,
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            YearsExperienceInHippotherapy = 100, // Exactly 100
            Verified = true,
            Referral = refInfo[1]
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }


    [Test]
    public async Task RegisterTherapist_Password_BelowMinLength_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "P@1a", // Below 5 characters
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Password"), Is.True,
                "ModelState should contain an error for Password.");
            Assert.That(((string[])errors["Password"])[0],
                Is.EqualTo("Password must be 5-20 characters long."));
        }
    }

    [Test]
    public async Task RegisterTherapist_Password_AtMinLength_ReturnsOk()
    {
        string email = GenerateUniqueEmail();

        OkObjectResult? refObj =
            await _authController.GenerateReferral(new ReferralRequest { Email = email, OwnerId = "12345" }) as
                OkObjectResult;
        List<string>? refInfo = refObj.Value as List<string>;

        TherapistRegistrationRequest validRequest = new()
        {
            Email = email,
            Password = "P@1aA1", // Exactly 6 characters with required complexity
            FName = "John",
            LName = "Doe",
            Verified = true,
            Referral = refInfo[1]
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }

    [Test]
    public async Task RegisterTherapist_Password_ExceedsMaxLength_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = GenerateUniqueEmail(),
            Password = new string('A', 21), // Exceeds 20 characters
            FName = "John",
            LName = "Doe",
            Verified = true
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Password"), Is.True,
                "ModelState should contain an error for Password.");
            Assert.That(((string[])errors["Password"])[0], Is.EqualTo("Password must be 5-20 characters long."),
                "Incorrect error message for password exceeding max length.");
        }
    }

    [Test]
    public async Task RegisterTherapist_Password_AtMaxLength_ReturnsOk()
    {
        string email = GenerateUniqueEmail();

        OkObjectResult? refObj =
            await _authController.GenerateReferral(new ReferralRequest { Email = email, OwnerId = "12345" }) as
                OkObjectResult;
        List<string>? refInfo = refObj.Value as List<string>;

        Console.WriteLine(refInfo);

        TherapistRegistrationRequest validRequest = new()
        {
            Email = email,
            Password = "P@ssw0rd1234567890!", // Exactly 20 characters with required complexity
            FName = "John",
            LName = "Doe",
            Verified = true,
            Referral = refInfo[1],
            OwnerId = refInfo[0]
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(validRequest);
        Console.WriteLine((result as ObjectResult).StatusCode);
        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }


    [Test]
    public async Task RegisterTherapist_MissingPassword_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = GenerateUniqueEmail(),
            Password = "",
            FName = "Jane",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Password"), Is.True,
                "ModelState should contain an error for Password.");
            Assert.That(((string[])errors["Password"])[0], Is.EqualTo("Password is required."),
                "Incorrect error message for Password.");
        }
    }

    [Test]
    public async Task RegisterTherapist_InvalidEmail_ReturnsErrorMessage()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "invalidemail.com",
            Password = "Password1!",
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Email"), Is.True, "ModelState should contain an error for Email.");
            Assert.That(((string[])errors["Email"])[0], Is.EqualTo("Invalid email format."),
                "Incorrect error message for Email.");
        }
    }

    [Test]
    public async Task RegisterTherapist_MissingEmail_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            // Missing Email
            Password = "Password1!",
            FName = "John",
            LName = "Doe"
        };

        // Validate model 
        await TestValidationHelper.ValidateModel(invalidRequest, _authController);


        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Email"), Is.True, "ModelState should contain an error for Email.");

            Assert.That(((string[])errors["Email"])[0], Is.EqualTo("Email is required."),
                "Incorrect error message for Email.");
        }
    }

    [Test]
    public async Task RegisterTherapist_Email_ExceedsMaxLength_ReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = new string('a', 31) + "@example.com", // Exceeds 30 characters
            Password = "Password1!",
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Email"), Is.True, "ModelState should contain an error for Email.");
            Assert.That(((string[])errors["Email"])[0], Is.EqualTo("Email must not exceed 30 characters."),
                "Incorrect error message for Email.");
        }
    }

    [Test]
    public async Task RegisterTherapist_Email_AtMaxLength_ReturnsOk()
    {
        string email = GenerateUniqueEmail(30);
        OkObjectResult? refObj =
            await _authController.GenerateReferral(new ReferralRequest { Email = email, OwnerId = "12345" }) as
                OkObjectResult;
        List<string>? refInfo = refObj.Value as List<string>;

        TherapistRegistrationRequest validRequest = new()
        {
            Email = email,
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Verified = true,
            Referral = refInfo[1]
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }


    [Test]
    public async Task RegisterTherapist_WithExistingEmail_ReturnsBadRequest()
    {
        // Generate a unique email
        string email = $"email{Guid.NewGuid()}@example.com";
        OkObjectResult? refObj =
            await _authController.GenerateReferral(new ReferralRequest { Email = email, OwnerId = "12345" }) as
                OkObjectResult;
        List<string>? refInfo = refObj.Value as List<string>;

        TherapistRegistrationRequest validRequest = new()
        {
            Email = email,
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Verified = true,
            Referral = refInfo[1]
        };

        // First registration should succeed
        IActionResult registerResult1 = await _authController.RegisterTherapist(validRequest);
        Assert.That(registerResult1, Is.TypeOf<OkObjectResult>(), "First registration should return Ok.");

        // Second registration with the same email should fail
        IActionResult registerResult2 = await _authController.RegisterTherapist(validRequest);
        Assert.That(registerResult2, Is.TypeOf<BadRequestObjectResult>(),
            "Second registration should return BadRequest.");

        // Deserialize the BadRequestObjectResult value
        if (registerResult2 is BadRequestObjectResult badRequest)
        {
            string jsonResponse = JsonConvert.SerializeObject(badRequest.Value);
            Dictionary<string, string>? errorResponse =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonResponse);


            // Validate the error message
            Assert.That(errorResponse, Is.Not.Null, "Expected error response.");
            Assert.That(errorResponse.ContainsKey("Message"), Is.True, "Error response should contain 'Message'.");
            Assert.That(errorResponse["Message"], Is.EqualTo($"Email {email} is already registered."),
                "Incorrect error message for existing email.");
        }
    }


    [Test]
    public async Task RegisterTherapist_PassNoUpper_ReturnsErrorMessage()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "pas%11",
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(),
            "Registration with password missing uppercase should return BadRequest.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Password"), Is.True,
                "ModelState should contain an error for Password.");
            Assert.That(((string[])errors["Password"])[0],
                Is.EqualTo(
                    "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character."));
        }
    }

    [Test]
    public async Task RegisterTherapist_PassNoLower_ReturnsErrorMessage()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "PAS%11",
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(),
            "Registration with password missing lowercase should return BadRequest.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Password"), Is.True,
                "ModelState should contain an error for Password.");
            Assert.That(((string[])errors["Password"])[0],
                Is.EqualTo(
                    "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character."));
        }
    }

    [Test]
    public async Task RegisterTherapist_PassNoNumber_ReturnsErrorMessage()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "PASs%a",
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(),
            "Registration with password missing number should return BadRequest.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Password"), Is.True,
                "ModelState should contain an error for Password.");
            Assert.That(((string[])errors["Password"])[0],
                Is.EqualTo(
                    "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character."));
        }
    }

    [Test]
    public async Task RegisterTherapist_PassNoSpecial_ReturnsErrorMessage()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "pasS111",
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(),
            "Registration with password missing special character should return BadRequest.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Password"), Is.True,
                "ModelState should contain an error for Password.");
            Assert.That(((string[])errors["Password"])[0],
                Is.EqualTo(
                    "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character."));
        }
    }

    [Test]
    public async Task RegisterTherapist_ValidRequest_StoresDataInFirestore()
    {
        string email = $"john.doe{Guid.NewGuid()}@example.com";
        OkObjectResult? refObj =
            await _authController.GenerateReferral(new ReferralRequest { Email = email, OwnerId = "12345" }) as
                OkObjectResult;
        List<string>? refInfo = refObj.Value as List<string>;

        TherapistRegistrationRequest validRequest = new()
        {
            FName = "John",
            LName = "Doe",
            Email = email,
            Password = "Password1!",
            Country = "USA",
            City = "New York",
            Street = "123 Main St",
            PostalCode = "10001",
            Phone = "+1-555-555-5555",
            Profession = "Physical Therapist",
            Major = "Hippotherapy",
            YearsExperienceInHippotherapy = 5,
            Verified = true,
            Referral = refInfo[1]
        };

        IActionResult result = await _authController.RegisterTherapist(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Valid registration should return Ok.");

        OkObjectResult? okResult = result as OkObjectResult;
        Console.WriteLine(okResult.Value);
        Assert.That(okResult.Value, Is.Not.Null, "OkObjectResult.Value should not be null.");


        // extract the token and id. 
        // Deserialize to anonymous type using dynamic
        string json = JsonConvert.SerializeObject(okResult.Value);
        RegistrationResponse res = JsonConvert.DeserializeObject<RegistrationResponse>(json);

        // Expect a string UID instead of an object with Message and UID
        string? uid = res.uid;

        Assert.That(uid, Is.Not.Null.Or.Empty, "UID should be a non-null, non-empty string.");

        DocumentSnapshot? doc = await _firestoreDb.Collection("owners").Document("12345").Collection("therapists")
            .Document(uid).GetSnapshotAsync();
        Assert.That(doc.Exists, Is.True, "Therapist document should exist in Firestore.");

        Therapist? therapist = doc.ConvertTo<Therapist>();
        Assert.That(therapist.FName, Is.EqualTo(validRequest.FName), "FName mismatch.");
        Assert.That(therapist.LName, Is.EqualTo(validRequest.LName), "LName mismatch.");
        Assert.That(therapist.Email, Is.EqualTo(validRequest.Email), "Email mismatch.");
        Assert.That(therapist.Country, Is.EqualTo(validRequest.Country), "Country mismatch.");
        Assert.That(therapist.City, Is.EqualTo(validRequest.City), "City mismatch.");
        Assert.That(therapist.Street, Is.EqualTo(validRequest.Street), "Street mismatch.");
        Assert.That(therapist.PostalCode, Is.EqualTo(validRequest.PostalCode), "PostalCode mismatch.");
        Assert.That(therapist.Phone, Is.EqualTo(validRequest.Phone), "Phone mismatch.");
        Assert.That(therapist.Profession, Is.EqualTo(validRequest.Profession), "Profession mismatch.");
        Assert.That(therapist.Major, Is.EqualTo(validRequest.Major), "Major mismatch.");
        Assert.That(therapist.YearsExperienceInHippotherapy, Is.EqualTo(validRequest.YearsExperienceInHippotherapy),
            "YearsExperienceInHippotherapy mismatch.");
    }

    [Test]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        string email = $"testuser{Guid.NewGuid()}@example.com";
        OkObjectResult? refObj =
            await _authController.GenerateReferral(new ReferralRequest { Email = email, OwnerId = "12345" }) as
                OkObjectResult;
        List<string>? refInfo = refObj.Value as List<string>;

        TherapistRegistrationRequest validRegistration = new()
        {
            Email = email,
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Verified = true,
            Referral = refInfo[1]
        };

        IActionResult registerResult = await _authController.RegisterTherapist(validRegistration);
        Assert.That(registerResult, Is.TypeOf<OkObjectResult>(), "Registration should return Ok.");

        LoginRequest loginRequest = new()
        {
            Email = email,
            Password = "Password1!"
        };

        IActionResult loginResult = await _authController.LoginTherapist(loginRequest);

        Assert.That(loginResult, Is.TypeOf<OkObjectResult>(), "Valid login should return Ok.");

        OkObjectResult? okResult = loginResult as OkObjectResult;
        Assert.That(okResult.Value, Is.Not.Null, "OkObjectResult.Value should not be null.");

        Dictionary<string, string>? response = JsonConvert.DeserializeObject<Dictionary<string, string>>(
            JsonConvert.SerializeObject(okResult.Value)
        );

        Assert.That(response.ContainsKey("Token"), Is.True, "Response should contain 'Token'.");
        Assert.That(response["Token"], Is.Not.Null.And.Not.Empty, "Token should not be null or empty.");
    }

    [Test]
    public async Task Login_WithIncorrectPassword_ReturnsUnauthorized()
    {
        string email = $"testuser{Guid.NewGuid()}@example.com";
        TherapistRegistrationRequest validRegistration = new()
        {
            Email = email,
            Password = "Password1!",
            FName = "John",
            LName = "Doe"
        };
        await _authController.RegisterTherapist(validRegistration);

        LoginRequest loginRequest = new()
        {
            Email = email,
            Password = "WrongPassword123!"
        };

        IActionResult loginResult = await _authController.LoginTherapist(loginRequest);

        Assert.That(loginResult, Is.TypeOf<UnauthorizedObjectResult>(),
            "Login with incorrect password should return Unauthorized.");

        if (loginResult is UnauthorizedObjectResult unauthorizedResult)
        {
            Assert.That(unauthorizedResult.Value, Is.Not.Null,
                "UnauthorizedObjectResult.Value should not be null.");

            Dictionary<string, string>? response = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                JsonConvert.SerializeObject(unauthorizedResult.Value)
            );
            Assert.That(response.ContainsKey("Message"), Is.True, "Response should contain 'Message'.");
            Assert.That(response["Message"], Is.EqualTo("Invalid email or password."),
                "Incorrect error message for unauthorized login.");
        }
    }

    [Test]
    public async Task Login_InvalidToken_ReturnsUnauthorized()
    {
        LoginRequest loginRequest = new()
        {
            Email = "nonexistent.user@example.com",
            Password = "invalid_token"
        };

        IActionResult loginResult = await _authController.LoginTherapist(loginRequest);

        Assert.That(loginResult, Is.TypeOf<UnauthorizedObjectResult>(),
            "Login with invalid token should return Unauthorized.");

        if (loginResult is UnauthorizedObjectResult unauthorizedResult)
        {
            Assert.That(unauthorizedResult.Value, Is.Not.Null,
                "UnauthorizedObjectResult.Value should not be null.");

            Dictionary<string, string>? response = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                JsonConvert.SerializeObject(unauthorizedResult.Value)
            );

            Assert.That(response.ContainsKey("Message"), Is.True, "Response should contain 'Message'.");
            Assert.That(response["Message"], Is.EqualTo("Invalid email or password."),
                "Incorrect error message for invalid token.");
        }
    }

    /*
     * Owner Authentication Tests
     */

    [Test]
    public async Task OwnerRegisterWithValidDetailsReturnsOk()
    {
        OwnerRegistrationRequest validRequest = new()
        {
            Email = GenerateUniqueEmail(),
            Password = "Password1!",
            FName = "John",
            LName = "Smith"
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }

    [Test]
    public async Task RegisterOwnerMissingFirstNameReturnsBadRequest()
    {
        OwnerRegistrationRequest invalidRequest = new()
        {
            Email = "john.doe@example.com",
            Password = "Password1!",
            FName = "",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("FName"), Is.True, "ModelState should contain an error for FName.");
            Assert.That(((string[])errors["FName"])[0], Is.EqualTo("First name is required."),
                "Incorrect error message for FName.");
        }
    }

    [Test]
    public async Task RegisterOwnerMissingLastNameReturnsBadRequest()
    {
        OwnerRegistrationRequest invalidRequest = new()
        {
            Email = "john.doe@example.com",
            Password = "Password1!",
            FName = "John",
            LName = ""
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("LName"), Is.True, "ModelState should contain an error for LName.");
            Assert.That(((string[])errors["LName"])[0], Is.EqualTo("Last name is required."),
                "Incorrect error message for LName.");
        }
    }

    [Test]
    public async Task RegisterOwnerFNameExceedsMaxLengthReturnsBadRequest()
    {
        OwnerRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "JohnWhoseNameIsLargerThanMaxLength",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("FName"), Is.True, "ModelState should contain an error for FName.");
            Assert.That(((string[])errors["FName"])[0], Is.EqualTo("First name must be at most 20 characters."),
                "Incorrect error message for FName.");
        }
    }

    [Test]
    public async Task RegisterOwner_FName_AtMaxLength_ReturnsOk()
    {
        OwnerRegistrationRequest validRequest = new()
        {
            Email = GenerateUniqueEmail(),
            Password = "Password1!",
            FName = new string('A', 20), // Exactly 20 characters
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }

    [Test]
    public async Task RegisterOwner_FName_BelowMaxLength_ReturnsOk()
    {
        OwnerRegistrationRequest validRequest = new()
        {
            Email = GenerateUniqueEmail(),
            Password = "Password1!",
            FName = new string('A', 19), // 19 characters
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }

    [Test]
    public async Task RegisterOwner_FName_InvalidCharacters_ReturnsBadRequest()
    {
        OwnerRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "John123", // Contains numbers
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("FName"), Is.True, "ModelState should contain an error for FName.");
            Assert.That(((string[])errors["FName"])[0], Is.EqualTo("First name must contain only letters."),
                "Incorrect error message for FName.");
        }
    }

    [Test]
    public async Task RegisterOwnerLNameExceedsMaxLengthReturnsBadRequest()
    {
        OwnerRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "John",
            LName = "DoeWhoseNameIsLargerThanMaxLength"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("LName"), Is.True, "ModelState should contain an error for LName.");
            Assert.That(((string[])errors["LName"])[0], Is.EqualTo("Last name must be at most 20 characters."),
                "Incorrect error message for LName.");
        }
    }

    [Test]
    public async Task RegisterOwner_LName_AtMaxLength_ReturnsOk()
    {
        OwnerRegistrationRequest validRequest = new()
        {
            Email = GenerateUniqueEmail(),
            Password = "Password1!",
            FName = "John",
            LName = new string('A', 20)
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }

    [Test]
    public async Task RegisterOwner_LName_BelowMaxLength_ReturnsOk()
    {
        OwnerRegistrationRequest validRequest = new()
        {
            Email = GenerateUniqueEmail(),
            Password = "Password1!",
            FName = "John",
            LName = new string('A', 19)
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }

    [Test]
    public async Task RegisterOwner_LName_InvalidCharacters_ReturnsBadRequest()
    {
        OwnerRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "Password1!",
            FName = "John", // Contains numbers
            LName = "Doe123"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("LName"), Is.True, "ModelState should contain an error for LName.");
            Assert.That(((string[])errors["LName"])[0], Is.EqualTo("Last name must contain only letters."),
                "Incorrect error message for LName.");
        }
    }

    [Test]
    public async Task RegisterOwnerMissingEmailReturnsBadRequest()
    {
        OwnerRegistrationRequest invalidRequest = new()
        {
            // Missing Email
            Password = "Password1!",
            FName = "John",
            LName = "Doe"
        };

        // Validate model 
        await TestValidationHelper.ValidateModel(invalidRequest, _authController);


        IActionResult result = await _authController.RegisterOwner(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Email"), Is.True, "ModelState should contain an error for Email.");

            Assert.That(((string[])errors["Email"])[0], Is.EqualTo("Email is required."),
                "Incorrect error message for Email.");
        }
    }

    [Test]
    public async Task RegisterOwnerInvalidEmailReturnsErrorMessage()
    {
        OwnerRegistrationRequest invalidRequest = new()
        {
            Email = "invalidemail.com",
            Password = "Password1!",
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Email"), Is.True, "ModelState should contain an error for Email.");
            Assert.That(((string[])errors["Email"])[0], Is.EqualTo("Invalid email format."),
                "Incorrect error message for Email.");
        }
    }

    [Test]
    public async Task RegisterOwnerEmailExceedsMaxLengthReturnsBadRequest()
    {
        TherapistRegistrationRequest invalidRequest = new()
        {
            Email = new string('a', 31) + "@example.com", // Exceeds 30 characters
            Password = "Password1!",
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterTherapist(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Email"), Is.True, "ModelState should contain an error for Email.");
            Assert.That(((string[])errors["Email"])[0], Is.EqualTo("Email must not exceed 30 characters."),
                "Incorrect error message for Email.");
        }
    }

    [Test]
    public async Task RegisterOwnerInvalidPasswordReturnsErrorMessage()
    {
        OwnerRegistrationRequest invalidRequest = new()
        {
            Email = GenerateUniqueEmail(),
            Password = "invalidpassword",
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Password"), Is.True, "ModelState should contain an error for Password.");
            Assert.That(((string[])errors["Password"])[0],
                Is.EqualTo(
                    "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character."),
                "Incorrect error message for Password.");
        }
    }

    [Test]
    public async Task OwnerLoginInvalidTokenReturnsUnauthorized()
    {
        LoginRequest loginRequest = new()
        {
            Email = "nonexistent.user@example.com",
            Password = "invalid_token"
        };

        IActionResult loginResult = await _authController.LoginOwner(loginRequest);

        Assert.That(loginResult, Is.TypeOf<UnauthorizedObjectResult>(),
            "Login with invalid token should return Unauthorized.");

        if (loginResult is UnauthorizedObjectResult unauthorizedResult)
        {
            Assert.That(unauthorizedResult.Value, Is.Not.Null,
                "UnauthorizedObjectResult.Value should not be null.");

            Dictionary<string, string>? response = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                JsonConvert.SerializeObject(unauthorizedResult.Value)
            );

            Assert.That(response.ContainsKey("Message"), Is.True, "Response should contain 'Message'.");
            Assert.That(response["Message"], Is.EqualTo("Invalid email or password."),
                "Incorrect error message for invalid token.");
        }
    }

    [Test]
    public async Task OwnerLoginWithIncorrectPasswordReturnsUnauthorized()
    {
        string email = $"testuser{Guid.NewGuid()}@example.com";
        OwnerRegistrationRequest validRegistration = new()
        {
            Email = email,
            Password = "Password1!",
            FName = "John",
            LName = "Doe"
        };
        await _authController.RegisterOwner(validRegistration);

        LoginRequest loginRequest = new()
        {
            Email = email,
            Password = "WrongPassword123!"
        };

        IActionResult loginResult = await _authController.LoginOwner(loginRequest);

        Assert.That(loginResult, Is.TypeOf<UnauthorizedObjectResult>(),
            "Login with incorrect password should return Unauthorized.");

        if (loginResult is UnauthorizedObjectResult unauthorizedResult)
        {
            Assert.That(unauthorizedResult.Value, Is.Not.Null,
                "UnauthorizedObjectResult.Value should not be null.");

            Dictionary<string, string>? response = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                JsonConvert.SerializeObject(unauthorizedResult.Value)
            );
            Assert.That(response.ContainsKey("Message"), Is.True, "Response should contain 'Message'.");
            Assert.That(response["Message"], Is.EqualTo("Invalid email or password."),
                "Incorrect error message for unauthorized login.");
        }
    }

    [Test]
    public async Task OwnerLoginValidCredentialsReturnsToken()
    {
        string email = $"testuser{Guid.NewGuid()}@example.com";

        OwnerRegistrationRequest validRegistration = new()
        {
            Email = email,
            Password = "Password1!",
            FName = "John",
            LName = "Doe"
        };

        IActionResult registerResult = await _authController.RegisterOwner(validRegistration);
        Assert.That(registerResult, Is.TypeOf<OkObjectResult>(), "Registration should return Ok.");

        // Verify the user
        string ownerUrl = await _authController.GetVerificationUrl(email);
        using HttpClient client = new();
        await client.GetAsync(ownerUrl);

        LoginRequest loginRequest = new()
        {
            Email = email,
            Password = "Password1!"
        };

        IActionResult loginResult = await _authController.LoginOwner(loginRequest);

        Assert.That(loginResult, Is.TypeOf<OkObjectResult>(), "Valid login should return Ok.");

        OkObjectResult? okResult = loginResult as OkObjectResult;
        Assert.That(okResult.Value, Is.Not.Null, "OkObjectResult.Value should not be null.");

        Dictionary<string, string>? response = JsonConvert.DeserializeObject<Dictionary<string, string>>(
            JsonConvert.SerializeObject(okResult.Value)
        );

        Assert.That(response.ContainsKey("Token"), Is.True, "Response should contain 'Token'.");
        Assert.That(response["Token"], Is.Not.Null.And.Not.Empty, "Token should not be null or empty.");
    }

    [Test]
    public async Task RegisterOwner_Password_BelowMinLength_ReturnsBadRequest()
    {
        OwnerRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "P@1a", // Below 5 characters
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Password"), Is.True,
                "ModelState should contain an error for Password.");
            Assert.That(((string[])errors["Password"])[0],
                Is.EqualTo("Password must be 5-20 characters long."));
        }
    }

    [Test]
    public async Task RegisterOwner_Password_AtMinLength_ReturnsOk()
    {
        OwnerRegistrationRequest validRequest = new()
        {
            Email = GenerateUniqueEmail(),
            Password = "P@1aA1", // Exactly 6 characters with required complexity
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }

    [Test]
    public async Task RegisterOwner_Password_ExceedsMaxLength_ReturnsBadRequest()
    {
        OwnerRegistrationRequest invalidRequest = new()
        {
            Email = GenerateUniqueEmail(),
            Password = new string('A', 21), // Exceeds 20 characters
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Password"), Is.True,
                "ModelState should contain an error for Password.");
            Assert.That(((string[])errors["Password"])[0], Is.EqualTo("Password must be 5-20 characters long."),
                "Incorrect error message for password exceeding max length.");
        }
    }

    [Test]
    public async Task RegisterOwner_Password_AtMaxLength_ReturnsOk()
    {
        OwnerRegistrationRequest validRequest = new()
        {
            Email = GenerateUniqueEmail(),
            Password = "P@ssw0rd1234567890!", // Exactly 20 characters with required complexity
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }


    [Test]
    public async Task RegisterOwner_MissingPassword_ReturnsBadRequest()
    {
        OwnerRegistrationRequest invalidRequest = new()
        {
            Email = GenerateUniqueEmail(),
            Password = "",
            FName = "Jane",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Password"), Is.True,
                "ModelState should contain an error for Password.");
            Assert.That(((string[])errors["Password"])[0], Is.EqualTo("Password is required."),
                "Incorrect error message for Password.");
        }
    }

    [Test]
    public async Task RegisterOwner_InvalidEmail_ReturnsErrorMessage()
    {
        OwnerRegistrationRequest invalidRequest = new()
        {
            Email = "invalidemail.com",
            Password = "Password1!",
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Email"), Is.True, "ModelState should contain an error for Email.");
            Assert.That(((string[])errors["Email"])[0], Is.EqualTo("Invalid email format."),
                "Incorrect error message for Email.");
        }
    }

    [Test]
    public async Task RegisterOwner_MissingEmail_ReturnsBadRequest()
    {
        OwnerRegistrationRequest invalidRequest = new()
        {
            // Missing Email
            Password = "Password1!",
            FName = "John",
            LName = "Doe"
        };

        // Validate model 
        await TestValidationHelper.ValidateModel(invalidRequest, _authController);


        IActionResult result = await _authController.RegisterOwner(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Email"), Is.True, "ModelState should contain an error for Email.");

            Assert.That(((string[])errors["Email"])[0], Is.EqualTo("Email is required."),
                "Incorrect error message for Email.");
        }
    }

    [Test]
    public async Task RegisterOwner_Email_ExceedsMaxLength_ReturnsBadRequest()
    {
        OwnerRegistrationRequest invalidRequest = new()
        {
            Email = new string('a', 31) + "@example.com", // Exceeds 30 characters
            Password = "Password1!",
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(), "Expected a BadRequestObjectResult.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Email"), Is.True, "ModelState should contain an error for Email.");
            Assert.That(((string[])errors["Email"])[0], Is.EqualTo("Email must not exceed 30 characters."),
                "Incorrect error message for Email.");
        }
    }

    [Test]
    public async Task RegisterOwner_Email_AtMaxLength_ReturnsOk()
    {
        OwnerRegistrationRequest validRequest = new()
        {
            Email = GenerateUniqueEmail(30),
            Password = "Password1!",
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(validRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(validRequest);

        Assert.That(result, Is.TypeOf<OkObjectResult>(), "Expected an OkObjectResult.");
    }


    [Test]
    public async Task RegisterOwner_WithExistingEmail_ReturnsBadRequest()
    {
        // Generate a unique email
        string email = $"email{Guid.NewGuid()}@example.com";
        OwnerRegistrationRequest validRequest = new()
        {
            Email = email,
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Verified = true
        };

        // First registration should succeed
        IActionResult registerResult1 = await _authController.RegisterOwner(validRequest);
        Assert.That(registerResult1, Is.TypeOf<OkObjectResult>(), "First registration should return Ok.");

        // Second registration with the same email should fail
        IActionResult registerResult2 = await _authController.RegisterOwner(validRequest);
        Assert.That(registerResult2, Is.TypeOf<BadRequestObjectResult>(),
            "Second registration should return BadRequest.");

        // Deserialize the BadRequestObjectResult value
        if (registerResult2 is BadRequestObjectResult badRequest)
        {
            string jsonResponse = JsonConvert.SerializeObject(badRequest.Value);
            Dictionary<string, string>? errorResponse =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonResponse);

            // Validate the error message
            Assert.That(errorResponse, Is.Not.Null, "Expected error response.");
            Assert.That(errorResponse.ContainsKey("Message"), Is.True, "Error response should contain 'Message'.");
            Assert.That(errorResponse["Message"], Is.EqualTo($"Email {email} is already registered."),
                "Incorrect error message for existing email.");
        }
    }

    [Test]
    public async Task RegisterOwner_WithExistingTherapistEmail_ReturnsBadRequest()
    {
        OkObjectResult? refObj = await _authController.GenerateReferral(new ReferralRequest
            { Email = $"john.doe{Guid.NewGuid()}@example.com", OwnerId = "12345" }) as OkObjectResult;
        List<string>? refInfo = refObj.Value as List<string>;

        // Generate a unique email
        string email = $"email{Guid.NewGuid()}@example.com";
        TherapistRegistrationRequest validRequest = new()
        {
            Email = email,
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Verified = true,
            Referral = refInfo[1]
        };

        // First registration should succeed
        IActionResult registerResult1 = await _authController.RegisterTherapist(validRequest);
        Assert.That(registerResult1, Is.TypeOf<OkObjectResult>(), "First registration should return Ok.");

        OwnerRegistrationRequest invalidRequest = new()
        {
            Email = email,
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Verified = true
        };

        // Second registration with the same email should fail
        IActionResult registerResult2 = await _authController.RegisterOwner(invalidRequest);
        Assert.That(registerResult2, Is.TypeOf<BadRequestObjectResult>(),
            "Second registration should return BadRequest.");

        // Deserialize the BadRequestObjectResult value
        if (registerResult2 is BadRequestObjectResult badRequest)
        {
            string jsonResponse = JsonConvert.SerializeObject(badRequest.Value);
            Dictionary<string, string>? errorResponse =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonResponse);

            // Validate the error message
            Assert.That(errorResponse, Is.Not.Null, "Expected error response.");
            Assert.That(errorResponse.ContainsKey("Message"), Is.True, "Error response should contain 'Message'.");
            Assert.That(errorResponse["Message"], Is.EqualTo($"Email {email} is already registered."),
                "Incorrect error message for existing email.");
        }
    }


    [Test]
    public async Task RegisterOwner_PassNoUpper_ReturnsErrorMessage()
    {
        OwnerRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "pas%11",
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(),
            "Registration with password missing uppercase should return BadRequest.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Password"), Is.True,
                "ModelState should contain an error for Password.");
            Assert.That(((string[])errors["Password"])[0],
                Is.EqualTo(
                    "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character."));
        }
    }

    [Test]
    public async Task RegisterOwner_PassNoLower_ReturnsErrorMessage()
    {
        OwnerRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "PAS%11",
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(),
            "Registration with password missing lowercase should return BadRequest.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Password"), Is.True,
                "ModelState should contain an error for Password.");
            Assert.That(((string[])errors["Password"])[0],
                Is.EqualTo(
                    "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character."));
        }
    }

    [Test]
    public async Task RegisterOwner_PassNoNumber_ReturnsErrorMessage()
    {
        OwnerRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "PASs%a",
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(),
            "Registration with password missing number should return BadRequest.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Password"), Is.True,
                "ModelState should contain an error for Password.");
            Assert.That(((string[])errors["Password"])[0],
                Is.EqualTo(
                    "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character."));
        }
    }

    [Test]
    public async Task RegisterOwner_PassNoSpecial_ReturnsErrorMessage()
    {
        OwnerRegistrationRequest invalidRequest = new()
        {
            Email = "testuser@example.com",
            Password = "pasS111",
            FName = "John",
            LName = "Doe"
        };

        await TestValidationHelper.ValidateModel(invalidRequest, _authController);
        IActionResult result = await _authController.RegisterOwner(invalidRequest);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>(),
            "Registration with password missing special character should return BadRequest.");

        if (result is BadRequestObjectResult badRequest)
        {
            SerializableError? errors = badRequest.Value as SerializableError;
            Assert.That(errors, Is.Not.Null, "Expected SerializableError in BadRequestObjectResult.");

            Assert.That(errors.ContainsKey("Password"), Is.True,
                "ModelState should contain an error for Password.");
            Assert.That(((string[])errors["Password"])[0],
                Is.EqualTo(
                    "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character."));
        }
    }

    [Test]
    public async Task GetVerfUrl_Returns_Url_For_Therapist()
    {
        // get verification url
        string url = await _authController.GetVerificationUrl("unverifiedtherapist@test.com");
        Assert.That(url, Is.Not.Null);
    }

    [Test]
    public async Task GetVerfUrl_Returns_Url_For_Owner()
    {
        // get verification url
        string url = await _authController.GetVerificationUrl("unverifiedowner@test.com");
        Assert.That(url, Is.Not.Null);
    }


    [Test]
    public async Task GetVerfUrl_Returns_Errors_When_No_Email_Found()
    {
        string message = await _authController.GetVerificationUrl("EmailNotInDatabase@example.com");
        Assert.That(message, Is.EqualTo("No user record found for the given identifier (USER_NOT_FOUND)."));
    }

    [Test]
    public async Task IsUserVerified_Returns_True_On_Verified_Therapist()
    {
        Therapist verifiedTherapistRequest = new()
        {
            Email = "verified-therapist@test.com",
            FName = "John",
            LName = "SmithOne",
            TherapistID = "verifiedTherapist"
        };

        UserRecordArgs therapist1Args = new()
        {
            Email = verifiedTherapistRequest.Email,
            Password = "Password1!",
            Uid = verifiedTherapistRequest.TherapistID,
            EmailVerified = true
        };

        FirebaseAuth firebaseAuth = FirebaseAuth.DefaultInstance;
        await firebaseAuth.CreateUserAsync(therapist1Args);


        bool testResult = await _authController.IsUserVerified(verifiedTherapistRequest.Email);
        Console.WriteLine(testResult);
        Assert.That(testResult, Is.True);
    }

    [Test]
    public async Task IsUserVerified_Returns_False_On_Unverified_Therapist()
    {
        string email = "unverifiedtherapist@test.com";
        bool testResult = await _authController.IsUserVerified(email);
        Console.WriteLine(testResult);
        Assert.That(testResult, Is.False);
    }

    [Test]
    public async Task Unverified_Therapist_Cannot_Login()
    {
        LoginRequest request = new() { Email = "unverifiedtherapist@test.com", Password = "Password1!" };
        IActionResult loginResult = await _authController.LoginOwner(request);

        Assert.That(loginResult, Is.TypeOf<UnauthorizedObjectResult>(),
            "Please verify you email before logging in. We have resent a verification email");
    }

    [Test]
    public async Task IsUserVerified_Returns_True_On_Verified_Owner()
    {
        OwnerRegistrationRequest verifiedOwnerRequest = new()
        {
            Email = "verifiedOwner@test.com",
            FName = "Verified",
            LName = "Owner",
            Password = "Password1!"
        };
        await _authController.RegisterOwner(verifiedOwnerRequest);

        try
        {
            string ownerUrl = await _authController.GetVerificationUrl("verifiedOwner@test.com");
            using HttpClient client = new();
            await client.GetAsync(ownerUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        string email = "verifiedowner@test.com";
        bool testResult = await _authController.IsUserVerified(email);
        Assert.That(testResult, Is.True);
    }

    [Test]
    public async Task IsUserVerified_Returns_False_On_Unverified_Owner()
    {
        string email = "unverifiedowner@test.com";
        bool testResult = await _authController.IsUserVerified(email);
        Console.WriteLine(testResult);
        Assert.That(testResult, Is.False);
    }

    [Test]
    public async Task Unverified_Owner_Cannot_Login()
    {
        LoginRequest request = new() { Email = "unverifiedowner@test.com", Password = "Password1!" };
        IActionResult loginResult = await _authController.LoginOwner(request);

        Assert.That(loginResult, Is.TypeOf<UnauthorizedObjectResult>(),
            "Please verify you email before logging in. We have resent a verification email");
    }


    [Test]
    public async Task Test_Email()
    {
        OwnerRegistrationRequest unverifiedOwnerRequest2 = new()
        {
            Email = "unverifiedowner2@test.com",
            FName = "unverified",
            LName = "Owner",
            Password = "Password1!"
        };
        try
        {
            await _authController.RegisterOwner(unverifiedOwnerRequest2);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        // gets the status code
        IActionResult sendResult = await _authController.SendVerfEmail("unverifiedowner2@test.com");

        Assert.That(sendResult, Is.Not.Null);
        Assert.That(sendResult.GetType(), Is.EqualTo(typeof(OkResult)));
    }
}