using FirebaseAdmin;
using Google.Cloud.Firestore;
using HippoApi.Controllers;
using HippoApi.Models;

namespace HippoApi;

/// <summary>
///     Launches the listener for the API and sets up the FirestoreDb connection via emulator.
///     Also seeds the db with data.
/// </summary>
public class Program
{
    private static IntegrationTestDataController testDataController;
    private static string firebaseProjectId;
    private static FirestoreDb _firestore;


    public static void Main(string[] args)
    {
        if (args.Contains("--production") || args.Contains("-p"))
            CreateHostBuilderProduction(args).Build().Run();
        else if (args.Contains("--integration-backend") || args.Contains("-i"))
            CreateHostBuilderBackendIntegration(args).Build().Run();
        else if (args.Contains("--seeded-emulator") || args.Contains("-s"))
        {
            CreateHostBuilderSeededEmulator(args).Build().Run();
        }
        else
            CreateHostBuilderEmulator(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilderEmulator(string[] args)
    {
        Console.WriteLine("\n\n==========Emulator App Starting==========");

        // Check that the emulator is running


        return new HostBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.AddJsonFile("appsettings.Emulator.json").AddEnvironmentVariables();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<StartupEmulator>().UseUrls("http://*:5001/");
            });
    }

    public static IHostBuilder CreateHostBuilderProduction(string[] args)
    {
        Console.WriteLine("\n\n==========Production App Starting==========");

        return new HostBuilder()
            .ConfigureAppConfiguration(config =>
            {
                // TODO: change json file used for different production firebase app
                config.AddJsonFile("appsettings.production.json").AddEnvironmentVariables();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<StartupProduction>().UseUrls("http://*:5001/");
            });
    }

