namespace HippoApi.controllers;

public class Utils
{
    public static List<String> GetEvaluationTagList()
    {
        return new List<String>
        {
            "sick", "tired", "injured", "unwell", "uncooperative", "weather", "pain", "medication", "seizure", "other"
        };
    }
}