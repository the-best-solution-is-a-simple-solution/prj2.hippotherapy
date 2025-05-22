using System.Net;
using System.Net.Http.Json;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using HippoApi;
using HippoApi.Controllers;
using HippoApi.middleware;
using HippoApi.Models;
using HippoApi.models.custom_responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using tests.Models;

namespace integration_tests;

[TestFixture]
public class AuthorizationTests
{
    #region Global Variables and Constructor
    

    public AuthorizationTests()
    {
        // use a custom Startup to connect to a real firestore database
        _testServer = new TestServer(new WebHostBuilder()
            .ConfigureAppConfiguration((context, builder) =>
            {
                builder.AddJsonFile("appsettings.IntegrationTests.json");
            })
            .UseStartup<StartupProduction>());

        // instantiate db
        _firestoreDb = new FirestoreDbBuilder
        {
            ProjectId = "hippotherapy-706f8"
        }.Build();
        _authController = new AuthController(_firestoreDb);

        _client = _testServer.CreateClient();
    }
    
    private readonly TestServer _testServer;
    private readonly FirestoreDb _firestoreDb;
    private readonly AuthController _authController;
    private TestAuthorizationHelper _helper;
    private readonly HttpClient _client;

    private const string TestPassword = "Password1!";
    private const string TestEmailDomain = "@test.com";
    private const string UnauthorizedMessage = "You are not authorized to access this resource.";

    private TestUserData owner1;
    private TestUserData o1t1;
    private PatientPrivate o1t1p1;
    private PatientPrivate o1t1p2;
    private PatientPrivate o1t1p3;

    private TestUserData o1t2;
    private PatientPrivate o1t2p1;

    private TestUserData owner2;
    private TestUserData o2t1;
    private PatientPrivate o2t1p1;
    
    #endregion

    #region Seed Data
    /// <summary>
    ///     Seed the data in local variables
    /// </summary>
    public async Task SeedDataInLocalVariables()
    {
        // Owner 1
        owner1 = _helper.GetTestOwner("o1-id", "o1", "ownerOne", "twoTherapists");
        //  Therapist 1
        o1t1 = _helper.GetTestTherapist(owner1.OwnerId, "o1t1-id", "o1t1", "therapistOneOwnerOne", "ThreePatients");
        o1t1p1 = _helper.GetTestPatient(o1t1.TherapistId, "o1t1p1-id", "patientOne", "ownerOne");
        o1t1p2 = _helper.GetTestPatient(o1t1.TherapistId, "o1t1p2-id", "patientTwo", "ownerOne");
        o1t1p3 = _helper.GetTestPatient(o1t1.TherapistId, "o1t1p3-id", "patientThree", "ownerOne");

        //  Therapist 2
        o1t2 = _helper.GetTestTherapist(owner1.OwnerId, "o1t2-id", "o1t2", "therapistTwoOwnerOne", "OnePatient");
        o1t2p1 = _helper.GetTestPatient(o1t2.TherapistId, "o1t2p1-id", "patientFour", "ownerOne");

        // Owner 2
        owner2 = _helper.GetTestOwner("o2-id", "o2", "ownerTwo", "oneTherapist");
        o2t1 = _helper.GetTestTherapist(owner2.OwnerId, "o2t1-id", "o2t1", "therapistOwnOwnerTwo", "OnePatient");
        o2t1p1 = _helper.GetTestPatient(o2t1.TherapistId, "o2t1p1-id", "patientFive", "ownerTwo");
    }

    /// <summary>
    ///     Adds login tokens for the owners and therapists
    /// </summary>
    public async Task AddAuthorizationTokensForUsers()
    {
        // Login owners and therapists to get their tokens
        await _helper.AddTokenToOwner(owner1);
        await _helper.AddTokenToOwner(owner2);
        await _helper.AddTokenToTherapist(o1t1);
        await _helper.AddTokenToTherapist(o1t2);
        await _helper.AddTokenToTherapist(o2t1);
        
        // Assert.That(o1t1.Token, Is.Not.Null);
        // Console.WriteLine($"o1t1 token ${o1t1.Token.Substring(0,10)}");
    }

