// The app calls SecureStorage and FileSystem, which only exist inside a running MAUI app.
// These stand ins have the same names and the same shape for the parts the app uses, so the
// app's real AuthService and LocalAssignmentStore compile and run here unchanged.
// (if the app starts using another member of these, this file is where to add it)
namespace Microsoft.Maui.Storage;

public interface ISecureStorage
{
    Task<string?> GetAsync(string key);

    Task SetAsync(string key, string value);

    bool Remove(string key);
}

public static class SecureStorage
{
    // the real one is get only. the setter is how a test plugs in its own fake
    public static ISecureStorage Default { get; set; } = new GritTrack.App.Tests.Support.FakeSecureStorage();
}

public static class FileSystem
{
    // the real one is the app's private data folder. tests point it at a throwaway folder
    public static string AppDataDirectory { get; set; } = Path.GetTempPath();
}
