using FirebaseAdmin;
using Google.Api.Gax;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace HippoApi;

public class StartupEmulator
{
    private readonly IConfiguration configuration;

    /// <summary>
    ///     The key for the section name to lookup in the configuration
    ///     (default config file is appsettings.json)
    /// </summary>
    private readonly string firebaseSectionName = "Firebase";

    public StartupEmulator(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    /// <summary>
    ///     Setup the services to use for production app
    ///     Example: Disable JWT tokens, load values from config file, etc.
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        // TODO: could switch to using this in the future and adding it as an
        // arg to controllers that need it (authController)
        services.Configure<FirebaseConfig>(configuration.GetSection(firebaseSectionName));

        // For now load the values here in to env variables
        FirebaseConfig? firebaseConfig = configuration.GetSection(firebaseSectionName).Get<FirebaseConfig>();
        Console.WriteLine("--------------------");
        Console.WriteLine($"FIRESTORE_EMULATOR_HOST: {firebaseConfig.FirestoreEmulatorHost}");
        Environment.SetEnvironmentVariable("FIREBASE_PROJECT_ID", firebaseConfig.ProjectId);
        Console.WriteLine($"FIREBASE_PROJECT_ID: {firebaseConfig.ProjectId}");

        Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", firebaseConfig.FirestoreEmulatorHost);
        Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", firebaseConfig.FirebaseAuthEmulatorHost);

        Environment.SetEnvironmentVariable("FIREBASE_AUTH_URL_START", firebaseConfig.FirebaseAuthUrlStart);
        Environment.SetEnvironmentVariable("FIREBASE_AUTH_URL_END", firebaseConfig.FirebaseAuthUrlEnd);
        Environment.SetEnvironmentVariable("FIREBASE_PROJECT_ID", firebaseConfig.ProjectId);

        // Not sure if better way but for now emulator complains     
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", firebaseConfig.CredentialFilePath);

        if (FirebaseApp.DefaultInstance == null)
        {
            Console.WriteLine("FirebaseApp.DefaultInstance is null, initializing...");
            // Initialize FirebaseApp only if it hasn't been initialized
            FirebaseApp.Create(new AppOptions
            {
                ProjectId = firebaseConfig.ProjectId,
                // use fake credentials for the emulator
                Credential = GoogleCredential.FromAccessToken("fake-credential-access-token")
            });
        }

        FirestoreDb? _firestore = new FirestoreDbBuilder
        {
            ProjectId = firebaseConfig.ProjectId,
            EmulatorDetection = EmulatorDetection.EmulatorOnly
        }.Build();
        services.AddSingleton(_firestore);

        services.AddRouting();
        services.AddControllers(options =>
        {
            options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
        });

        // Allow any for development
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(
                policy =>
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });

        Console.WriteLine("Backend ready for use! ----------");
    }

    /// <summary>
    ///     Set up the app to use emulator settings (NO authorization and authentication)
    /// </summary>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();
        app.UseCors();

        // DO NOT add authorization to allow current frontend integration tests to work
        // app.UseAuthorization();
        // app.UseAuthentication();

        app.UseEndpoints(opt => { opt.MapControllers(); });
    }
}