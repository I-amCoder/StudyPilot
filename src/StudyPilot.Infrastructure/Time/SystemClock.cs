using StudyPilot.SharedKernel.Time;

namespace StudyPilot.Infrastructure.Time;

/// <summary>The real clock. Registered as a singleton by the host.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
