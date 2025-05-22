using System.ComponentModel.DataAnnotations;
using HippoApi.Models;

namespace tests;

[TestFixture]
public class PatientEvaluationModelTests
{
    [SetUp]
    public void Setup()
    {
        _validationResults = new List<ValidationResult>();
        testItem1 = new PatientEvaluation
        {
            SessionID = "session1",
            HeadAnt = 0,
            HeadLat = 1,
            KneeFlex = -1,
            Pelvic = 2,
            PelvicTilt = -2,
            Thoracic = 0,
            Trunk = 1,
            TrunkInclination = -1,
            ElbowExtension = 2
        };
        testItem2 = new PatientEvaluation
        {
            SessionID = "session1",
            HeadAnt = 1,
            HeadLat = -1,
            KneeFlex = 2,
            Pelvic = -2,
            PelvicTilt = 0,
            Thoracic = 1,
            Trunk = -1,
            TrunkInclination = 2,
            ElbowExtension = -2
        };
    }

    private ValidationContext _validationContext;
    private List<ValidationResult> _validationResults;
    private PatientEvaluation testItem1, testItem2;

    private bool ValidateModel(PatientEvaluation model)
    {
        _validationContext = new ValidationContext(model, null, null);
        return Validator.TryValidateObject(model, _validationContext, _validationResults, true);
    }

    [Test]
    public void TestValidPatientEvaluationModel()
    {
        bool isValid = ValidateModel(testItem1);
        Assert.IsTrue(isValid);
        Assert.IsEmpty(_validationResults);
    }


    // test old and dont matter
    [Test]
    public void TestInvalidPatientEvaluationModel_InvalidSessionID()
    {
        testItem1.SessionID = null;
        bool isValid = ValidateModel(testItem1);
        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults[0].ErrorMessage, Is.EqualTo("Session ID is required"));
    }

    [Test]
    public void TestInvalidPatientEvaluationModel_InvalidHeadLat()
    {
        testItem1.HeadLat = 3; // Invalid HeadLat
        bool isValid = ValidateModel(testItem1);
        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults[0].ErrorMessage, Is.EqualTo("HeadLat must be between -2 and 2"));
    }

    [Test]
    public void TestInvalidPatientEvaluationModel_InvalidKneeFlex()
    {
        testItem1.KneeFlex = 3; // Invalid KneeFlex
        bool isValid = ValidateModel(testItem1);
        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults[0].ErrorMessage, Is.EqualTo("KneeFlex must be between -2 and 2"));
    }

    [Test]
    public void TestInvalidPatientEvaluationModel_InvalidPelvic()
    {
        testItem1.Pelvic = 3; // Invalid Pelvic
        bool isValid = ValidateModel(testItem1);
        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults[0].ErrorMessage, Is.EqualTo("Pelvic must be between -2 and 2"));
    }

    [Test]
    public void TestInvalidPatientEvaluationModel_InvalidPelvicTilt()
    {
        testItem1.PelvicTilt = 3; // Invalid PelvicTilt
        bool isValid = ValidateModel(testItem1);
        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults[0].ErrorMessage, Is.EqualTo("PelvicTilt must be between -2 and 2"));
    }

    [Test]
    public void TestInvalidPatientEvaluationModel_InvalidThoracic()
    {
        testItem1.Thoracic = 3; // Invalid Thoracic
        bool isValid = ValidateModel(testItem1);
        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults[0].ErrorMessage, Is.EqualTo("Thoracic must be between -2 and 2"));
    }

    [Test]
    public void TestInvalidPatientEvaluationModel_InvalidTrunk()
    {
        testItem1.Trunk = 3; // Invalid Trunk
        bool isValid = ValidateModel(testItem1);
        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults[0].ErrorMessage, Is.EqualTo("Trunk must be between -2 and 2"));
    }

    [Test]
    public void TestInvalidPatientEvaluationModel_InvalidTrunkInclination()
    {
        testItem1.TrunkInclination = 3; // Invalid TrunkInclination
        bool isValid = ValidateModel(testItem1);
        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults[0].ErrorMessage, Is.EqualTo("TrunkInclination must be between -2 and 2"));
    }

    [Test]
    public void TestInvalidPatientEvaluationModel_InvalidElbowExtension()
    {
        testItem1.ElbowExtension = 3; // Invalid ElbowExtension
        bool isValid = ValidateModel(testItem1);
        Assert.IsFalse(isValid);
        Assert.IsNotEmpty(_validationResults);
        Assert.That(_validationResults[0].ErrorMessage, Is.EqualTo("ElbowExtension must be between -2 and 2"));
    }
}