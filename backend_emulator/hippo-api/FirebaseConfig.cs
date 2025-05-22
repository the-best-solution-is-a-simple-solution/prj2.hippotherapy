namespace HippoApi;

/// <summary>
///     For loading in different values from appsettings.json
///     Based on the names in the file under the section name e.g. "firebase"
///     In startup.cs call
///     services.Configure(angle brackets FirbaseConfig)(configuration.GetSection("Firebase"));
/// </summary>
public class FirebaseConfig
{
    // MUST match names in the "appsettings.json" (or what ever config file is used).

    public string CredentialFilePath { get; set; }
    public string ProjectId { get; set; }
    public string ApiKey { get; set; }
    public string FirestoreEmulatorHost { get; set; }
    public string FirebaseAuthEmulatorHost { get; set; }
    public string FirebaseAuthUrlStart { get; set; }
    public string FirebaseAuthUrlEnd { get; set; }
}