    /// <summary>
    /// Do NOT DELETE will be commented out because data stays in firebase real app
    /// </summary>
    public async Task SeedDataInRealFirebase()
    {
        // TODO: check if the data is already in the db, if so don't add it
        await _helper.CreateOwner(owner1);
        await _helper.CreateTherapist(o1t1);
        await _helper.CreateTherapist(o1t2);

        await _helper.CreatePatient(o1t1p1);
        await _helper.CreatePatient(o1t1p2);
        await _helper.CreatePatient(o1t1p3);

        await _helper.CreatePatient(o1t2p1);

        await _helper.CreateOwner(owner2);
        await _helper.CreateTherapist(o2t1);
        await _helper.CreatePatient(o2t1p1);
        
        OwnerRegistrationRequest ownerRegRequest1 = new()
        {
            Email = "owner@test.com",
            FName = "John",
            LName = "DoeOne",
            Password = "Password1!",
            Verified = true
        };

        ObjectResult owner1res = await _authController.RegisterOwner(ownerRegRequest1) as ObjectResult;

        string owner1Id = owner1res.Value.ToString();

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

        ObjectResult? therapist1Res =
            await _authController.RegisterTherapist(owner1TherapistRegRequest1) as ObjectResult;
        string json = JsonConvert.SerializeObject(therapist1Res.Value);
        RegistrationResponse response = JsonConvert.DeserializeObject<RegistrationResponse>(json);
        string therapist1Id = response.uid;

        string patientWithSessionsID = "patient-with-sessions-0cc9-4539-9b1e-1db2c1163fe1";

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

        await IntegrationTestDataController.AddPatient(johnSmithPatientPrivate);

        string sessionDWithNoEvals = "test-sessionC_964a114b-e810-40-5bb07f46aada";

        Session sessionD = new()
        {
            SessionID = sessionDWithNoEvals,
            PatientID = patientWithSessionsID,
            DateTaken = new DateTime(2020, 12, 31, 4, 20, 09, DateTimeKind.Utc),
            Location = "PA"
        };

        await IntegrationTestDataController.AddSession(johnSmithPatientPrivate, sessionD);


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
    }
    
    #endregion
    
    #region Setup and Teardown

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // Set Environment variables
        // Environment.SetEnvironmentVariable("FIREBASE_PROJECT_ID"); // Needed for ContentOwnerAuthorization.cs

        Console.WriteLine("Status:");
        Console.WriteLine("auth " + FirebaseAuth.DefaultInstance);
        Console.WriteLine("db " + _firestoreDb);

        _helper = new TestAuthorizationHelper(
            new AuthController(_firestoreDb),
            FirebaseAuth.DefaultInstance,
            _firestoreDb,
            _client,
            TestPassword,
            TestEmailDomain,
            UnauthorizedMessage);
        await SeedDataInLocalVariables();

        // Give users a login token
        await AddAuthorizationTokensForUsers();

