using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using HippoApi.controllers;
using HippoApi.middleware;
using HippoApi.Models;
using HippoApi.models.custom_responses;
using HippoApi.Models.enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


//using nodemailer;
namespace HippoApi.Controllers;

/// <summary>
///     Controller to handle Therapist registration, logins, logouts, and password resets.
///     Firebase REST API doc
///     https://firebase.google.com/docs/reference/rest/auth/
/// </summary>
[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly FirebaseAuth _firebaseAuth;
    private readonly FirestoreDb _firestore;
    private readonly bool isDev;
    private readonly int port;
    private readonly CollectionReference referralCollection;
    private readonly string smtpServer = "";

    public AuthController(FirestoreDb firestore)
    {
        _firestore = firestore;
        _firebaseAuth = FirebaseAuth.DefaultInstance;
        referralCollection = _firestore.Collection("referrals");
        isDev = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Production"; // 
        if (!isDev)
        {
            smtpServer = Environment.GetEnvironmentVariable("SMTP_SERVER");
            port = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT"));
        }
    }


    /// <summary>
    ///     This will take in an email, and send out a password reset link, if it exists
    ///     otherwise it will return an error
    /// </summary>
    /// <param name="email">Valid email of an existing user in firestore to reset</param>
    /// <returns>
    ///     OK if sent out successfully, or a BadRequest if the email is invalid,
    ///     or a Problem  with an explanation to the failure if it was something else
    /// </returns>
    [HttpPost("request-password-reset-email")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestPasswordResetEmail([FromBody] string email)
    {
        string passResetLink;

        try
        {
            passResetLink = await _firebaseAuth.GeneratePasswordResetLinkAsync(email);
            Console.WriteLine($"Password reset link generated {passResetLink}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e.Message}\nUser email:{email}\n");
            return BadRequest("Invalid email");
        }

        try
        {
            await SendEmail(email, new MailAddress("noreply@hippotherapy.ca", "Hippotherapy Administrator"),
                "Password Reset",
                "<h2>You have requested a password reset, navigate to this link to reset your password</h2>" +
                $"Link: <a href=\"{Request.Headers.Origin}/?link={Uri.EscapeDataString(passResetLink)}#/reset-password\">{Request.Headers.Origin}/?link={Uri.EscapeDataString(passResetLink)}#/reset-password</a>");
        }
        catch (Exception e)
        {
            return BadRequest($"Unable to send password reset email to email {email} with error: {e.Message}");
        }

        return Ok("Password reset email sent");
    }

    /// <summary>
    ///     Register method which registers a Therapist with Firebase Authentication and also
    ///     with the local firebase emulator.
    /// </summary>
    /// <param name="req">
    ///     TherapistRegistrationRequest which contains details about the
    ///     Therapist as well as their password to be passed to Firebase Auth
    /// </param>
    /// <returns></returns>
    [HttpPost("therapist/register")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterTherapist([FromBody] TherapistRegistrationRequest req)
    {
        try
        {
            // Validate the request
            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState is invalid.");
                foreach (ModelError error in ModelState.Values.SelectMany(v => v.Errors))
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");

                // Return model validation errors
                return BadRequest(ModelState);
            }

            // Check if email is already registered
            try
            {
                UserRecord? res = await _firebaseAuth.GetUserByEmailAsync(req.Email);
                Console.WriteLine("Registration failed: Email is already registered.");
                return BadRequest(new { Message = $"Email {req.Email} is already registered." });
            }
            catch (FirebaseAuthException)
            {
                // User does not exist continue
            }

            try
            {
                // set the verified to true to skip email verf if email is the same as ref email
                Dictionary<string, string> referralRef = await VerifyReferral(req.Referral);
                req.OwnerId = referralRef["ownerID"];
                await referralCollection.Document(req.Referral).DeleteAsync();

                // set verified here
                if (referralRef["email"] == req.Email) req.Verified = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not grab ownerId from referrals Error: " + e);
                // Return not acceptable if cannot find referral code and redeem it
                return StatusCode(406);
            }

            UserRecord createdUser;
            UserRecordArgs ownerArgs = new()
            {
                Email = req.Email.ToLower(),
                Password = req.Password,
                EmailVerified = false
            };
            try
            {
                createdUser = await _firebaseAuth.CreateUserAsync(ownerArgs);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error creating owner {ownerArgs.Email}" + e);
                return BadRequest(new { Message = "Failed to register user. Please try again later." });
            }

            if (req.Verified == true)
                // make user verified manually
                await _firebaseAuth.UpdateUserAsync(new UserRecordArgs
                {
                    Uid = (await _firebaseAuth.GetUserByEmailAsync(req.Email)).Uid,
                    EmailVerified = true
                });
            else //send verification email if user has different email than owner specification
                await SendVerfEmail(req.Email);

            string uid = createdUser.Uid;

            // Save therapist data to Firestore
            Therapist therapist = new()
            {
                TherapistID = uid,
                FName = req.FName,
                LName = req.LName,
                Email = req.Email,
                Country = req.Country ?? "Unknown",
                City = req.City ?? "Unknown",
                Street = req.Street ?? "Unknown",
                PostalCode = req.PostalCode ?? "0000",
                Phone = req.Phone ?? "N/A",
                Profession = req.Profession ?? "N/A",
                Major = req.Major ?? "N/A",
                YearsExperienceInHippotherapy = req.YearsExperienceInHippotherapy
            };

            if (req.OwnerId != null)
            {
                Console.WriteLine($"Attempting to save therapist under {req.OwnerId} owner collection");
                await _firestore.Collection(OwnerController.COLLECTION_NAME).Document(req.OwnerId)
                    .Collection(TherapistController.COLLECTION_NAME).Document(uid).SetAsync(therapist);
            }
            else
            {
                await _firestore.Collection(TherapistController.COLLECTION_NAME).Document(uid).SetAsync(therapist);
            }

            Console.WriteLine("Therapist data saved in Firestore.");
            // Add therapist role
            await SetUserRole(uid, AccountRole.Therapist);

            if (req.Verified == true)
                // Success response
                return Ok(new
                    { Message = "Therapist registered successfully!", UID = uid });


            return Accepted("Therapist successfully saved, but not verified.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return BadRequest(new
                { Message = "An unexpected error occurred during registration. Please try again later." });
        }
    }

    /// <summary>
    ///     Sends a verification email to the email given as a parameter
    /// </summary>
    /// <param name="email"></param>
    public async Task<IActionResult> SendVerfEmail(string email)
    {
        string origin = "";
        try
        {
            origin = Request.Headers.Origin.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine("we are generating locally.");
        }

        try
        {
            string link = $"{origin}/?verify={await GetVerificationUrl(email)}#/login";
            await SendEmail(email, new MailAddress("noreply@hippotherapy.ca", "Hippotherapy Administrator"),
                "Email Verification",
                $"<h2>Click <a href=\"{link}\">here</a> to verify your account</h2>" +
                $"<br /> Link: <a href=\"{link}\">{link}<a/>"
            );
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw; // throw to ex to parent
        }

        return Ok();
    }


    /// <summary>
    ///     /// Returns a verification link for user of given email
    /// </summary>
    /// <param name="req">
    /// </param>
    /// <returns></returns>
    public async Task<string> GetVerificationUrl(string email)
    {
        try
        {
            email = email.ToLower();
            string? link =
                await _firebaseAuth.GenerateEmailVerificationLinkAsync(email);
            return link;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting verification link for email: '{email}' : {ex.Message}");
            return ex.Message;
        }
    }


    /// <summary>
    ///     Returns true if the given email is verified. Otherwise returns false.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task<bool> IsUserVerified(string email)
    {
        try
        {
            // Convert to lowercase as for some reason it matters for verification
            email = email.ToLower();
            UserRecord userRecord = await _firebaseAuth.GetUserByEmailAsync(email);
            Console.WriteLine(userRecord);
            Console.WriteLine(email + " verified: " + userRecord.EmailVerified);
            bool isVerified = userRecord.EmailVerified;
            return isVerified;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }


    /// <summary>
    ///     /// Login method for Therapists
    /// </summary>
    /// <param name="req">
    ///     TherapistLoginRequest used to isolate email and password
    ///     for use with Firebase Authentication
    /// </param>
    /// <returns></returns>
    [HttpPost("therapist/login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginTherapist([FromBody] LoginRequest req)
    {
        return await Login(req, AccountRole.Therapist);
    }


    private async Task<IActionResult> Login(LoginRequest req, AccountRole role)
    {
        try
        {
            var payload = new
            {
                email = req.Email.ToLower(),
                password = req.Password,
                returnSecureToken = true
            };

            string firebaseAuthUrl = MakeAuthUrl("accounts:signInWithPassword");

            using HttpClient client = new();
            HttpResponseMessage response = await client.PostAsync(
                firebaseAuthUrl,
                new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            );

            string responseContent = await response.Content.ReadAsStringAsync();

            //checks if user is verified
            bool userVerified = await IsUserVerified(req.Email.ToLower());

            // userVerified = true; // TODO - for debugging

            if (response.IsSuccessStatusCode && userVerified)
            {
                JObject authResponse = JObject.Parse(responseContent);
                string? idToken = authResponse["idToken"]?.ToString();
                string? userId = authResponse["localId"]?.ToString();

                Console.WriteLine($"User ID in Login method: {userId}");
                LoginResponse loginResponse = new("Login successful", idToken, userId);
                // return Ok(new { Message = "Login successful!", Token = idToken, UserId = userId });
                return Ok(loginResponse);
            }

            // Handle specific Firebase error messages
            JObject errorResponse = JObject.Parse(responseContent);
            string? firebaseError = errorResponse["error"]?["message"]?.ToString();

            if (firebaseError == "EMAIL_NOT_FOUND" || firebaseError == "INVALID_PASSWORD")
                return Unauthorized(new { ErrorType = "InvalidCredentials", Message = "Invalid email or password." });
            if (!userVerified)
            {
                Console.WriteLine("About to send email");
                IActionResult emailRes = await SendVerfEmail(req.Email.ToLower());
                Console.WriteLine(emailRes);

                return Unauthorized(new
                    { Message = "Please verify you email before logging in. We have resent a verification email" });
            }

            return Unauthorized(new { ErrorType = "General", Message = firebaseError ?? "Login failed." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { ErrorType = "Exception", Message = $"Login failed: {ex.Message}" });
        }
    }

    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Logout()
    {
        try
        {
            string authHeader = HttpContext.Request.Headers["Authorization"];
            string token = authHeader.Substring("Bearer ".Length).Trim();

            // MUST explicitly check if token is revoked see documentation:
            // https://firebase.google.com/docs/auth/admin/manage-sessions#detect_id_token_revocation
            if (!await ContentOwnerAuthorization.IsTokenValid(token))
            {
                Console.WriteLine("Token is expired.");
                return Unauthorized("You are already logged out.");
            }

            // Source: https://firebase.google.com/docs/auth/admin/manage-sessions#c
            // Get the user
            ClaimsPrincipal userInToken = HttpContext.User;
            string uid = userInToken.FindFirst("user_id")?.Value;


            await FirebaseAuth.DefaultInstance.RevokeRefreshTokensAsync(uid);
            UserRecord? user = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
            Console.WriteLine("Tokens revoked at: " + user.TokensValidAfterTimestamp);

            // invalidate the token
            return Ok(new { Message = "Logout successful." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during logout: {ex.Message}");
            return StatusCode(500, new { Error = "An error occurred while logging out." });
        }
    }


    /// <summary>
    ///     Register a new owner for the app
    /// </summary>
    /// <param name="req">The Owner to register</param>
    /// <returns>BadRequest if it fails validation or there is an error, else Ok</returns>
    [HttpPost("owner/register")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterOwner([FromBody] OwnerRegistrationRequest req)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState is invalid.");
                foreach (ModelError error in ModelState.Values.SelectMany(v => v.Errors))
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");

                return BadRequest(ModelState);
            }

            // Check if email is already registered
            try
            {
                UserRecord? res = await _firebaseAuth.GetUserByEmailAsync(req.Email);
                Console.WriteLine("Registration failed: Email is already registered.");
                return BadRequest(new { Message = $"Email {req.Email} is already registered." });
            }
            catch (FirebaseAuthException)
            {
                // User does not exist continue
            }

            UserRecord createdUser;
            UserRecordArgs ownerArgs = new()
            {
                Email = req.Email.ToLower(),
                Password = req.Password,
                EmailVerified = req.Verified
            };

            try
            {
                createdUser = await _firebaseAuth.CreateUserAsync(ownerArgs);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error creating owner {ownerArgs.Email}" + e);
                return BadRequest(new { Message = "Failed to register user. Please try again later." });
            }

            //send verification email

            await SendVerfEmail(req.Email);


            // Add owner role
            await SetUserRole(createdUser.Uid, AccountRole.Owner);

            // Check if the user exists in Firebase  
            try
            {
                UserRecord? userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(createdUser.Uid);
                if (userRecord == null)
                    Console.WriteLine($"User with UID {createdUser.Uid} does not exist in Firebase.");
            }
            catch (FirebaseAuthException ex)
            {
                Console.WriteLine($"Firebase Auth Exception: {ex.Message}");
            }

            Owner owner = new()
            {
                OwnerId = createdUser.Uid,
                FName = req.FName,
                LName = req.LName,
                Email = req.Email
            };

            // Make owner object
            await _firestore.Collection(OwnerController.COLLECTION_NAME).Document(createdUser.Uid).SetAsync(owner);

            Console.WriteLine("Owner data saved in Firestore.");


            // Success response  
            return Ok(createdUser.Uid);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return BadRequest(new
                { Message = "An unexpected error occurred during registration. Please try again later." });
        }
    }


    [HttpPost("owner/login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginOwner([FromBody] LoginRequest req)
    {
        return await Login(req, AccountRole.Owner);
    }

    /// <summary>
    ///     Get a list of therapist ids for an ownerId
    /// </summary>
    public async Task<List<string>> GetAllTherapistIdsForOwner(string ownerId)
    {
        List<string> ids = new();
        Query userQuery = _firestore
            .Collection(OwnerController.COLLECTION_NAME)
            .Document(ownerId)
            .Collection(TherapistController.COLLECTION_NAME);
        QuerySnapshot userQuerySnapshot = await userQuery.GetSnapshotAsync();
        foreach (DocumentSnapshot documentSnapshot in userQuerySnapshot.Documents)
            ids.Add(documentSnapshot.Id);
        return ids;
    }

    /// <summary>
    ///     Generates a referral code and emails it out.
    /// </summary>
    /// <param name="request">The ownerID that the therapist is to be assoicated with and the email of the therapist</param>
    /// <param name="expiryDate">Specify expiry time for the record</param>
    /// <returns>Returns 503 Service Unavailable if sending email fails. Returns 200 if send and generated on backend referral</returns>
    [HttpPost("referral")]
    public async Task<IActionResult> GenerateReferral([FromBody] ReferralRequest request, int expiryDate = 1800)
    {
        string origin = "";
        try
        {
            origin = Request.Headers.Origin.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine("we are generating locally.");
        }

        // validate the email
        if (!Regex.Match(request.Email, "^\\S+@\\S+\\.\\S+$").Success)
            return NotFound($"Email: {request.Email} is an invalid email address.");

        // declare random to generate random integer that is 6 digits
        Random rnd = new();
        int code = rnd.Next(100000, 999999);

        try
        {
            // check if code already exists, if not, remake
            while (true)
            {
                // searches referral collection for a code
                DocumentReference docRef = referralCollection.Document(code.ToString());
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    code = rnd.Next(100000, 999999);
                    continue;
                }

                ;

                // if code already exists, exit
                break;
            }

            Dictionary<string, string> referral = new()
            {
                { "code", code.ToString() },
                { "ownerID", request.OwnerId },
                { "email", request.Email },
                { "expiryDate", DateTime.UtcNow.AddSeconds(expiryDate).ToString() }
            };


            DocumentReference
                referralDocument = referralCollection.Document(code.ToString()); // add document to firebase
            await referralDocument.SetAsync(referral);

            try
            {
                string assembledUrl =
                    $"{origin}/?code={code}&owner={request.OwnerId}&email={Uri.EscapeDataString(request.Email)}#/register";
                await SendEmail(referral["email"],
                    new MailAddress("noreply@hippotherapy.ca", "Hippotherapy Administrator"),
                    "Referral Code For Registration",
                    "<h1>Hippotherapy App</h1>" +
                    $"<h2>Your referral code is {referral["code"]}</h2><p>Please goto Hippotherapy registration page to input referral code and complete registration.<br />" +
                    $"Or use link to Hippotherapy Therapist Registration Page: <a href=\"{assembledUrl}\">CLICK HERE</a><br />" +
                    $"<p><br />Verification Link: <a href=\"{assembledUrl}\">{assembledUrl}</a><p>");
            }

            catch (Exception ex)
            {
                // cannot send email therefore return 503, service unavailable
                return StatusCode(503, $"Cannot send to email: {referral["email"]} for referral. Ex: " + ex.Message);
            }

            return Ok(new List<string>
                { request.OwnerId, code.ToString() }); // return code and ownerId to be associated
        }
        catch (Exception ex)
        {
            return Problem($"Failed to generate referral for code {code}: {ex.Message}");
        }
    }

    /// <summary>
    ///     Verifies the referral code that the therapist has entered.
    ///     Also deletes any expired Therapist Controllers
    /// </summary>
    /// <param name="code">6-digit integer only code</param>
    /// <returns>JSON of strings that consists of OwnerID and Email assoicated with the referral Code</returns>
    public async Task<Dictionary<string, string>> VerifyReferral(string code)
    {
        await foreach (DocumentReference? referralDocument in referralCollection.ListDocumentsAsync())
        {
            DocumentSnapshot referralSnapshot = await referralDocument.GetSnapshotAsync();
            Dictionary<string, string> referral = referralSnapshot.ConvertTo<Dictionary<string, string>>();

            // if expirydate surpasses now, delete it
            if (DateTime.Parse(referral["expiryDate"]) < DateTime.UtcNow) await referralDocument.DeleteAsync();
        }

        // grab the values now
        DocumentReference docRef = referralCollection.Document(code);
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
        Dictionary<string, string> currentReferral = snapshot.ConvertTo<Dictionary<string, string>>();

        //return the dictionary
        return currentReferral;
    }

    /// <summary>
    ///     Method to send email with the mail client.
    /// </summary>
    /// <param name="clientEmail">Client email to send to</param>
    /// <param name="from">The mail address that we are sending from</param>
    /// <param name="subject">The subject line</param>
    /// <param name="body">The message of the body</param>
    private async Task SendEmail(string clientEmail, MailAddress from, string subject, string body)
    {
        try
        {
            // declare mail client
            using SmtpClient mailClient = new(isDev ? "localhost" : smtpServer, isDev ? 1025 : port);
            // send off email
            await mailClient.SendMailAsync(new MailMessage
            {
                To = { clientEmail },
                From = from,
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            }, CancellationToken.None);
        }
        catch (Exception e)
        {
            Console.WriteLine($"There was an error sending out the email: {e.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Set custom claims for a user
    /// </summary>
    /// <param name="uid">user id to apply it to</param>
    /// <param name="role">role to give them</param>
    public async Task SetUserRole(string uid, AccountRole role)
    {
        Console.WriteLine($"Setting user role to {role} for {uid}");
        // Check if running in the Firebase Auth Emulator
        bool isEmulator = Environment.GetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST") != null;

        if (isEmulator)
        {
            Console.WriteLine("Skipping SetCustomUserClaimsAsync because the emulator does not support it.");
            return;
        }

        try
        {
            Dictionary<string, object> claims = new()
            {
                { "role", role.GetDescription() } // "owner" or "therapist"
            };
            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(uid, claims);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error when setting role for {uid} to {role}: {e.Message}");

            // DO NOT throw error since this will fail for the emulator
        }
    }

    /// <summary>
    ///     Make the auth url based on environment variables
    ///     See https://firebase.google.com/docs/reference/rest/auth#section-sign-in-with-oauth-credential
    ///     for options
    ///     e.g. request = "accounts:signInWithPassword"
    /// </summary>
    /// <param name="request">request to make</param>
    /// <returns>string with full url having the key</returns>
    private string MakeAuthUrl(string request)
    {
        try
        {
            string start = Environment.GetEnvironmentVariable("FIREBASE_AUTH_URL_START");
            string end = Environment.GetEnvironmentVariable("FIREBASE_AUTH_URL_END");
            string authUrl = $"{start}{request}{end}";
            Console.WriteLine(authUrl);
            return authUrl;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error environment variables not set in AuthController: {e.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Returns a hard coded list of possible obstructions a therapist can encounter during an evaluation
    /// </summary>
    /// <returns>A list of tags in string format</returns>
    [HttpGet]
    [Route("tags")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEvaluationTagList()
    {
        return Ok(Utils.GetEvaluationTagList());
    }
}