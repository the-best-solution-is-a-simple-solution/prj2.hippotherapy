using System.ComponentModel.DataAnnotations;
using HippoApi.Models;

namespace tests;

public class TherapistModelTests
{
    private TherapistRegistrationRequest _testRequest;
    private ValidationContext _validationContext;
    private List<ValidationResult> _validationResults;

    [SetUp]
    public void Setup()
    {
        _validationResults = new List<ValidationResult>();
        _testRequest = new TherapistRegistrationRequest
        {
            Email = "test.email@example.com",
            Password = "Password1!",
            FName = "John",
            LName = "Doe",
            Country = "USA",
            City = "New York",
            Street = "123 Main St.",
            PostalCode = "A1A 1A1",
            Phone = "+1-123-456-7890",
            Profession = "Therapist",
            Major = "Psychology",
            YearsExperienceInHippotherapy = 5
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

    // Country tests
    [Test]
    public void Validate_CountryExceedsMaxLength()
    {
        _testRequest.Country = new string('A', 21);

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Country must not exceed 20 characters."));
    }

    [Test]
    public void Validate_CountryContainsInvalidCharacters()
    {
        _testRequest.Country = "USA123";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage,
            Is.EqualTo("Country must contain only letters and spaces."));
    }

    [Test]
    public void Validate_Country_MaxEdgeCase()
    {
        _testRequest.Country = new string('A', 20);

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_Country_MinEdgeCase()
    {
        _testRequest.Country = "A";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_CountryContainsSpaces()
    {
        _testRequest.Country = "United States";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_CountryIsEmpty()
    {
        _testRequest.Country = "";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    // City tests
    [Test]
    public void Validate_CityExceedsMaxLength()
    {
        _testRequest.City = new string('A', 21);

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("City must not exceed 20 characters."));
    }

    [Test]
    public void Validate_CityContainsInvalidCharacters()
    {
        _testRequest.City = "City123";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("City must contain only letters and spaces."));
    }

    [Test]
    public void Validate_City_MaxEdgeCase()
    {
        _testRequest.City = new string('A', 20);

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_City_MinEdgeCase()
    {
        _testRequest.City = "A";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_CityContainsSpaces()
    {
        _testRequest.City = "New York";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_CityIsEmptyString()
    {
        _testRequest.City = "";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    // Street tests
    [Test]
    public void Validate_StreetExceedsMaxLength()
    {
        _testRequest.Street = new string('A', 26);

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Street must not exceed 20 characters."));
    }

    [Test]
    public void Validate_StreetContainsInvalidCharacters()
    {
        _testRequest.Street = "Main St@";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage,
            Is.EqualTo("Street must contain only letters, numbers, spaces, and common punctuation."));
    }

    [Test]
    public void Validate_Street_MaxEdgeCase()
    {
        _testRequest.Street = new string('A', 20);

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_Street_MinEdgeCase()
    {
        _testRequest.Street = "A";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_StreetContainsCommonCharacters()
    {
        _testRequest.Street = "123 Main St., Apt-5";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_StreetIsEmptyString()
    {
        _testRequest.Street = "";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }


    // Postal code tests
    [Test]
    public void Validate_PostalCodeValidFormat_Space()
    {
        _testRequest.PostalCode = "A1A 1A1";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_PostalCodeValidFormat_Dash()
    {
        _testRequest.PostalCode = "A1A-1A1";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_PostalCodeValidFormat_NoSeparator()
    {
        _testRequest.PostalCode = "A1A1A1";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_PostalCodeInvalidFormat_ExtraCharacter()
    {
        _testRequest.PostalCode = "A1A 1A1A";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Postal code should be in the form L#L #L#."));
    }

    [Test]
    public void Validate_PostalCodeInvalidFormat_MissingCharacter()
    {
        _testRequest.PostalCode = "A1 1A1";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Postal code should be in the form L#L #L#."));
    }

    [Test]
    public void Validate_PostalCodeInvalidFormat_WrongPattern()
    {
        _testRequest.PostalCode = "111 111";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Postal code should be in the form L#L #L#."));
    }

    [Test]
    public void Validate_PostalCodeEmptyString()
    {
        _testRequest.PostalCode = "";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    // Phone tests

    [Test]
    public void Validate_PhoneValidFormats()
    {
        string[]? validPhoneNumbers = new[]
        {
            "+1-555-555-5555",
            "123-456-7890",
            "(123) 456-7890",
            "123 456 7890",
            "1234567890",
            "+1 123 456 7890"
        };

        foreach (string? phoneNumber in validPhoneNumbers)
        {
            _testRequest.Phone = phoneNumber;
            bool isValid = ValidateModel(_testRequest);

            Assert.IsTrue(isValid, $"Failed for phone number: {phoneNumber}");
        }
    }

    [Test]
    public void Validate_PhoneValidFormat_Dashes()
    {
        _testRequest.Phone = "123-456-7890";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_PhoneValidFormat_Parentheses()
    {
        _testRequest.Phone = "(123) 456-7890";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_PhoneValidFormat_WithCountryCode()
    {
        _testRequest.Phone = "+1 123-456-7890";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_PhoneValidFormat_Spaces()
    {
        _testRequest.Phone = "123 456 7890";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_PhoneValidFormat_Compact()
    {
        _testRequest.Phone = "1234567890";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_PhoneInvalidFormat_InvalidCharacters()
    {
        _testRequest.Phone = "123-456-ABCD";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage,
            Is.EqualTo("Please enter a valid phone number (e.g., +1-555-555-5555)."));
    }

    [Test]
    public void Validate_PhoneIsEmptyString()
    {
        _testRequest.Phone = "";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    // Profession Tests
    [Test]
    public void Validate_ProfessionExceedsMaxLength()
    {
        _testRequest.Profession = new string('A', 26);

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Profession must not exceed 25 characters."));
    }

    [Test]
    public void Validate_ProfessionContainsInvalidCharacters()
    {
        _testRequest.Profession = "Therapist123";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage,
            Is.EqualTo("Profession must contain only letters and spaces."));
    }

    [Test]
    public void Validate_Profession_MaxEdgeCase()
    {
        _testRequest.Profession = new string('A', 25);

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_Profession_MinEdgeCase()
    {
        _testRequest.Profession = "A";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_ProfessionContainsSpaces()
    {
        _testRequest.Profession = "Mental Health";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_ProfessionIsEmptyString()
    {
        _testRequest.Profession = "";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    // Major tests
    [Test]
    public void Validate_MajorExceedsMaxLength()
    {
        _testRequest.Major = new string('A', 26);

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Major must not exceed 25 characters."));
    }

    [Test]
    public void Validate_MajorContainsInvalidCharacters()
    {
        _testRequest.Major = "Science123";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage, Is.EqualTo("Major must contain only letters and spaces."));
    }

    [Test]
    public void Validate_Major_MaxEdgeCase()
    {
        _testRequest.Major = new string('A', 25);

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_Major_MinEdgeCase()
    {
        _testRequest.Major = "A";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_MajorContainsSpaces()
    {
        _testRequest.Major = "Computer Science";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_MajorIsEmptyString()
    {
        _testRequest.Major = "";

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    // Years exp tests
    [Test]
    public void Validate_YearsExperienceBelowMinimum()
    {
        _testRequest.YearsExperienceInHippotherapy = -1;

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage,
            Is.EqualTo("Years of experience must be an integer between 0 and 100."));
    }

    [Test]
    public void Validate_YearsExperienceAboveMaximum()
    {
        _testRequest.YearsExperienceInHippotherapy = 101;

        bool isValid = ValidateModel(_testRequest);

        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults.First().ErrorMessage,
            Is.EqualTo("Years of experience must be an integer between 0 and 100."));
    }

    [Test]
    public void Validate_YearsExperience_MinEdgeCase()
    {
        _testRequest.YearsExperienceInHippotherapy = 0;

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_YearsExperience_MaxEdgeCase()
    {
        _testRequest.YearsExperienceInHippotherapy = 100;

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_YearsExperienceValidValue()
    {
        _testRequest.YearsExperienceInHippotherapy = 25;

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }

    [Test]
    public void Validate_YearsExperienceIsNull()
    {
        _testRequest.YearsExperienceInHippotherapy = null;

        bool isValid = ValidateModel(_testRequest);

        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }
}