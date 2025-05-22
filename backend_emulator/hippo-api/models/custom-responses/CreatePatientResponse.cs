namespace HippoApi.Models.custom_responses;

public class CreatePatientResponse
{
    public CreatePatientResponse(string message, string patientId)
    {
        Message = message;
        PatientId = patientId;
    }

    public string Message { get; set; }
    public string PatientId { get; set; }
}