    public static IHostBuilder CreateHostBuilderBackendIntegration(string[] args)
    {
        Console.WriteLine("\n\n==========Integration Tests for Backend App Starting==========");
        Console.WriteLine("WARNING");
        Console.WriteLine("WARNING");
        Console.WriteLine("WARNING");
        Console.WriteLine("WARNING");
        Console.WriteLine("Warning: will use an actual connection to firebase. ONLY run backend integration tests.");
        return new HostBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.AddJsonFile("appsettings.IntegrationTests.json").AddEnvironmentVariables();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<StartupProduction>().UseUrls("http://*:5001/");
            });
    }
    
    public static IHostBuilder CreateHostBuilderSeededEmulator(string[] args)
    {
        Console.WriteLine("\n\n==========Seeded Emulator App Starting==========");

        return new HostBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.AddJsonFile("appsettings.SeededEmulator.json").AddEnvironmentVariables();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<StartupSeededEmulator>().UseUrls("http://*:5001/");
            });
    }

    private static async Task TestFirestoreConnection(FirestoreDb db)
    {
        try
        {
            // Try to fetch a simple document or collection
            CollectionReference collection = db.Collection("test-connectivity-collection");
            collection.AddAsync("test-emulator-is-on");
            Console.WriteLine("Successfully wrote test document connected to Firestore.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Check that the emulator is running");
            Console.WriteLine($"Failed to connect to Firestore: {ex.Message}");
            // You might want to log more details for debugging
            Console.WriteLine($"Details: {ex}");
        }
    }


    /// <summary>
    ///     USE THIS METHOD TO SEED ALL YOUR DATA THAT YOUR STORY NEEDS IN THE APPLICATION
    /// </summary>
    public static async Task SeedData(FirestoreDb firestoreDb)
    {
        testDataController = new IntegrationTestDataController(firestoreDb);
        // Clear the current database
        await testDataController.ClearFirestoreEmulatorDataAsync();
        await testDataController.ClearMailEmulatorDataAsync();
        await testDataController.ClearFirebaseAuthenticationEmulatorDataAsync();
        
        testDataController = new IntegrationTestDataController(firestoreDb);
        CollectionReference pCollection = firestoreDb.Collection(PatientController.COLLECTION_NAME);
        foreach (PatientPrivate p in SeedPatientData()) await pCollection.AddAsync(p);

        // Can get stuff to view in app BUT DO NOT CHANGE or integration tests will break
        await testDataController.SeedPatientInfoPageSessionTabTestData();
        //await testDataController.SeedPatientExportPageTestData();
        await IntegrationTestDataController.AddTherapist(SeedTherapistData(), null);
        await testDataController.SeedOwnerTherapistInfo();
    }


    /// <summary>
    ///     This method will create 50 randomly generated patients
    ///     and return them in a list
    /// </summary>
    /// <returns>50 random patients</returns>
    private static List<PatientPrivate> SeedPatientData()
    {
        return new List<PatientPrivate>
        {
            new()
            {
                FName = "Aron",
                LName = "Szabo",
                Phone = "555-012-3456",
                Age = 21,
                Weight = 150,
                Height = 190,
                Email = "szabo5144@saskpolytech.ca",
                DoctorPhoneNumber = "555-123-4567",
                GuardianPhoneNumber = "555-555-6666", // Guardian not needed, but still added
                Condition = "Injured leg",
                TherapistID = "default"
            },
            new()
            {
                FName = "Ella",
                LName = "Brown",
                Phone = "555-012-3456",
                Age = 11,
                Weight = 40.5,
                Height = 145,
                Email = "ella.brown@example.com",
                DoctorPhoneNumber = "555-123-4567",
                GuardianPhoneNumber = "555-555-6666", // Guardian needed because age is under 18
                Condition = "Cold",
                TherapistID = "default"
            },
            new()
            {
                FName = "Felix",
                LName = "Davis",
                Phone = "555-123-4567",
                Age = 31,
                Weight = 78.0,
                Height = 182,
                Email = "felix.davis@example.com",
                DoctorPhoneNumber = "555-234-5678",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Arthritis",
                TherapistID = "default"
            },
            new()
            {
                FName = "Freya",
                LName = "Evans",
                Phone = "555-234-5678",
                Age = 28,
                Weight = 65.4,
                Height = 170,
                Email = "freya.evans@example.com",
                DoctorPhoneNumber = "555-345-6789",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Chronic Pain",
                TherapistID = "default"
            },
            new()
            {
                FName = "George",
                LName = "Gonzalez",
                Phone = "555-345-6789",
                Age = 50,
                Weight = 85.2,
                Height = 185,
                Email = "george.gonzalez@example.com",
                DoctorPhoneNumber = "555-456-7890",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Heart Disease",
                TherapistID = "default"
            },
            new()
            {
                FName = "Grace",
                LName = "Hughes",
                Phone = "555-456-7890",
                Age = 17,
                Weight = 55.6,
                Height = 160,
                Email = "grace.hughes@example.com",
                DoctorPhoneNumber = "555-567-8901",
                GuardianPhoneNumber = "555-666-7777", // Guardian needed because age is under 18
                Condition = "Diabetes",
                TherapistID = "default"
            },
            new()
            {
                FName = "Henry",
                LName = "Hernandez",
                Phone = "555-567-8901",
                Age = 29,
                Weight = 74.3,
                Height = 177,
                Email = "henry.hernandez@example.com",
                DoctorPhoneNumber = "555-678-9012",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Hypertension",
                TherapistID = "default"
            },
            new()
            {
                FName = "Hannah",
                LName = "Jackson",
                Phone = "555-678-9012",
                Age = 21,
                Weight = 60.0,
                Height = 165,
                Email = "hannah.jackson@example.com",
                DoctorPhoneNumber = "555-789-0123",
                GuardianPhoneNumber = "555-777-8888", // Guardian needed because age is under 18
                Condition = "Cold",
                TherapistID = "default"
            },
            new()
            {
                FName = "Isaiah",
                LName = "King",
                Phone = "555-789-0123",
                Age = 24,
                Weight = 71.5,
                Height = 180,
                Email = "isaiah.king@example.com",
                DoctorPhoneNumber = "555-890-1234",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Migraine",
                TherapistID = "default"
            },
            new()
            {
                FName = "Ivy",
                LName = "Lewis",
                Phone = "555-890-1234",
                Age = 19,
                Weight = 50.3,
                Height = 155,
                Email = "ivy.lewis@example.com",
                DoctorPhoneNumber = "555-901-2345",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Healthy",
                TherapistID = "default"
            },
            new()
            {
                FName = "Jack",
                LName = "Moore",
                Phone = "555-901-2345",
                Age = 15,
                Weight = 56.7,
                Height = 160,
                Email = "jack.moore@example.com",
                DoctorPhoneNumber = "555-012-3456",
                GuardianPhoneNumber = "555-888-9999", // Guardian needed because age is under 18
                Condition = "Asthma",
                TherapistID = "default"
            },
            new()
            {
                FName = "Jasmine",
                LName = "Martinez",
                Phone = "555-012-3456",
                Age = 33,
                Weight = 77.0,
                Height = 170,
                Email = "jasmine.martinez@example.com",
                DoctorPhoneNumber = "555-123-4567",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Anxiety",
                TherapistID = "default"
            },
            new()
            {
                FName = "Jackson",
                LName = "Nelson",
                Phone = "555-123-4567",
                Age = 20,
                Weight = 72.2,
                Height = 178,
                Email = "jackson.nelson@example.com",
                DoctorPhoneNumber = "555-234-5678",
                GuardianPhoneNumber = "555-999-0000", // Guardian needed because age is under 18
                Condition = "Back Pain",
                TherapistID = "default"
            },
            new()
            {
                FName = "Julian",
                LName = "O'Connor",
                Phone = "555-234-5678",
                Age = 60,
                Weight = 88.9,
                Height = 180,
                Email = "julian.oconnor@example.com",
                DoctorPhoneNumber = "555-345-6789",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Obesity",
                TherapistID = "default"
            },
            new()
            {
                FName = "Liam",
                LName = "Johnson",
                Phone = "555-111-2222",
                Age = 17,
                Weight = 55.4,
                Height = 165,
                Email = "liam.johnson@example.com",
                DoctorPhoneNumber = "555-222-3333",
                GuardianPhoneNumber = "555-444-5555", // Guardian needed because age is under 18
                Condition = "Allergies",
                TherapistID = "default"
            },
            new()
            {
                FName = "Sophia",
                LName = "Williams",
                Phone = "555-222-3333",
                Age = 32,
                Weight = 62.0,
                Height = 160,
                Email = "sophia.williams@example.com",
                DoctorPhoneNumber = "555-333-4444",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Thyroid Disorder",
                TherapistID = "default"
            },
            new()
            {
                FName = "Mason",
                LName = "Brown",
                Phone = "555-333-4444",
                Age = 15,
                Weight = 50.5,
                Height = 157,
                Email = "mason.brown@example.com",
                DoctorPhoneNumber = "555-444-5555",
                GuardianPhoneNumber = "555-555-6666", // Guardian needed because age is under 18
                Condition = "Bronchitis",
                TherapistID = "default"
            },
            new()
            {
                FName = "Olivia",
                LName = "Jones",
                Phone = "555-444-5555",
                Age = 24,
                Weight = 67.8,
                Height = 162,
                Email = "olivia.jones@example.com",
                DoctorPhoneNumber = "555-555-6666",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Endometriosis",
                TherapistID = "default"
            },
            new()
            {
                FName = "Noah",
                LName = "Garcia",
                Phone = "555-555-6666",
                Age = 11,
                Weight = 40.8,
                Height = 145,
                Email = "noah.garcia@example.com",
                DoctorPhoneNumber = "555-666-7777",
                GuardianPhoneNumber = "555-777-8888", // Guardian needed because age is under 18
                Condition = "Ear Infection",
                TherapistID = "default"
            },
            new()
            {
                FName = "Isabella",
                LName = "Martinez",
                Phone = "555-666-7777",
                Age = 20,
                Weight = 59.6,
                Height = 168,
                Email = "isabella.martinez@example.com",
                DoctorPhoneNumber = "555-777-8888",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Anemia",
                TherapistID = "default"
            },
            new()
            {
                FName = "Ethan",
                LName = "Hernandez",
                Phone = "555-777-8888",
                Age = 16,
                Weight = 58.2,
                Height = 170,
                Email = "ethan.hernandez@example.com",
                DoctorPhoneNumber = "555-888-9999",
                GuardianPhoneNumber = "555-999-0000", // Guardian needed because age is under 18
                Condition = "Asthma",
                TherapistID = "default"
            },
            new()
            {
                FName = "Charlotte",
                LName = "Lee",
                Phone = "555-888-9999",
                Age = 29,
                Weight = 63.4,
                Height = 164,
                Email = "charlotte.lee@example.com",
                DoctorPhoneNumber = "555-999-0000",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Fibromyalgia",
                TherapistID = "default"
            },
            new()
            {
                FName = "James",
                LName = "Perez",
                Phone = "555-999-0000",
                Age = 18,
                Weight = 75.1,
                Height = 178,
                Email = "james.perez@example.com",
                DoctorPhoneNumber = "555-111-2222",
                GuardianPhoneNumber = "555-222-3333", // Guardian required since age is 18
                Condition = "Severe Anxiety",
                TherapistID = "default"
            },
            new()
            {
                FName = "Amelia",
                LName = "Wilson",
                Phone = "555-111-2222",
                Age = 36,
                Weight = 65.5,
                Height = 167,
                Email = "amelia.wilson@example.com",
                DoctorPhoneNumber = "555-222-3333",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Post-surgery recovery",
                TherapistID = "default"
            },
            new()
            {
                FName = "Benjamin",
                LName = "Davis",
                Phone = "555-222-3333",
                Age = 14,
                Weight = 45.0,
                Height = 155,
                Email = "benjamin.davis@example.com",
                DoctorPhoneNumber = "555-333-4444",
                GuardianPhoneNumber = "555-444-5555", // Guardian needed because age is under 18
                Condition = "Cold",
                TherapistID = "default"
            },
            new()
            {
                FName = "Mia",
                LName = "Rodriguez",
                Phone = "555-333-4444",
                Age = 44,
                Weight = 80.3,
                Height = 170,
                Email = "mia.rodriguez@example.com",
                DoctorPhoneNumber = "555-444-5555",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Hypertension",
                TherapistID = "default"
            },
            new()
            {
                FName = "Oliver",
                LName = "Smith",
                Phone = "555-444-5555",
                Age = 19,
                Weight = 68.0,
                Height = 175,
                Email = "oliver.smith@example.com",
                DoctorPhoneNumber = "555-555-6666",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Migraine",
                TherapistID = "default"
            },
            new()
            {
                FName = "Ava",
                LName = "Johnson",
                Phone = "555-555-6666",
                Age = 10,
                Weight = 37.5,
                Height = 140,
                Email = "ava.johnson@example.com",
                DoctorPhoneNumber = "555-666-7777",
                GuardianPhoneNumber = "555-777-8888", // Guardian needed because age is under 18
                Condition = "Fever",
                TherapistID = "default"
            },
            new()
            {
                FName = "Lucas",
                LName = "Garcia",
                Phone = "555-666-7777",
                Age = 22,
                Weight = 78.5,
                Height = 183,
                Email = "lucas.garcia@example.com",
                DoctorPhoneNumber = "555-777-8888",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Back Pain",
                TherapistID = "default"
            },
            new()
            {
                FName = "Hannah",
                LName = "Martinez",
                Phone = "555-777-8888",
                Age = 17,
                Weight = 58.9,
                Height = 162,
                Email = "hannah.martinez@example.com",
                DoctorPhoneNumber = "555-888-9999",
                GuardianPhoneNumber = "555-999-0000", // Guardian needed because age is under 18
                Condition = "Asthma",
                TherapistID = "default"
            },
            new()
            {
                FName = "Grace",
                LName = "Lee",
                Phone = "555-888-9999",
                Age = 25,
                Weight = 62.1,
                Height = 160,
                Email = "grace.lee@example.com",
                DoctorPhoneNumber = "555-999-0000",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Healthy",
                TherapistID = "default"
            },
            new()
            {
                FName = "Sebastian",
                LName = "Martínez",
                Phone = "555-123-4567",
                Age = 16,
                Weight = 53.0,
                Height = 168,
                Email = "sebastian.martinez@example.com",
                DoctorPhoneNumber = "555-234-5678",
                GuardianPhoneNumber = "555-345-6789", // Guardian needed because age is under 18
                Condition = "Bronchitis",
                TherapistID = "default"
            },
            new()
            {
                FName = "Valentina",
                LName = "González",
                Phone = "555-234-5678",
                Age = 27,
                Weight = 58.5,
                Height = 162,
                Email = "valentina.gonzalez@example.com",
                DoctorPhoneNumber = "555-345-6789",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Endometriosis",
                TherapistID = "default"
            },
            new()
            {
                FName = "Lucas",
                LName = "Silva",
                Phone = "555-345-6789",
                Age = 15,
                Weight = 49.8,
                Height = 163,
                Email = "lucas.silva@example.com",
                DoctorPhoneNumber = "555-456-7890",
                GuardianPhoneNumber = "555-567-8901", // Guardian needed because age is under 18
                Condition = "Asthma",
                TherapistID = "default"
            },
            new()
            {
                FName = "Isabela",
                LName = "Rodríguez",
                Phone = "555-456-7890",
                Age = 23,
                Weight = 64.3,
                Height = 170,
                Email = "isabela.rodriguez@example.com",
                DoctorPhoneNumber = "555-567-8901",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Migraines",
                TherapistID = "default"
            },
            new()
            {
                FName = "Matías",
                LName = "Hernández",
                Phone = "555-567-8901",
                Age = 11,
                Weight = 42.0,
                Height = 147,
                Email = "matias.hernandez@example.com",
                DoctorPhoneNumber = "555-678-9012",
                GuardianPhoneNumber = "555-789-0123", // Guardian needed because age is under 18
                Condition = "Cough",
                TherapistID = "default"
            },
            new()
            {
                FName = "Emilia",
                LName = "Pérez",
                Phone = "555-678-9012",
                Age = 30,
                Weight = 70.5,
                Height = 175,
                Email = "emilia.perez@example.com",
                DoctorPhoneNumber = "555-789-0123",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Back Pain",
                TherapistID = "default"
            },
            new()
            {
                FName = "Felipe",
                LName = "López",
                Phone = "555-789-0123",
                Age = 16,
                Weight = 56.0,
                Height = 171,
                Email = "felipe.lopez@example.com",
                DoctorPhoneNumber = "555-890-1234",
                GuardianPhoneNumber = "555-901-2345", // Guardian needed because age is under 18
                Condition = "Flu",
                TherapistID = "default"
            },
            new()
            {
                FName = "Camila",
                LName = "Fernández",
                Phone = "555-890-1234",
                Age = 19,
                Weight = 60.1,
                Height = 160,
                Email = "camila.fernandez@example.com",
                DoctorPhoneNumber = "555-901-2345",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Healthy",
                TherapistID = "default"
            },
            new()
            {
                FName = "Juan",
                LName = "Ramírez",
                Phone = "555-901-2345",
                Age = 50,
                Weight = 85.5,
                Height = 180,
                Email = "juan.ramirez@example.com",
                DoctorPhoneNumber = "555-012-3456",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Heart Disease",
                TherapistID = "default"
            },
            new()
            {
                FName = "Carla",
                LName = "Sánchez",
                Phone = "555-012-3456",
                Age = 37,
                Weight = 63.0,
                Height = 165,
                Email = "carla.sanchez@example.com",
                DoctorPhoneNumber = "555-123-4567",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Chronic Pain",
                TherapistID = "default"
            },
            new()
            {
                FName = "Mateo",
                LName = "Torres",
                Phone = "555-123-4567",
                Age = 14,
                Weight = 50.7,
                Height = 150,
                Email = "mateo.torres@example.com",
                DoctorPhoneNumber = "555-234-5678",
                GuardianPhoneNumber = "555-345-6789", // Guardian needed because age is under 18
                Condition = "Cold",
                TherapistID = "default"
            },
            new()
            {
                FName = "Diana",
                LName = "Cordero",
                Phone = "555-234-5678",
                Age = 42,
                Weight = 72.8,
                Height = 160,
                Email = "diana.cordero@example.com",
                DoctorPhoneNumber = "555-345-6789",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Hypertension",
                TherapistID = "default"
            },
            new()
            {
                FName = "Juliana",
                LName = "Mendoza",
                Phone = "555-345-6789",
                Age = 21,
                Weight = 61.0,
                Height = 167,
                Email = "juliana.mendoza@example.com",
                DoctorPhoneNumber = "555-456-7890",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Asthma",
                TherapistID = "default"
            },
            new()
            {
                FName = "Gabriel",
                LName = "Álvarez",
                Phone = "555-456-7890",
                Age = 29,
                Weight = 77.5,
                Height = 174,
                Email = "gabriel.alvarez@example.com",
                DoctorPhoneNumber = "555-567-8901",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Diabetes",
                TherapistID = "default"
            },
            new()
            {
                FName = "Paula",
                LName = "Díaz",
                Phone = "555-567-8901",
                Age = 19,
                Weight = 55.8,
                Height = 158,
                Email = "paula.diaz@example.com",
                DoctorPhoneNumber = "555-678-9012",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Healthy",
                TherapistID = "default"
            },
            new()
            {
                FName = "Juan Pablo",
                LName = "Castro",
                Phone = "555-678-9012",
                Age = 18,
                Weight = 70.0,
                Height = 180,
                Email = "juanpablo.castro@example.com",
                DoctorPhoneNumber = "555-789-0123",
                GuardianPhoneNumber = "555-890-1234", // Guardian needed because age is 18
                Condition = "Back Pain",
                TherapistID = "default"
            },
            new()
            {
                FName = "Renata",
                LName = "Gutiérrez",
                Phone = "555-789-0123",
                Age = 32,
                Weight = 65.2,
                Height = 165,
                Email = "renata.gutierrez@example.com",
                DoctorPhoneNumber = "555-890-1234",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Stress",
                TherapistID = "default"
            },
            new()
            {
                FName = "Diego",
                LName = "Vargas",
                Phone = "555-890-1234",
                Age = 15,
                Weight = 59.3,
                Height = 172,
                Email = "diego.vargas@example.com",
                DoctorPhoneNumber = "555-901-2345",
                GuardianPhoneNumber = "555-012-3456", // Guardian needed because age is under 18
                Condition = "Asthma",
                TherapistID = "default"
            },
            new()
            {
                FName = "Fernanda",
                LName = "Serrano",
                Phone = "555-901-2345",
                Age = 28,
                Weight = 62.5,
                Height = 169,
                Email = "fernanda.serrano@example.com",
                DoctorPhoneNumber = "555-012-3456",
                GuardianPhoneNumber = null, // No guardian needed since age is above 18
                Condition = "Migraine",
                TherapistID = "default"
            }
        };
    }

    private static Therapist SeedTherapistData()
    {
        return new Therapist
        {
            TherapistID = "test-therapist",
            FName = "First",
            LName = "Last",
            Email = "existing@example.com",
            Phone = "123-456-7890",
            Country = "Canada",
            City = "Saskatoon",
            Profession = "Hippotherapist",
            Major = "Physiotherapy",
            YearsExperienceInHippotherapy = 5
        };
    }
}