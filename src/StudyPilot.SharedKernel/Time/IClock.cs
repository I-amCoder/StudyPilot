namespace StudyPilot.SharedKernel.Time;

/// <summary>Abstracts the system clock so time-dependent behaviour stays testable.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
