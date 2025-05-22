using FirebaseAdmin;
using Google.Api.Gax;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using HippoApi.middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.IdentityModel.Tokens;

namespace HippoApi;

public class StartupProduction
{
    /// <summary>
    ///     The key for the section name to lookup in the configuration
    ///     (default config file is ..../hippo-api/appsettings.json)
    /// </summary>
    private readonly string _firebaseSectionName = "Firebase";

    private readonly int _tokenLifetimeMinutes = 5;
    private readonly IConfiguration configuration;

    public StartupProduction(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    /// <summary>
    ///     Setup the services to use for production app
    ///     Example: JWT tokens, load values from config file, etc.
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        // load all values in the section into a class to reduce spelling mistakes
        services.Configure<FirebaseConfig>(configuration.GetSection(_firebaseSectionName));

        // For now load the values here in to env variables
        FirebaseConfig? firebaseConfig = configuration.GetSection(_firebaseSectionName).Get<FirebaseConfig>();
        Console.WriteLine("---------------------------------------");
        Console.WriteLine($"ProjectId: {firebaseConfig.ProjectId}");

        // DO NOT SET THESE!!!
        // or it will try to talk to the emulator
        // Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", firebaseConfig.FirestoreEmulatorHost);
        // Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", firebaseConfig.FirebaseAuthEmulatorHost);

        Environment.SetEnvironmentVariable("FIREBASE_AUTH_URL_START", firebaseConfig.FirebaseAuthUrlStart);
        Environment.SetEnvironmentVariable("FIREBASE_AUTH_URL_END", $"?key={firebaseConfig.ApiKey}");
        Environment.SetEnvironmentVariable("FIREBASE_PROJECT_ID", firebaseConfig.ProjectId);

        // Set credential path to use
        // points to private key for app (default is stored in .../hippo-api/config/firebase-admin.json)
        string credentialPath = Path.Combine(Directory.GetCurrentDirectory(), firebaseConfig.CredentialFilePath);
        if (!File.Exists(credentialPath)) throw new Exception($"Credential file not found at {credentialPath}");
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);
        Console.WriteLine($"GOOGLE_APPLICATION_CREDENTIALS set to: {credentialPath}");

        GoogleCredential? credential = GoogleCredential.FromFile(credentialPath);


        // Initialize FirebaseApp only if it hasn't been initialized
        if (FirebaseApp.DefaultInstance == null)
        {
            FirebaseApp.Create(new AppOptions
            {
                ProjectId = firebaseConfig.ProjectId,
                Credential = credential
            });

            Console.WriteLine("Success: \tFirebaseApp initialized successfully.");
        }
        else
        {
            Console.WriteLine("Error: \tFirebaseApp is already initialized.");
        }

        // Make the database instance with 
        FirestoreDb firestore = new FirestoreDbBuilder
        {
            ProjectId = firebaseConfig.ProjectId,
            Credential = GoogleCredential.GetApplicationDefault(),
            EmulatorDetection = EmulatorDetection.None
        }.Build();
        services.AddSingleton(firestore);

        services.AddRouting();

        services.AddControllers(options =>
        {
            // Set default to return stuff in JSON
            options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());

            // AVOID setting authorization to all controllers here since
            // the priorities make it happen BEFORE getting the token
            // (might be possible if you look into setting priorities for ContentOwnerAuthorization)
            // options.Filters.Add<ContentOwnerAuthorization>();
            // options.Filters.Add<ContentOwnerAuthorization>(int.MinValue);
        });


        // Authenticate the Jwt Token
        // from https://blog.markvincze.com/secure-an-asp-net-core-api-with-firebase/

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"https://securetoken.google.com/{firebaseConfig.ProjectId}";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = $"https://securetoken.google.com/{firebaseConfig.ProjectId}",
                    ValidateAudience = true,
                    ValidAudience = firebaseConfig.ProjectId,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(_tokenLifetimeMinutes) // Adjust token lifetime 
                };

                options.Events = new JwtBearerEvents
                {
                    // Set the custom unauthorized response for if the user has no token
                    OnChallenge = context =>
                    {
                        context.Response.Headers["WWW-Authenticate"] =
                            "Bearer realm=\"Access to the site\", error=\"invalid_token\", error_description=\"Unauthorized\"";
                        context.HandleResponse(); // Prevents the default response

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return context.Response.WriteAsync(ContentOwnerAuthorization.DefaultUnauthorizedMessage);
                    }
                };
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
    }

    /// <summary>
    ///     Configure the app to use authentication, routing, authorization etc.
    /// </summary>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();
        app.UseCors();

        // Put authentication BEFORE authorization
        app.UseAuthentication();
        app.UseExceptionHandler("/error");

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            // Require authorization which checks the token against Google's credentials
            endpoints.MapControllers().RequireAuthorization();
        });
    }
}