        //----------If getting errors check if the data and users are in firebase and can uncomment this//
        // TODO: check if the data is already in the db, if so don't add it
        // await SeedDataInRealFirebase();
    }

    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        _client.Dispose();
        _testServer.Dispose();

        // IMPORTANT: I've opted when modifying data (like reassigning patients) to move them, check, and move back
            // This prevents future tests from breaking
        
        // AVOID: submitting valid data, just check it gets past the middleware
    }
    
    #endregion
    
    #region Test Helper Methods
    
    [Test]
    public async Task TestHelperMethod_IsOwnerContentOwnerOfPatientSuceeds()
    {
        ContentOwnerAuthorization auth = new();
        bool res = auth.IsOwnerContentOwnerOfPatient(owner1.OwnerId, o1t1p1.Id).Result;
        Assert.True(res);
    }

    [Test]
    public async Task TestHelperMethod_IsOwnerContentOwnerOfPatientFails()
    {
        ContentOwnerAuthorization auth = new();
        bool res = auth.IsOwnerContentOwnerOfPatient(owner2.OwnerId, o1t1p1.Id).Result;
        Assert.False(res);
    }

    [Test]
    public async Task TestHelperMethod_IsTherapistContentOwnerOfPatientSuceeds()
    {
        ContentOwnerAuthorization auth = new();
        bool res = auth.IsTherapistContentOwnerOfPatient(o1t1.TherapistId, o1t1p1.Id).Result;
        Assert.True(res);
    }

    [Test]
    public async Task TestHelperMethod_IsTherapistContentOwnerOfPatientFails()
    {
        ContentOwnerAuthorization auth = new();
        bool res = auth.IsTherapistContentOwnerOfPatient(o1t2.TherapistId, o1t1p1.Id).Result;
        Assert.False(res);
    }

    [Test]
    public async Task TestHelperMethod_IsOwnerContentOwnerOfTherapistSucceeds()
    {
        ContentOwnerAuthorization auth = new();
        bool res = auth.IsOwnerContentOwnerOfTherapist(owner1.OwnerId, o1t1.TherapistId).Result;
        Assert.True(res);
    }

    [Test]
    public async Task TestHelperMethod_IsOwnerContentOwnerOfTherapistFails()
    {
        ContentOwnerAuthorization auth = new();
        bool res = auth.IsOwnerContentOwnerOfTherapist(owner2.OwnerId, o1t1.TherapistId).Result;
        Assert.False(res);
    }
    
    [Test]
    public async Task TestGetAllTherapistIdsForOwner()
    {
        List<string> therapistIds = await _authController.GetAllTherapistIdsForOwner(owner1.OwnerId);
        Assert.That(therapistIds, Is.Not.Null);
        Console.WriteLine(therapistIds.Count);
        foreach (string therapistId in therapistIds)
        {
            Console.WriteLine(therapistId);
        }
        Assert.That(therapistIds.Count, Is.EqualTo(2));
    }
    
    #endregion
    
    #region Owner Controller
    
    [Test]
    public async Task TestOwnersCanAccessOwnData()
    {
        // Use owner1Token in request
        string route = $"/owners/{owner1.OwnerId}";
        string token = owner1.Token;
        HttpMethod method = HttpMethod.Get;
        Console.WriteLine($"Route: {route} ");
        HttpResponseMessage? res = await _helper.MakeAuthenticatedRequest(route, token, method);

        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // extract the data
        Owner? returnData = JsonConvert.DeserializeObject<Owner>(await res.Content.ReadAsStringAsync());

        // Check that they are the same
        Assert.That(returnData.FName, Is.EqualTo(owner1.FName));
        Assert.That(returnData.LName, Is.EqualTo(owner1.LName));
        Assert.That(returnData.Email, Is.EqualTo(owner1.Email));
    }

    [Test]
    public async Task TestOwnersCanNotAccessOtherOwnersData()
    {
        // Use owner2Token in request
        string route = $"/owners/{owner1.OwnerId}";
        string token = owner2.Token;
        HttpMethod method = HttpMethod.Get;

        HttpResponseMessage? res = _helper.MakeAuthenticatedRequest(route, token, method).Result;

        // Should fail
        // check status code
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        //  check the content
        string content = await res.Content.ReadAsStringAsync();
        Assert.That(content, Is.EqualTo(UnauthorizedMessage));
    }


    [Test]
    public async Task TestOwnerCanReassignPatientsUnderThem()
    {
        // Use owner Token in request
        string route = $"/owners/{owner1.OwnerId}/{o1t1.TherapistId}/{o1t2.TherapistId}";
        string token = owner1.Token;
        HttpMethod method = HttpMethod.Put;
        JsonContent content = JsonContent.Create(new List<string> { o1t1p3.Id });

        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method, content);
        Console.WriteLine(res);

        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        string expectedMessage =
            $"Successfully transferred 1 patient(s) from {o1t1.FName} {o1t1.LName} to {o1t2.FName} {o1t2.LName}";
        String message = await res.Content.ReadAsStringAsync();
        Assert.That(message, Is.EqualTo(expectedMessage), $"Expected: {expectedMessage}, Actual: {message}");
        
        // Transfer back (To avoid breaking future tests)
        // Use owner Token in request
        route = $"/owners/{owner1.OwnerId}/{o1t2.TherapistId}/{o1t1.TherapistId}";
        token = owner1.Token;
        method = HttpMethod.Put;
        content = JsonContent.Create(new List<string> { o1t1p3.Id });

        res = await _helper.MakeAuthenticatedRequest(route, token, method, content);
        Console.WriteLine(res);

        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task TestOwnerCanNotReassignPatientsUnderOtherOwners()
    {
        // Use owner Token in request
        string route = $"/owners/{owner1.OwnerId}/{o1t1.TherapistId}/{o1t2.TherapistId}";
        string token = owner2.Token;
        HttpMethod method = HttpMethod.Put;
        JsonContent content = JsonContent.Create(new List<string> { o1t1p1.Id });

        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method, content);;
        Assert.IsNotNull(res);

        // Check for unauthorized status code
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        // Check response content if needed
        string responseContent = await res.Content.ReadAsStringAsync();
        Assert.That(responseContent, Is.EqualTo(UnauthorizedMessage));
    }
    
    [Test]
    public async Task TestOwnerCanNotReassignPatientsUnderOtherOwners2()
    {
        // Use owner Token in request
        string route = $"/owners/{owner1.OwnerId}/{o1t1.TherapistId}/{o2t1.TherapistId}";
        string token = owner1.Token;
        HttpMethod method = HttpMethod.Put;
        JsonContent content = JsonContent.Create(new List<string> { o1t1p1.Id });

        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method, content);;
        Assert.IsNotNull(res);

        // Check for unauthorized status code
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        // Check response content if needed
        string responseContent = await res.Content.ReadAsStringAsync();
        Assert.That(responseContent, Is.EqualTo("o1-id does not have permission to reassign between o1t1-id and o2t1-id"));
    }

    #endregion
    
    #region Therapist Controller
    
    [Test]
    public async Task TestTherapistCanNotReassignPatients()
    {
        Assert.That(o1t1.Token, Is.Not.Null);
        // Use owner Token in request
        string route = $"/owners/{owner1.OwnerId}/{o1t1.OwnerId}/{o1t2.OwnerId}";
        string token = o1t1.Token;
        HttpMethod method = HttpMethod.Put;
        JsonContent content = JsonContent.Create(new List<string> { o1t1p1.Id });

        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method, content);;
        Console.WriteLine(res);
        string contentString = await res.Content.ReadAsStringAsync();
        Assert.That(contentString, Is.EqualTo(UnauthorizedMessage));
    }

    [Test]
    public async Task TestTherapistCanNotGetListOfTherapistsForAnyOwner()
    {
        Assert.That(o1t1.Token, Is.Not.Null);
        // Use owner Token in request
        string route = $"/owners/{owner1.OwnerId}/therapists";
        string token = o1t1.Token;
        HttpMethod method = HttpMethod.Get;
        Console.WriteLine(route);
        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method);
        Assert.IsNotNull(res);
        // check status code
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        //  check the content
        string content = await res.Content.ReadAsStringAsync();
        Assert.That(content, Is.EqualTo(UnauthorizedMessage));
    }

    [Test]
    public async Task TestOwnersCanAccessTherapistDataForTherapistsUnderThem()
    {
        string route = $"owners/{owner1.OwnerId}/therapists";
        string token = owner1.Token;
        HttpMethod method = HttpMethod.Get;
        Assert.That(token, Is.Not.Null);

        HttpResponseMessage? res = _helper.MakeAuthenticatedRequest(route, token, method).Result;

        // Check for status code
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // extract the data
        Therapist[]? returnData = JsonConvert.DeserializeObject<Therapist[]>(await res.Content.ReadAsStringAsync());
        Assert.That(returnData, Is.Not.Null);

        Assert.That(returnData.Length, Is.EqualTo(2));
        Assert.That(returnData[0].FName, Is.EqualTo(o1t1.FName));
        Assert.That(returnData[0].LName, Is.EqualTo(o1t1.LName));
        Assert.That(returnData[0].Email, Is.EqualTo(o1t1.Email));

        Assert.That(returnData[1].FName, Is.EqualTo(o1t2.FName));
        Assert.That(returnData[1].LName, Is.EqualTo(o1t2.LName));
        Assert.That(returnData[1].Email, Is.EqualTo(o1t2.Email));
    }

    [Test]
    public async Task TherapistRegistrationAndLoginAvailableToUnauthenticated()
    {
        string route = "/auth/therapist/register";
        string token = "invalid_token";
        HttpMethod method = HttpMethod.Post;

        TherapistRegistrationRequest therapistRegister = new()
        {
            Email = "temp@invalid@test.com",
            Password = TestPassword,
            FName = "invalid-info",
            LName = "invalid-info"
        };
        JsonContent content = JsonContent.Create(therapistRegister);

        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method, content);;

        // Assert that it got past the authorization (should have still failed)
        // because don't want to put extra data in database
        Assert.IsNotInstanceOf<UnauthorizedObjectResult>(res);
        Assert.That(res.RequestMessage.ToString(), Is.Not.EqualTo(UnauthorizedMessage));
    }

    [Test]
    public async Task TherapistLoginAvailableToUnauthenticated()
    {
        string route = "/auth/therapist/login";
        string token = "invalid_token";
        HttpMethod method = HttpMethod.Post;

        LoginRequest therapistLogin = new()
        {
            Email = o2t1.Email,
            Password = o2t1.Password
        };
        JsonContent content = JsonContent.Create(therapistLogin);

        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method, content);;

        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));


        // extract the token and id. 
        var loginResponse = JsonConvert.DeserializeObject<TestAuthorizationHelper.LoginResponse>(await res.Content.ReadAsStringAsync());
        Assert.That(loginResponse.Token, Is.Not.Null);
        Assert.That(loginResponse.UserId, Is.EqualTo(o2t1.TherapistId));
    }

    #endregion
    
    #region Register and Login
    
    [Test]
    public async Task OwnerRegistrationAndLoginAvailableToUnauthenticated()
    {
        string route = "/auth/owner/register";
        string token = "invalid_token";
        HttpMethod method = HttpMethod.Post;

        OwnerRegistrationRequest ownerRegister = new()
        {
            Email = "tempowner@test.com",
            Password = TestPassword,
            FName = "testfirstowner",
            LName = "testlastowner"
        };
        JsonContent content = JsonContent.Create(ownerRegister);
        // var res = _client.PostAsync($"/auth/owner/register", content).Result;
        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method, content);;

        // Assert that it got past the authorization (should have still failed)
        // because don't want to put extra data in database
        Assert.That(res.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(res.RequestMessage.ToString(), Is.Not.EqualTo(UnauthorizedMessage));
    }

    [Test]
    public async Task OwnerLoginAvailableToUnauthenticated()
    {
        string route = "/auth/owner/login";
        string token = "invalid_token";
        HttpMethod method = HttpMethod.Post;


        LoginRequest ownerLogin = new()
        {
            Email = owner2.Email,
            Password = owner2.Password
        };
        // content = JsonContent.Create(ownerLogin);

        // res = _client.PostAsync($"/auth/owner/login", content).Result;

        // int statusCode = (int)res.StatusCode;
        // Assert.That(statusCode, Is.EqualTo(200));

        // extract the token and id. 
        // var loginResponse = JsonConvert.DeserializeObject<TestAuthorizationHelper.LoginResponse>(await res.Content.ReadAsStringAsync());
        // Assert.That(loginResponse.token, Is.Not.Null);

        JsonContent content = JsonContent.Create(ownerLogin);
        // var res = _client.PostAsync($"/auth/owner/register", content).Result;
        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method, content);;

        // Assert that it got past the authorization (should have still failed)
        // because don't want to put extra data in database
        Assert.That(res.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // extract the token and id. 
        TestAuthorizationHelper.LoginResponse? loginResponse =
            JsonConvert.DeserializeObject<TestAuthorizationHelper.LoginResponse>(await res.Content.ReadAsStringAsync());
        Assert.That(loginResponse.Token, Is.Not.Null);
    }


    [Test]
    public async Task TestLogoutAccessibleByAuthenticatedUser()
    {
        string route = "/auth/logout";
        string token = owner2.Token;
        HttpMethod method = HttpMethod.Post;
        
        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method);
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Read the response content
        string responseContent = await res.Content.ReadAsStringAsync();
        Console.WriteLine(responseContent);

        // Deserialize the JSON response
        var responseObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);

        // Assert that the message matches
        Assert.That(responseObject["message"], Is.EqualTo("Logout successful."));

        Assert.That(res.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(res.RequestMessage.ToString(), Is.Not.EqualTo(UnauthorizedMessage));
        
        // get new token for owner
        await _helper.AddTokenToOwner(owner2);
    }
    
    

    #endregion

    #region Export Controller Tests

    [Test]
    public async Task TestExportForOtherOwnerDenied()
    {
        string route = $"/export/records/{o1t1.OwnerId}/";
        string token = o1t2.Token;
        HttpMethod method = HttpMethod.Get;

        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method);
        // check status code
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.IsNotNull(res.RequestMessage);

        //  check the content
        string content = await res.Content.ReadAsStringAsync();
        Console.WriteLine(content);
        Assert.That(content, Is.EqualTo(UnauthorizedMessage));
    }

    #endregion
    
    #region Misc. Controller Tests
    
    [Test]
    public async Task TestNoTokenIsBlocked()
    {
        string route = $"/owners/{owner1.OwnerId}";
        string token = null;
        HttpMethod method = HttpMethod.Get;

        HttpResponseMessage? res = _helper.MakeAuthenticatedRequest(route, token, method).Result;

        // check status code
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        //  check the content
        string content = await res.Content.ReadAsStringAsync();
        Assert.That(content, Is.EqualTo(UnauthorizedMessage));
    }


    [Test]
    public async Task TestOtherOwnerIsBlocked()
    {
        string route = $"/owners/{owner1.OwnerId}";
        string token = owner2.Token;
        HttpMethod method = HttpMethod.Get;

        HttpResponseMessage? res = _helper.MakeAuthenticatedRequest(route, token, method).Result;

        // check status code
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        //  check the content
        string content = await res.Content.ReadAsStringAsync();
        Assert.That(content, Is.EqualTo(UnauthorizedMessage));
    }

    [Test]
    public async Task TestAuthenticatedTherapistCanSubmitEvaluation()
    {
        Assert.That(o1t1.Token, Is.Not.Null);
        string route = $"/patientevaluation/{o1t1p1.Id}/submit-evaluation";
        string token = o1t1.Token;
        HttpMethod method = HttpMethod.Post;
        JsonContent content = JsonContent.Create("test");

        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method, content);
        string contentString = await res.Content.ReadAsStringAsync();
        Console.WriteLine(contentString);
        Console.WriteLine("token start: " + token.Substring(0,10));
        Console.WriteLine(route);
        Assert.That(contentString.Contains("One or more validation errors occurred."), contentString);

        // Assert that it got past the authorization (should have still failed)
        // because don't want to put extra data in database
        Assert.That(res.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task TestOtherAuthenticatedTherapistCanNotSubmitEvaluationForPatientNotUnderThem()
    {
        string route = $"/patientevaluation/{o1t1p1.Id}/submit-evaluation";
        string token = o1t2.Token;
        HttpMethod method = HttpMethod.Post;
        PatientEvaluation eval = _helper.GetEvaluation("s1", "e1", "pre", 2);
        JsonContent content = JsonContent.Create(eval);
        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method, content);;

        // check status code
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        //  check the content
        string resContent = await res.Content.ReadAsStringAsync();
        Assert.That(resContent, Is.EqualTo(UnauthorizedMessage));
    }

    [Test]
    public async Task TestAuthenticatedOwnerCanSubmitEvaluation()
    {
        string route = $"/patientevaluation/{o1t1p1.Id}/submit-evaluation";
        string token = owner1.Token;
        HttpMethod method = HttpMethod.Post;

        JsonContent content = JsonContent.Create("test");

        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method, content);;

        // Assert that it got past the authorization (should have still failed)
        // because don't want to put extra data in database
        Assert.That(res.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
        Console.WriteLine(res.Content.ReadAsStringAsync().Result);
    }

    [Test]
    public async Task TestUnauthenticatedPersonCanNotSubmitPatient()
    {
        string route = $"/patientevaluation/{o1t1p1.Id}/submit-evaluation";
        string token = null;
        HttpMethod method = HttpMethod.Post;

        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method);
        // check status code
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        //  check the content
        string content = await res.Content.ReadAsStringAsync();
        Assert.That(content, Is.EqualTo(UnauthorizedMessage));
    }

    [Test]
    public async Task TestAuthenticatedPersonCanGetEvaluationData()
    {
        string route = $"/patientevaluation/{o1t1p1.Id}/submit-evaluation";
        string token = null;
        HttpMethod method = HttpMethod.Post;

        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method);
        // check status code
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        //  check the content
        string content = await res.Content.ReadAsStringAsync();
        Assert.That(content, Is.EqualTo(UnauthorizedMessage));
    }

    [Test]
    public async Task TestUnatuthentcatedPersonCanNotGetEvaluationData()
    {
        string route = $"/patientevaluation/{o1t1p1.Id}/";
        string token = null;
        HttpMethod method = HttpMethod.Get;

        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method);
        // check status code
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        //  check the content
        string content = await res.Content.ReadAsStringAsync();
        Assert.That(content, Is.EqualTo(UnauthorizedMessage));
    }

    #endregion
    
    #region Session Controller
    
    [Test]
    public async Task TestTherapistCanSubmitSession()
    {
        Assert.That(o1t1.Token, Is.Not.Null);
        string route = $"/session/patient/{o1t1p1.Id}/submit-session/";
        string token = o1t1.Token;
        HttpMethod method = HttpMethod.Post;

        JsonContent content = JsonContent.Create("test");
        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method, content);

        // check status code
        Assert.That(res.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task TestTherapistCanGetSessions()
    {
        Assert.That(o1t1.Token, Is.Not.Null);
        string route = $"/session/patient/{o1t1p1.Id}/session/";
        string token = o1t1.Token;
        HttpMethod method = HttpMethod.Get;

        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method);
        // check status code
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        // Read the content as string
        string content = await res.Content.ReadAsStringAsync();
        //  check the content
        List<Session> sessions = JsonConvert.DeserializeObject<List<Session>>(content);
        Assert.That(sessions, Is.Not.Null);
    }

    [Test]
    public async Task TestOwnerCanGetSessions()
    {
        string route = $"/session/patient/{o1t1p1.Id}/session/";
        string token = owner1.Token;
        HttpMethod method = HttpMethod.Get;

        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method);
        // check status code
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        //  check the content
        Assert.IsNotNull(res.RequestMessage);
    }

    [Test]
    public async Task TestOwnerCanGetPrePostEvaluations()
    {
        string route = $"/session/patient/{o1t1p1.Id}/session/session-id-here/pre-post";
        string token = owner1.Token;
        HttpMethod method = HttpMethod.Get;

        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method);
        // check status code
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        //  check the content
        string content = await res.Content.ReadAsStringAsync();
        Assert.That(content, Is.EqualTo(""));
    }

    [Test]
    public async Task TestTherapistCanGetPrePostEvaluations()
    {
        Assert.That(o1t1.Token, Is.Not.Null);
        string route = $"/session/patient/{o1t1p1.Id}/session/session-id-here/pre-post";
        string token = o1t1.Token;
        HttpMethod method = HttpMethod.Get;

        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method);
        // check status code
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        //  check the content
        string content = await res.Content.ReadAsStringAsync();
        Assert.That(content, Is.EqualTo(""));
    }

    [Test]
    public async Task TestTherapistCanNotGetPrePostEvaluationsForOtherTherapist()
    {
        string route = $"/session/patient/{o1t1p1.Id}/session/session-id-here/pre-post";
        string token = o1t2.Token;
        HttpMethod method = HttpMethod.Get;

        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method);
        // check status code
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        //  check the content
        string content = await res.Content.ReadAsStringAsync();
        Assert.That(content, Is.EqualTo(UnauthorizedMessage));
    }

    [Test]
    public async Task TestTherapistCanNotGetSessionsForOtherTherapist()
    {
        string route = $"/session/patient/{o1t1p1.Id}/session/";
        string token = o1t2.Token;
        HttpMethod method = HttpMethod.Get;

        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method);
        // check status code
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.IsNotNull(res.RequestMessage);

        //  check the content
        string content = await res.Content.ReadAsStringAsync();
        Console.WriteLine(content);
        Assert.That(content, Is.EqualTo(UnauthorizedMessage));
    }
    
    #endregion
    
    #region Log Controller Tests
    
    [Test]
    public async Task TestLogGuestLoginIsAvailableToUnauthenticatedUser()
    {
        string route = $"/log/login-guest/";
        string token = null;
        HttpMethod method = HttpMethod.Post;
        

        JsonContent content = JsonContent.Create("test");
        HttpResponseMessage res = await _helper.MakeAuthenticatedRequest(route, token, method, content);
        
        // check status code
        Assert.That(res.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        string contentString = await res.Content.ReadAsStringAsync();
        Assert.That(contentString, Is.EqualTo("Invalid email"));
    }
    
    #endregion
}