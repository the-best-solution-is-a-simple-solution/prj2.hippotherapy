using System.Net.Http.Json;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using HippoApi.Controllers;
using HippoApi.Models;
using HippoApi.Models.enums;
using Newtonsoft.Json;

namespace tests.Models;

/// <summary>
///     A test helper to make data and put it in the database with a specific id
///     This makes it so that the data does NOT need to be seeded every time, to save
///     our free firebase credits.
/// </summary>
public class TestAuthorizationHelper
{
    #region Global Variables and Setup

    //--------------------Global Variables--------------------//
    private readonly HttpClient _client;
    private readonly FirebaseAuth _firebaseAuth;
    private readonly FirestoreDb _firestoreDb;

    private readonly string _testEmailDomain;

    private readonly string _testPassword;
    private AuthController _authController;
    private string _unauthorizedMessage;


    /// <summary>
    ///     Pass in values to use
    /// </summary>
    /// <param name="authController">auth controller</param>
    /// <param name="auth">firebase auth instance to create users with</param>
    /// <param name="db">firestore database instance</param>
    /// <param name="client">client to make the requests to</param>
    /// <param name="defaultPassword">default password for all users created</param>
    /// <param name="testEmailDomain">default ending domain e.g. @test.com</param>
    /// <param name="defaultUnauthorizedMessage">the expected unauthorized message</param>
    public TestAuthorizationHelper(AuthController authController, FirebaseAuth auth, FirestoreDb db, HttpClient client,
        string defaultPassword, string testEmailDomain, string defaultUnauthorizedMessage)
    {
        _authController = authController;
        _firebaseAuth = auth;
        _firestoreDb = db;
        _client = client;
        _testPassword = defaultPassword;
        _testEmailDomain = testEmailDomain;
        _unauthorizedMessage = defaultUnauthorizedMessage;
    }

    #endregion

    #region Geting Data Objects

    /// <summary>
    ///     Get a test owner setting default password
    /// </summary>
    /// <param name="emailName">
    ///     providing "o1" would make the email
    ///     o1@{defaultDomain}.com
    /// </param>
    public TestUserData GetTestOwner(string ownerId, string emailName, string firstName, string lastName)
    {
        TestUserData user = new()
        {
            OwnerId = ownerId,
            Email = emailName + _testEmailDomain,
            FName = firstName,
            LName = lastName,
            Password = _testPassword
        };

        return user;
    }

