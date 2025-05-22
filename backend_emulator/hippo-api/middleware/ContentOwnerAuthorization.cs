using System.Security.Claims;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using HippoApi.Controllers;
using HippoApi.Models;
using HippoApi.Models.enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;

namespace HippoApi.middleware;

/* Custom attributes Based off of guide by: Veerendra Annigere
 * https://www.c-sharpcorner.com/article/how-to-override-customauthorization-class-in-net-core/
 * use it by adding [ContentOwnerAuthorization] to the top of classes
 */

/// <summary>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ContentOwnerAuthorization : Attribute, IAsyncAuthorizationFilter
{
    public static readonly string DefaultUnauthorizedMessage = "You are not authorized to access this resource.";

    private readonly FirestoreDb _firestoreDb =
        FirestoreDb.Create(Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID"));


    /// <summary>
    ///     This will restrict to only allow content owners access to their own data
    /// </summary>
    public async Task OnAuthorizationAsync(AuthorizationFilterContext filterContext)
    {
        // Could find better way to ignore authorization if in testing mode
        bool isTesting = Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST") == "localhost:8080";
        if (isTesting) return;

        string authHeader = filterContext.HttpContext.Request.Headers["Authorization"];
        string token = authHeader.Substring("Bearer ".Length).Trim();

        // MUST explicitly check if token is revoked see documentation:
        // https://firebase.google.com/docs/auth/admin/manage-sessions#detect_id_token_revocation
        if (!await IsTokenValid(token))
        {
            SetUnauthorized(filterContext, DefaultUnauthorizedMessage);
            Console.WriteLine("Token is expired.");
            return;
        }

        // Ensure both role and userId exist
        // Get the authenticated user
        ClaimsPrincipal user = filterContext.HttpContext.User;
        if (user.Claims.IsNullOrEmpty())
        {
            SetUnauthorized(filterContext, DefaultUnauthorizedMessage);
            Console.WriteLine("Error: \tUser claims Null");
            return;
        }

        // Extract user ID and role from claims
        string userId = user.FindFirst("user_id")?.Value;
        // How to check the stored data
        string role = user.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;


        // Save this for later to debug check what is in the claims
        // foreach (Claim claim in user.Claims) Console.WriteLine($"Type: {claim.Type}, Value: {claim.Value}");
        // Console.WriteLine($"User: {userId} | Role: {role}");

        if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(userId))
        {
            SetUnauthorized(filterContext, "Error: Token does not have role or user id.");
            return;
        }

        // Can't add therapistId to every non owner route because if not required to fetch data could be faked so...
        // so set every route to either have therapistId, ownerId, or patientId then...
        if (await IsAuthorized(filterContext, userId, role))
        {
            // Valid access
            return;
        }
            

        // By default deny
        SetUnauthorized(filterContext, DefaultUnauthorizedMessage);
    }


    /// <summary>
    ///     Check if the provided user is allowed access to the resources
    /// </summary>
    /// <param name="filterContext">context to get route data from</param>
    /// <param name="userId">userId to check</param>
    /// <param name="role">role of user</param>
    /// <returns></returns>
    private async Task<bool> IsAuthorized(AuthorizationFilterContext filterContext, string userId, string role)
    {
        // Get the route data
        RouteData routeData = filterContext.RouteData;


        // Todo: add to list
        foreach (KeyValuePair<string, object?> key in routeData.Values)
        {
            if (key.ToString().Contains("Therapist"))
            {
                Console.WriteLine("Found therapistId");
                Console.WriteLine(key.ToString());
            }

            Console.WriteLine("Key: " + key.Key + " - Value: " + key.Value);
        }

        // Check if specific route parameters exist
        bool hasPatientId = routeData.Values.ContainsKey("patientId");
        bool hasTherapistId = routeData.Values.ContainsKey("therapistId");
        bool hasOwnerId = routeData.Values.ContainsKey("ownerId");

        string therapistId = routeData.Values["therapistId"]?.ToString();
        string patientId = routeData.Values["patientId"]?.ToString();
        string ownerId = routeData.Values["ownerId"]?.ToString();
        
        Console.WriteLine("\n---ContentOwnerAuthorization---");
        Console.WriteLine($"TherapistId: {hasTherapistId} {therapistId}");
        Console.WriteLine($"PatientId: {hasPatientId} {patientId}");
        Console.WriteLine($"OwnerId: {hasOwnerId} {ownerId}");
        Console.WriteLine($"Role: {role} for {userId}");

        // Track if they have authorized access to any ids
        List<bool> isAuthorizedList = new();

        // If therapist, check if they are owner of patient or their
        if (role == AccountRole.Therapist.GetDescription())
        {
            if (hasOwnerId)
            {
                Console.WriteLine("Unauthorized therapist cannot access ownerId resources");
                isAuthorizedList.Add(false);
            }

            // Check if owner of patient
            if (hasPatientId)
            {
                if (await IsTherapistContentOwnerOfPatient(userId, patientId))
                {
                    Console.WriteLine($"Therapist {userId} is content owner of patient {patientId}");
                    isAuthorizedList.Add(true);
                }
                else
                {
                    isAuthorizedList.Add(false);
                }
            }

            // check if therapistId matches the current requesting user
            if (hasTherapistId)
            {
                if (therapistId == userId)
                    isAuthorizedList.Add(true);
                else
                    isAuthorizedList.Add(false);
            }
        }
        // If owner check valid access cases
        else if (role == AccountRole.Owner.GetDescription())
        {
            Console.WriteLine("Checking owner auth...");
            // check if ownerId matches the current requesting user
            if (hasOwnerId)
            {
                if (ownerId == userId)
                    isAuthorizedList.Add(true);
                else
                    isAuthorizedList.Add(false);
            }

            // Is owner the content owner of this patient
            if (hasPatientId)
            {
                if (await IsOwnerContentOwnerOfPatient(userId, patientId))
                    isAuthorizedList.Add(true);
                else
                    isAuthorizedList.Add(false);
            }

            // Is owner the content owner of this therapist
            if (hasTherapistId)
            {
                if (await IsOwnerContentOwnerOfTherapist(userId, therapistId))
                    isAuthorizedList.Add(true);
                else
                    isAuthorizedList.Add(false);
            }
            // TODO: add special cases here
            // maybe PUT /owners/{ownerId}/{oldTherapistId/newTherapistId} reassign patients between therapists
        }

        // Only allow access if content owner of every id in route
        return !isAuthorizedList.Contains(false);

        // If needed to check the path for custom validation
        // var path = filterContext.HttpContext.Request.Path.Value;
    }

    /// <summary>
    ///     Checks if the therapist is the owner of the provided patient
    /// </summary>
    /// <param name="userId">therapistId</param>
    /// <param name="patientId">patientId to search for</param>
    /// <returns>true if they are, false otherwise</returns>
    public async Task<bool> IsTherapistContentOwnerOfPatient(string userId, string patientId)
    {
        Console.WriteLine("checking is therapist owner of patient...");
        try
        {
            // Get fetch patient document and check therapistId in it
            DocumentSnapshot? patientDoc = _firestoreDb
                .Collection(PatientController.COLLECTION_NAME)
                .Document(patientId)
                .GetSnapshotAsync().Result;

            // Convert to model
            string contentOwnerTherapistId = patientDoc.ConvertTo<PatientPrivate>().TherapistID;

            // Return if it matches the therapistId who is requesting it
            return contentOwnerTherapistId == userId;
        }
        // If any error return false
        catch (Exception e)
        {
            return false;
        }
    }

    /// <summary>
    ///     Checks if the provided owner is the content owner of the therapist
    /// </summary>
    /// <param name="userId">ownerId</param>
    /// <param name="therapistId">therapistId to see if they are under them</param>
    /// <returns>true if yes, false otherwise</returns>
    public async Task<bool> IsOwnerContentOwnerOfTherapist(string userId, string therapistId)
    {
        Console.WriteLine("checking is owner an owner of therapist...");
        bool isContentOwner = false;
        try
        {
            // Get fetch patient document and check therapistId in it
            Query userQuery = _firestoreDb
                .Collection(OwnerController.COLLECTION_NAME)
                .Document(userId)
                .Collection(TherapistController.COLLECTION_NAME);
            QuerySnapshot userQuerySnapshot = await userQuery.GetSnapshotAsync();
            foreach (DocumentSnapshot documentSnapshot in userQuerySnapshot.Documents)
                if (documentSnapshot.Id == therapistId)
                {
                    isContentOwner = true;
                    break;
                }


            return isContentOwner;
        }
        // If any error return false
        catch (Exception e)
        {
            return false;
        }
    }

    /// <summary>
    ///     Checks if the provided userId is content owner of the patien
    /// </summary>
    /// <param name="userId">ownerId of requesting owner</param>
    /// <param name="patientId">patientId to look for</param>
    /// <returns></returns>
    public async Task<bool> IsOwnerContentOwnerOfPatient(string userId, string patientId)
    {
        Console.WriteLine("checking is owner content owner of patient...");
        bool isContentOwner = false;
        string patientTherapistId;
        try
        {
            // Get fetch patient document and get the therapist Id in it
            DocumentSnapshot? patientDoc = _firestoreDb
                .Collection(PatientController.COLLECTION_NAME)
                .Document(patientId)
                .GetSnapshotAsync().Result;

            // Convert to model
            patientTherapistId = patientDoc.ConvertTo<PatientPrivate>().TherapistID;


            // Get fetch patient document and check therapistId in it
            Query userQuery = _firestoreDb
                .Collection(OwnerController.COLLECTION_NAME)
                .Document(userId)
                .Collection(TherapistController.COLLECTION_NAME);
            QuerySnapshot userQuerySnapshot = await userQuery.GetSnapshotAsync();
            foreach (DocumentSnapshot documentSnapshot in userQuerySnapshot.Documents)
                if (documentSnapshot.Id == patientTherapistId)
                {
                    isContentOwner = true;
                    return isContentOwner;
                }

            return isContentOwner;
        }
        // If any error return false
        catch (Exception e)
        {
            return false;
        }
    }

    /// <summary>
    ///     Helper method to make returning invalid access with custom message shorter.
    ///     Call return right after.
    /// </summary>
    /// <param name="filterContext">context to set the result of</param>
    /// <param name="message">Message to return</param>
    private void SetUnauthorized(AuthorizationFilterContext filterContext, string message)
    {
        // filterContext.Result = new UnauthorizedResult();
        filterContext.HttpContext.Response.Headers["X-Custom-Auth-Message"] = message;
        // Return plain text unauthorized message
        filterContext.Result = new ContentResult
        {
            Content = message,
            StatusCode = StatusCodes.Status401Unauthorized,
            ContentType = "text/plain"
        };
    }

    /// <summary>
    ///     Check if token is expired.
    ///     https://firebase.google.com/docs/auth/admin/manage-sessions#c
    /// </summary>
    /// <param name="idToken"></param>
    /// <returns></returns>
    public static async Task<bool> IsTokenValid(string idToken)
    {
        try
        {
            Console.WriteLine("Checking token is revoked...");
            // Verify the ID token while checking if the token is revoked by passing checkRevoked
            // as true.
            bool checkRevoked = true;
            FirebaseToken? decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(
                idToken, checkRevoked);
            // Token is valid and not revoked.
            string uid = decodedToken.Uid;
            Console.WriteLine($"all good {uid}");
            return true;
        }
        catch (FirebaseAuthException ex)
        {
            Console.WriteLine($"not good token error: {ex.Message}");
            if (ex.AuthErrorCode == AuthErrorCode.RevokedIdToken)
                // Token has been revoked. Inform the user to re-authenticate or signOut() the user.
                return false;

            // Token is invalid.
            return false;
        }
    }
}