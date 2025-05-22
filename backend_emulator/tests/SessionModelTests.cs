using System.ComponentModel.DataAnnotations;
using HippoApi.Models;

namespace tests;

public class SessionModelTests
{
    private ValidationContext _validationContext;
    private List<ValidationResult> _validationResults;
    private Session testS1;

    [SetUp]
    public void Setup()
    {
        _validationResults = new List<ValidationResult>();
        testS1 = new Session
        {
            SessionID = "9332a786-e777-486c-995f-41fc5afc678c",
            Location = "NA",
            DateTaken = new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            PatientID = "something"
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
    public void TestSessionWithAllValidPasses()
    {
        bool isValid = ValidateModel(testS1);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void TestSessionWithInvalidLocation()
    {
        testS1.Location = "C";
        bool isValid = ValidateModel(testS1);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Location must be at least 2 characters long"));

        testS1.Location = "Canada";
        isValid = ValidateModel(testS1);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.Last().ErrorMessage, Is.EqualTo("Location must be at most 2 characters long"));
    }

    [Test]
    public void TestSessionWithNullLocation()
    {
        testS1.Location = null;
        bool isValid = ValidateModel(testS1);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Location is required"));
    }
}