    /// <summary>
    ///     Get a test therapist setting their info
    /// </summary>
    public TestUserData GetTestTherapist(string ownerId, string therapistId, string emailName, string firstName,
        string lastName)
    {
        TestUserData user = new()
        {
            OwnerId = ownerId,
            TherapistId = therapistId,
            Email = emailName + _testEmailDomain,
            FName = firstName,
            LName = lastName,
            Password = _testPassword
        };

        return user;
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
    ///     Makes a test evaluation with the default value
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

    #endregion

    #region Creating Data Objects

    /// <summary>
    ///     Creates an owner record in firebase auth, and in firestore db.
    ///     Also adds the owner role
    /// </summary>
    /// <param name="user">Data to use for Owner</param>
    /// <param name="isVerified">Set if the user's email is verified.</param>
    /// <returns>True if it succeeded, otherwise false</returns>
    public async Task CreateOwner(TestUserData user, bool isVerified = true)
    {
        try
        {
            UserRecordArgs record = new();
            record.Uid = user.OwnerId;
            record.Email = user.Email;
            record.Password = _testPassword;
            record.EmailVerified = isVerified;

            // Create user record in firebase auth
            await _firebaseAuth.CreateUserAsync(record);

            // Assign them a role
            Dictionary<string, object> claims = new()
            {
                { "role", AccountRole.Owner.GetDescription() }
            };
            await _firebaseAuth.SetCustomUserClaimsAsync(user.OwnerId, claims);

            Owner ownerDbRecord = new()
            {
                Email = user.Email,
                FName = user.FName,
                LName = user.LName,
                OwnerId = user.OwnerId
            };

            // create the object in firestore db
            await _firestoreDb.Collection(OwnerController.COLLECTION_NAME)
                .Document(user.OwnerId).SetAsync(ownerDbRecord);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error adding owner: {user.Email}");
            Console.WriteLine(e);
        }
    }

    /// <summary>
    ///     Creates an owner record in firebase auth, and in firestore db.
    ///     Also adds the therapist role
    /// </summary>
    public async Task CreateTherapist(TestUserData user, bool isVerified = true)
    {
        try
        {
            UserRecordArgs record = new();
            record.Uid = user.TherapistId;
            record.Email = user.Email;
            record.Password = _testPassword;
            record.EmailVerified = isVerified;

            // Create user record in firebase auth
            await _firebaseAuth.CreateUserAsync(record);

            // Assign them a role
            Dictionary<string, object> claims = new()
            {
                { "role", AccountRole.Therapist.GetDescription() }
            };
            await _firebaseAuth.SetCustomUserClaimsAsync(user.TherapistId, claims);

            Therapist therapistDbRecord = new()
            {
                TherapistID = user.TherapistId,
                Email = user.Email,
                FName = user.FName,
                LName = user.LName
            };

            // create the object in firestore db
            await _firestoreDb.Collection(OwnerController.COLLECTION_NAME)
                .Document(user.OwnerId)
                .Collection(TherapistController.COLLECTION_NAME)
                .Document(user.TherapistId)
                .SetAsync(therapistDbRecord);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error adding therapist: {user.Email}");
            Console.WriteLine(e);
        }
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
    ///     Logs in the provided user assigning them a valid token to use for requests
    /// </summary>
    /// <param name="user">User to login</param>
    public async Task AddTokenToOwner(TestUserData user)
    {
        var loginReq = new
        {
            email = user.Email,
            password = user.Password
        };
        JsonContent content = JsonContent.Create(loginReq);
        HttpResponseMessage? res = await _client.PostAsync("/auth/owner/login", content);
        LoginResponse? loginResponse =
            JsonConvert.DeserializeObject<LoginResponse>(await res.Content.ReadAsStringAsync());
        user.Token = loginResponse.Token;
    }

    /// <summary>
    ///     Logs in the provided user assigning them a valid token to use for requests
    /// </summary>
    /// <param name="user">User to login</param>
    public async Task AddTokenToTherapist(TestUserData user)
    {
        var loginReq = new
        {
            email = user.Email,
            password = user.Password
        };
        JsonContent content = JsonContent.Create(loginReq);
        HttpResponseMessage? res = await _client.PostAsync("/auth/therapist/login", content);
        LoginResponse? loginResponse =
            JsonConvert.DeserializeObject<LoginResponse>(await res.Content.ReadAsStringAsync());
        user.Token = loginResponse.Token;
    }

    #endregion

    #region MakeAuthenticatedRequest and Helper Class

    /// <summary>
    ///     A helper method for making a request with content
    /// </summary>
    /// <param name="content">optional if making a GET or DELETE</param>
    /// <returns>Result of request</returns>
    public async Task<HttpResponseMessage> MakeAuthenticatedRequest(string route, string token, HttpMethod method,
        JsonContent content = null)
    {
        _client.DefaultRequestHeaders.Clear();
        if (token != null) _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        HttpResponseMessage response;

        if (method == HttpMethod.Get)
            response = await _client.GetAsync(route);
        else if (method == HttpMethod.Put)
            response = await _client.PutAsync(route, content);
        else if (method == HttpMethod.Post)
            response = await _client.PostAsync(route, content);
        else if (method == HttpMethod.Delete)
            response = await _client.DeleteAsync(route);
        else
            throw new ArgumentException($"Unsupported HTTP method: {method}");

        return response;
    }

    //--------------------Private Classes--------------------//

    /// Class to map responses from login requests to values
    public class LoginResponse
    {
        public string Message { get; set; }
        public string Token { get; set; }
        public string UserId { get; set; }
    }

    #endregion
}