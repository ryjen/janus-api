#nullable enable

namespace Janus;

// Aggregate-only observations. Do not add caller-controlled values, identifiers,
// raw paths, tokens, sessions, credentials, exception data or free-form labels.
// AuthRoute and AuthObservationKind together form a finite-cardinality taxonomy.
public enum AuthObservationKind
{
    Succeeded,
    Accepted,
    InvalidInput,
    DeniedOrChallenged,
    Unavailable,
    Failed
}

public readonly record struct AuthObservation(AuthRoute Route, AuthObservationKind Kind);

public interface IAuthObservationSink
{
    void Record(AuthObservation observation);
}

// Zero telemetry by default. An explicitly injected implementation may count
// observations, but no production logging or metrics backend is configured.
public sealed class NoOpAuthObservationSink : IAuthObservationSink
{
    public static NoOpAuthObservationSink Instance { get; } = new();

    private NoOpAuthObservationSink() { }

    public void Record(AuthObservation observation) { }
}
