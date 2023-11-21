namespace Janus;

record struct Account
{
    public string Id;
    public string Username;
    public string Password;
    public string Email;
}

record struct User
{
    public string Id;
    public string AccountId;
}

record struct Session
{
    public string Token;
}
