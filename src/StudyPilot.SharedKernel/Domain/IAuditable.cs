namespace StudyPilot.SharedKernel.Domain;

/// <summary>
/// An entity whose creation and last modification are recorded. Timestamps are stamped by the
/// persistence layer so callers cannot forget — or forge — them.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedAtUtc { get; set; }

    DateTimeOffset? UpdatedAtUtc { get; set; }
}
