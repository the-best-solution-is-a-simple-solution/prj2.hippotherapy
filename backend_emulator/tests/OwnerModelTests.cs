using System.ComponentModel.DataAnnotations;
using HippoApi.Models;

namespace tests;

public class OwnerModelTests
{
    private OwnerRegistrationRequest _testRequest;
    private ValidationContext _validationContext;
    private List<ValidationResult> _validationResults;

    [SetUp]
    public void Setup()
    {
        _validationResults = new List<ValidationResult>();
        _testRequest = new OwnerRegistrationRequest
        {
            Email = "test.email@example.com",
            Password = "Password1!",
            FName = "John",
            LName = "Doe"
        };
    }

    private bool ValidateModel(object model)
    {
        _validationContext = new ValidationContext(model);
        return Validator.TryValidateObject(model, _validationContext, _validationResults, true);
    }

    [Test]
    public void Validate_EmailIsRequired()
    {
        _testRequest.Email = null;

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Email is required."));
    }

    [Test]
    public void Validate_EmailFormatIsValid()
    {
        _testRequest.Email = "invalid-email-format";


        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Invalid email format."));
    }

    [Test]
    public void Validate_EmailIsValid()
    {
        _testRequest.Email = "validemail@example.com";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_FirstNameIsRequired()
    {
        _testRequest.FName = "";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("First name is required."));
    }

    [Test]
    public void Validate_FirstNameExceedsMaxLength()
    {
        _testRequest.FName = new string('A', 21);

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage,
            Is.EqualTo("First name must be at most 20 characters."));
    }

    [Test]
    public void Validate_FirstNameContainsInvalidCharacters()
    {
        _testRequest.FName = "John123";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("First name must contain only letters."));
    }

    [Test]
    public void Validate_FirstName_MaxEdgeCase()
    {
        _testRequest.FName = new string('A', 20);

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_FirstName_MinEdgeCase()
    {
        _testRequest.FName = "A";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }


    // Last name tests
    [Test]
    public void Validate_LastNameIsRequired()
    {
        _testRequest.LName = "";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Last name is required."));
    }

    [Test]
    public void Validate_LastNameExceedsMaxLength()
    {
        _testRequest.LName = new string('A', 21);

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage,
            Is.EqualTo("Last name must be at most 20 characters."));
    }

    [Test]
    public void Validate_LastNameContainsInvalidCharacters()
    {
        _testRequest.LName = "Doe123";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Last name must contain only letters."));
    }

    [Test]
    public void Validate_LastName_MaxEdgeCase()
    {
        _testRequest.LName = new string('A', 20);

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_LastName_MinEdgeCase()
    {
        _testRequest.LName = "A";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }
}