using Janus;
using Xunit;

namespace Janus.Tests;

public class AccessPolicyTests
{
    [Theory]
    [InlineData("subject-123", null)]
    [InlineData("subject-123", "subject-123")]
    public void PermitsOnlyOwnRecord(string subject, string? requestedId)
    {
        Assert.True(AccessPolicy.MatchesSubject(subject, requestedId));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("subject-123", "subject-456")]
    [InlineData("subject-123", "SUBJECT-123")]
    public void DeniesMissingIdentityOrCrossUserRecord(string? subject, string? requestedId)
    {
        Assert.False(AccessPolicy.MatchesSubject(subject, requestedId));
    }
}
