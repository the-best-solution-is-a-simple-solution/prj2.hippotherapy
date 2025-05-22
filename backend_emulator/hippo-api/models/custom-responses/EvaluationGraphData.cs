namespace HippoApi.Models.custom_responses;

/// <summary>
///     A copy of PatientEvaluation but with extra data needed for the graph. (e.g Date).
/// </summary>
public class EvaluationGraphData
{
    private DateTime _dateTaken;

    public EvaluationGraphData(PatientEvaluation evaluation, DateTime date)
    {
        DateTaken = date;
        // add data from evaluation
        EvaluationID = evaluation.EvaluationID;
        SessionID = evaluation.SessionID;
        EvalType = evaluation.EvalType;
        Notes = evaluation.Notes;
        Exclude = evaluation.Exclude;
        Lumbar = evaluation.Lumbar;
        HipFlex = evaluation.HipFlex;
        HeadAnt = evaluation.HeadAnt;
        HeadLat = evaluation.HeadLat;
        KneeFlex = evaluation.KneeFlex;
        Pelvic = evaluation.Pelvic;
        PelvicTilt = evaluation.PelvicTilt;
        Thoracic = evaluation.Thoracic;
        Trunk = evaluation.Trunk;
        TrunkInclination = evaluation.TrunkInclination;
        ElbowExtension = evaluation.ElbowExtension;
    }

    public DateTime DateTaken
    {
        get => _dateTaken;
        set => _dateTaken = DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public string EvaluationID { get; set; }


    public string SessionID { get; set; }


    public string EvalType { get; set; }
    
    public string? Notes { get; set; }
    
    public bool Exclude { get; set; }

    public long Lumbar { get; set; }


    public long HipFlex { get; set; }


    public long HeadAnt { get; set; }


    public long HeadLat { get; set; }


    public long KneeFlex { get; set; }


    public long Pelvic { get; set; }


    public long PelvicTilt { get; set; }


    public long Thoracic { get; set; }


    public long Trunk { get; set; }


    public long TrunkInclination { get; set; }


    public long ElbowExtension { get; set; }
}