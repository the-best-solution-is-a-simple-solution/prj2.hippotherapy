using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace tests.Models;

/// <summary>
///     This class is used to help with validating model's in unit tests.
///     Apparently when using controllers directly in testsyou bypass model validation, so this
///     class allows manual validation.
/// </summary>
public static class TestValidationHelper
{
    /// <summary>
    ///     Validates a model and populates the ModelState of the provided controller.
    /// </summary>
    /// <param name="model">The model to validate.</param>
    /// <param name="controller">The controller whose ModelState will be populated.</param>
    public static async Task ValidateModel(object model, ControllerBase controller)
    {
        ValidationContext validationContext = new(model, null, null);
        List<ValidationResult> validationResults = new();

        //Performs validation of model based on annotations
        Validator.TryValidateObject(model, validationContext, validationResults, true);

        foreach (ValidationResult validationResult in validationResults)
        foreach (string memberName in validationResult.MemberNames)
            controller.ModelState.AddModelError(memberName, validationResult.ErrorMessage);
    }
}