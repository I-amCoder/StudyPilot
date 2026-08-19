namespace StudyPilot.SharedKernel.Domain;

/// <summary>
/// Marks the entry point of an aggregate. Only aggregate roots may be loaded or persisted
/// directly by a repository; everything inside the boundary is reached through the root.
/// </summary>
public interface IAggregateRoot;
