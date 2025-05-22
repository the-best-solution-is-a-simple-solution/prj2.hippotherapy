using System.ComponentModel;

namespace HippoApi.Models.enums;

/// <summary>
///     Valid account roles for the app
/// </summary>
public enum AccountRole
{
    [Description("owner")] Owner,

    [Description("therapist")] Therapist,
    
    [Description("guest")] Guest,
}