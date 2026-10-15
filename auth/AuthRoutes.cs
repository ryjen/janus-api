#nullable enable
namespace Janus;

// Route names are part of the published v1 API contract. Define each literal
// once; the Lambda adapter dispatches on an enum rather than duplicated text.
public enum AuthRoute
{
    Unknown,
    Authenticate,
    NewPasswordChallenge,
    SignUp,
    ConfirmSignUp,
    ForgotPassword,
    ConfirmForgotPassword
}

public static class AuthRoutes
{
    public const string AuthenticatePath = "/authenticate";
    public const string NewPasswordChallengePath = "/reset";
    public const string SignUpPath = "/signup";
    public const string ConfirmSignUpPath = "/confirm";
    public const string ForgotPasswordPath = "/password/forgot";
    public const string ConfirmForgotPasswordPath = "/password/confirm";

    public static AuthRoute Resolve(string? path) => path switch
    {
        AuthenticatePath => AuthRoute.Authenticate,
        NewPasswordChallengePath => AuthRoute.NewPasswordChallenge,
        SignUpPath => AuthRoute.SignUp,
        ConfirmSignUpPath => AuthRoute.ConfirmSignUp,
        ForgotPasswordPath => AuthRoute.ForgotPassword,
        ConfirmForgotPasswordPath => AuthRoute.ConfirmForgotPassword,
        _ => AuthRoute.Unknown
    };
}
