using System.ComponentModel.DataAnnotations;
using HippoApi.Models;

namespace tests;

public class PatientModelTests
{
    private ValidationContext _validationContext;
    private List<ValidationResult> _validationResults;
    private PatientPrivate testP1;

    [SetUp]
    public void Setup()
    {
        _validationResults = new List<ValidationResult>();
        // Initialize testP1 with valid data
        testP1 = new PatientPrivate
        {
            FName = "John", // Valid first name
            LName = "Doe", // Valid last name
            Phone = "306-974-2038", // Valid phone number
            Age = 30, // Valid adult age
            Email = "john.doe@example.com", // Valid email
            DoctorPhoneNumber = "639-317-5582", // Valid doctor phone number
            Weight = 70, // Valid weight (kg)
            Height = 170, // Valid height (cm)
            GuardianPhoneNumber = "306-456-7892", // Valid guardian phone number
            Condition = "Disability" // Valid Condition
        };
    }

    private bool ValidateModel(object model)
    {
        // Initialize validation context for each model
        _validationContext = new ValidationContext(model);

        // Return whether the model is valid or not
        return Validator.TryValidateObject(model, _validationContext, _validationResults, true);
    }

    [Test]
    public void TestPatientPrivateWithAllValidPasses()
    {
        bool isValid = ValidateModel(testP1);
        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void TestPatientPrivateWithInvalidFirstNameFails()
    {
        testP1.FName = "";
        bool isValid = ValidateModel(testP1);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("First name is required"));

        testP1.FName = null;
        isValid = ValidateModel(testP1);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("First name is required"));
    }

    [Test]
    public void TestPatientPrivateWithInvalidLastNameFails()
    {
        testP1.LName = "";
        bool isValid = ValidateModel(testP1);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Last name is required"));

        testP1.LName = null;
        isValid = ValidateModel(testP1);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Last name is required"));
    }

    [Test]
    public void TestPatientPrivateWithInvalidPhoneFails()
    {
        testP1.Phone = "avc-nvc-dddd";
        bool isValid = ValidateModel(testP1);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Invalid phone number"));
    }

    [Test]
    public void TestPatientPrivateWithInvalidGuardianPhoneNumberFailsWhenAgeIsBelow18()
    {
        // If under 18, should require GuardianPhoneNumber
        testP1.Age = 16;
        testP1.GuardianPhoneNumber = null;

        bool isValid = ValidateModel(testP1);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage,
            Is.EqualTo("Guardian phone number is required when the age is below 18"));
    }

    [Test]
    public void TestPatientPrivateWithValidGuardianPhoneNumberPassesWhenAgeIsBelow18()
    {
        testP1.Age = 16;
        //this is a valid #
        testP1.GuardianPhoneNumber = "639-317-5573";

        bool isValid = ValidateModel(testP1);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }


    [Test]
    public void TestPatientPrivateWithMalformedGuardianPhoneNumberFailsWhenAgeIsBelow18()
    {
        testP1.Age = 16;
        //this is a valid #
        testP1.GuardianPhoneNumber = "639-317-xxxx";

        bool isValid = ValidateModel(testP1);

        Assert.IsFalse(isValid);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Invalid guardian phone number"));
    }

    [Test]
    public void TestPatientPrivateWithGuardianPhoneNumberNotRequiredWhenAgeIs18OrAbove()
    {
        // 18 or above, GuardianPhoneNumber is not required
        testP1.Age = 18;
        testP1.GuardianPhoneNumber = null;

        bool isValid = ValidateModel(testP1);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void TestPatientPrivateWithInvalidEmailFails()
    {
        testP1.Email = "invalid-email";
        bool isValid = ValidateModel(testP1);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Must be a valid email address"));
    }

    [Test]
    public void TestPatientPrivateWithInvalidWeightFails()
    {
        testP1.Weight = 10; // Invalid weight (below 20kg)
        bool isValid = ValidateModel(testP1);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Weight must be between 20kg and 300kg"));

        testP1.Weight = 350; // Invalid weight (above 300kg)
        isValid = ValidateModel(testP1);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Weight must be between 20kg and 300kg"));
    }

    [Test]
    public void TestPatientPrivateWithInvalidHeightFails()
    {
        testP1.Height = 40; // Invalid height (below 50cm)
        bool isValid = ValidateModel(testP1);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Age must be between 50cm and 300cm"));

        testP1.Height = 350; // Invalid height (above 300cm)
        isValid = ValidateModel(testP1);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Age must be between 50cm and 300cm"));
    }
}