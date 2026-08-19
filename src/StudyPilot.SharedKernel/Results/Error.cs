namespace StudyPilot.SharedKernel.Results;

/// <summary>A machine-readable failure reason. <paramref name="Code"/> is stable; the message is not.</summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}
