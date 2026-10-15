#nullable enable

namespace Janus;

// Finite, aggregate-only dimensions. Never add identifiers, credentials,
// tokens, sessions, HTTP bodies, exception data or caller-controlled labels.
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

// A sealed, bounded in-memory counter is the only opt-in implementation.
// No arbitrary callbacks, virtual sinks, I/O, logging or task scheduling can
// execute while the authentication response is being returned.
public sealed class AuthObservationCounter
{
    private static readonly int RouteCount = Enum.GetValues<AuthRoute>().Length;
    private static readonly int KindCount = Enum.GetValues<AuthObservationKind>().Length;

    private readonly long[] _counts = new long[RouteCount * KindCount];
    private long _totalCount;

    public void Record(AuthObservation observation)
    {
        var route = (int)observation.Route;
        var kind = (int)observation.Kind;
        if ((uint)route >= (uint)RouteCount || (uint)kind >= (uint)KindCount)
            return;

        Interlocked.Increment(ref _counts[route * KindCount + kind]);
        Interlocked.Increment(ref _totalCount);
    }

    public long Count(AuthRoute route, AuthObservationKind kind)
    {
        var routeIndex = (int)route;
        var kindIndex = (int)kind;
        if ((uint)routeIndex >= (uint)RouteCount || (uint)kindIndex >= (uint)KindCount)
            return 0;

        return Interlocked.Read(ref _counts[routeIndex * KindCount + kindIndex]);
    }

    public long TotalCount => Interlocked.Read(ref _totalCount);
}
