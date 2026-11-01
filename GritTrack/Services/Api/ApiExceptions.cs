namespace GritTrack.Services.Api
{
    // the ways talking to the API can go wrong. each one carries a message that
    // is safe to show on screen as is, so a PageModel never has to look at
    // HTTP status codes itself

    // the server couldn't be reached, timed out, or answered with an error we
    // can't do anything about. "Try Again" is the right response
    public class ApiRequestException : Exception
    {
        public ApiRequestException(string message) : base(message) { }
    }

    // the server said the data was no good: 400 (a field rule broke) or 409
    // (that course code is taken). Messages is one friendly line per problem
    public class ApiValidationException : ApiRequestException
    {
        public ApiValidationException(IReadOnlyList<string> messages)
            : base(string.Join(Environment.NewLine, messages))
        {
            Messages = messages;
        }

        public IReadOnlyList<string> Messages { get; }
    }

    // the server answered 401: a write needs a login (or the token ran out)
    public class AuthenticationRequiredException : Exception
    {
        public AuthenticationRequiredException() : base("Sign in to make changes.") { }
    }

    // the login page was closed without signing in
    public class LoginCancelledException : Exception
    {
        public LoginCancelledException() : base("Sign in is needed to save changes.") { }
    }
}
