using Janus;
using Xunit;

namespace Janus.Tests;

public sealed class AuthRoutesTests
{
    [Theory]
    [InlineData(AuthRoutes.AuthenticatePath, AuthRoute.Authenticate)]
    [InlineData(AuthRoutes.NewPasswordChallengePath, AuthRoute.NewPasswordChallenge)]
    [InlineData(AuthRoutes.SignUpPath, AuthRoute.SignUp)]
    [InlineData(AuthRoutes.ConfirmSignUpPath, AuthRoute.ConfirmSignUp)]
    [InlineData(AuthRoutes.ForgotPasswordPath, AuthRoute.ForgotPassword)]
    [InlineData(AuthRoutes.ConfirmForgotPasswordPath, AuthRoute.ConfirmForgotPassword)]
    public void KnownPathsHaveOneTypedRoute(string path, AuthRoute expected)
    {
        Assert.Equal(expected, AuthRoutes.Resolve(path));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/unknown")]
    [InlineData("/Signup")]
    [InlineData("/signup/")]
    [InlineData("/password/forgot/")]
    public void UnknownPathsFailClosed(string? path)
    {
        Assert.Equal(AuthRoute.Unknown, AuthRoutes.Resolve(path));
    }
}
