namespace StudyPilot.SharedKernel.Security;

/// <summary>Claim types StudyPilot issues beyond the registered JWT set.</summary>
public static class StudyPilotClaimTypes
{
    /// <summary>Snapshot of the account's security stamp when the token was issued.</summary>
    public const string SecurityStamp = "studypilot:security_stamp";
}
