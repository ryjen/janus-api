namespace Janus;

public static class AccessPolicy
{
    // An absent client ID selects the subject's own record, never an arbitrary record.
    public static bool MatchesSubject(string? subject, string? clientId) =>
        !string.IsNullOrWhiteSpace(subject) &&
        (string.IsNullOrWhiteSpace(clientId) || string.Equals(subject, clientId, StringComparison.Ordinal));
}
