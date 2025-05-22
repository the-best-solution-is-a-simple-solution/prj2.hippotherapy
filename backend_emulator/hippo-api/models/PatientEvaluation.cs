using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace HippoApi.Models;

[FirestoreData]
public class PatientEvaluation
{
    [FirestoreDocumentId] public string EvaluationID { get; set; }

    [FirestoreProperty]
    [Required(ErrorMessage = "Session ID is required")]
    public string SessionID { get; set; }

    [FirestoreProperty]
    [RegularExpression("^(pre|post)$", ErrorMessage = "EvalType must be either 'Pre' or 'Post'")]
    public string EvalType { get; set; }

    [FirestoreProperty] 
    public bool Exclude { get; set; }
    
    [FirestoreProperty]
    [MaxLength(2048)]
    public string? Notes { get; set; }

    [FirestoreProperty]
    [Range(-2, 2, ErrorMessage = "Lumbar must be between -2 and 2")]
    public long Lumbar { get; set; }

    [FirestoreProperty]
    [Range(-2, 2, ErrorMessage = "HipFlex must be between -2 and 2")]
    public long HipFlex { get; set; }

    [FirestoreProperty]
    [Range(-2, 2, ErrorMessage = "HeadAnt must be between -2 and 2")]
    public long HeadAnt { get; set; }

    [FirestoreProperty]
    [Range(-2, 2, ErrorMessage = "HeadLat must be between -2 and 2")]
    public long HeadLat { get; set; }

    [FirestoreProperty]
    [Range(-2, 2, ErrorMessage = "KneeFlex must be between -2 and 2")]
    public long KneeFlex { get; set; }

    [FirestoreProperty]
    [Range(-2, 2, ErrorMessage = "Pelvic must be between -2 and 2")]
    public long Pelvic { get; set; }

    [FirestoreProperty]
    [Range(-2, 2, ErrorMessage = "PelvicTilt must be between -2 and 2")]
    public long PelvicTilt { get; set; }

    [FirestoreProperty]
    [Range(-2, 2, ErrorMessage = "Thoracic must be between -2 and 2")]
    public long Thoracic { get; set; }

    [FirestoreProperty]
    [Range(-2, 2, ErrorMessage = "Trunk must be between -2 and 2")]
    public long Trunk { get; set; }

    [FirestoreProperty]
    [Range(-2, 2, ErrorMessage = "TrunkInclination must be between -2 and 2")]
    public long TrunkInclination { get; set; }

    [FirestoreProperty]
    [Range(-2, 2, ErrorMessage = "ElbowExtension must be between -2 and 2")]
    public long ElbowExtension { get; set; }
}