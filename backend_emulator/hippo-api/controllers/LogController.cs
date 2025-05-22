using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;
using HippoApi.Models;
using HippoApi.Models.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace HippoApi.controllers;

/// <summary>
/// A controller to handle method related to guest mode like login an email used to login for a potential
/// mailing list
/// </summary>
[Route("log")]
public class LogController : ControllerBase
{
    private static FirestoreDb _firestore;
    public const string COLLECTION_NAME = "logs";
    
    public LogController(FirestoreDb firestore)
    {
        _firestore = firestore;
    }
    
    /// <summary>
    /// Log email and time for guest login
    /// </summary>
    /// <returns>
    /// Status 200 if successful and valid email
    /// Status 400 (Bad Request) if unsuccessful
    /// </returns>
    [HttpPost("login-guest")]
    [AllowAnonymous]
    public async Task<IActionResult> LogGuestLogin([FromBody] string email)
    {
        // Make Login Record for guest
        UserLoginRecord record = new UserLoginRecord
        {
            Email = email,
            DateTaken = DateTime.Now,
            Role = AccountRole.Guest
        };
        
        // validate email
        bool isValid = ValidateModel(record);
        
        // If not valid send bad request
        if (!isValid)
        {
            return BadRequest("Invalid email");
        }
        
        // Save new document in log collection
        await _firestore.Collection(COLLECTION_NAME).AddAsync(record);
        return Ok("Success log");
    }
    
    /// <summary>
    /// A helper method to validate the model
    /// </summary>
    /// <param name="model">model to validate</param>
    /// <returns>true if valid, false otherwise</returns>
    private bool ValidateModel(object model)
    {
        List<ValidationResult> _validationResults = new List<ValidationResult>();
        ValidationContext _validationContext = new ValidationContext(model);
        return Validator.TryValidateObject(model, _validationContext, _validationResults, true);
    }
    
    
    
}