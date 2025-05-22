using Google.Api.Gax;
using Google.Cloud.Firestore;
using HippoApi;
using HippoApi.controllers;
using HippoApi.Controllers;
using HippoApi.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace tests;

[TestFixture]
public class LogControllerTests : IDisposable
{
    private readonly LogController _logController;
    private readonly FirestoreDb _firestoreDb;
    private readonly IntegrationTestDataController _integrationTestDataController;
    private readonly TestServer _testServer;

    
    public LogControllerTests()
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
        
        // instantiate controller and collection
        _integrationTestDataController = new IntegrationTestDataController(_firestoreDb);
        _logController = new LogController(_firestoreDb);
    }
    
    public void Dispose()
    {
        _testServer.Dispose();
    }


    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        await _integrationTestDataController.ClearFirestoreEmulatorDataAsync();
    }

    [SetUp]
    public async Task SetUpAsync()
    {
        // Clear the logs collection between tests
        CollectionReference logsRef = _firestoreDb.Collection(LogController.COLLECTION_NAME);
        QuerySnapshot? snapshot = await logsRef.GetSnapshotAsync();
        foreach (DocumentSnapshot? doc in snapshot.Documents) await doc.Reference.DeleteAsync();
    }

    // INFO - Also see test that unauthenticated access is allowed in backendIntegration tests
    
    [Test]
    public async Task TestLogGuestLoginSavesEmailAndTimestamp()
    {
        string email = "guest@test.com";
        
        var res = await _logController.LogGuestLogin(email) as ObjectResult;
        Assert.That(res, Is.Not.Null);
        Assert.That(res.StatusCode, Is.EqualTo(200));
        
        // check data exists in firestore collection
        CollectionReference logsRef = _firestoreDb.Collection(LogController.COLLECTION_NAME);
        Query query = logsRef.WhereEqualTo("Email", email);
        QuerySnapshot querySnapshot = await query.GetSnapshotAsync();
        Assert.IsNotNull(querySnapshot);
        Assert.That(querySnapshot.Count, Is.EqualTo(1));
        
        // Get entry, check values
        foreach (DocumentSnapshot documentSnapshot in querySnapshot.Documents)
        {
            UserLoginRecord record = documentSnapshot.ConvertTo<UserLoginRecord>();
            Assert.That(record.Email, Is.EqualTo(email));
            Assert.That(record.DateTaken.DayOfYear, Is.EqualTo(DateTime.Now.DayOfYear));
        }
    }
    
    [Test]
    public async Task TestLogGuestLoginReturnsBadRequestWithInvalidEmail()
    {
        string email = "invalid-email";
        
        var res = await _logController.LogGuestLogin(email) as ObjectResult;
        Assert.That(res, Is.Not.Null);
        Assert.IsInstanceOf<BadRequestObjectResult>(res);
        
        // check data does not exist in firestore collection
        CollectionReference logsRef = _firestoreDb.Collection(LogController.COLLECTION_NAME);
        Query query = logsRef.WhereEqualTo("Email", email);
        QuerySnapshot querySnapshot = await query.GetSnapshotAsync();
        Assert.IsNotNull(querySnapshot);
        Assert.That(querySnapshot.Count, Is.EqualTo(0));
    }
    
    
    
    
    
}