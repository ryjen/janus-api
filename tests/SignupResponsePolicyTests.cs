using Janus;
using Xunit;

namespace Janus.Tests;

public sealed class SignupResponsePolicyTests
{
    [Fact]
    public void AcknowledgementHasOneNonDisclosingShape()
    {
        var first = SignupResponsePolicy.Acknowledged();
        var repeated = SignupResponsePolicy.Acknowledged();
        Assert.Equal(200, first.StatusCode);
        Assert.Equal(first.StatusCode, repeated.StatusCode);
        Assert.Equal(first.Body, repeated.Body);
        Assert.DoesNotContain("already", first.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exists", first.Body, StringComparison.OrdinalIgnoreCase);
    }
}
