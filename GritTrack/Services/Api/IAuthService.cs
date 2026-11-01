namespace GritTrack.Services.Api
{
    // everything about being logged in. PageModels use this instead of
    // touching tokens, storage or the login page themselves
    public interface IAuthService
    {
        // true while there's a token that hasn't run out
        bool IsSignedIn { get; }

        // who is signed in, or an empty string when nobody is
        string UserName { get; }

        // raised whenever someone signs in or out, so screens can update
        event EventHandler? SignInChanged;

        // loads a saved login from secure storage. call once at startup
        Task RestoreAsync();

        // the token to send with writes, or null when signed out or expired
        Task<string?> GetTokenAsync();

        // asks the API to log in. throws ApiValidationException for a wrong
        // user name or password, ApiRequestException if the server can't be reached
        Task SignInAsync(string userName, string password);

        Task SignOutAsync();

        // opens the login page and waits. true = signed in, false = they backed out
        Task<bool> RequestLoginAsync();

        // the login page calls this when it is done, so the waiting write can go on
        void CompleteLogin(bool signedIn);

        // runs a write. if the server says "log in first" it opens the login
        // page, and once they sign in, runs the same write again. so a
        // logged-out save just continues after login
        Task<T> RunWriteAsync<T>(Func<Task<T>> write);
    }
}
