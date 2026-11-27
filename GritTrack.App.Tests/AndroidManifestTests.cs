using System.Xml.Linq;

namespace GritTrack.App.Tests;

// /* ---------- AndroidManifest.xml ---------- */
// the camera, the gallery and the reminders only work on a phone if the manifest asks for the right stuff.
// nobody sees a missing permission until the video, so these read the real file and check it
public class AndroidManifestTests
{
    private static readonly XNamespace AndroidNamespace = "http://schemas.android.com/apk/res/android";

    // walks up from the test's bin folder until it finds the solution, same trick the API tests use
    private static string FindManifestPath()
    {
        DirectoryInfo? folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "GritTrack.slnx")))
            folder = folder.Parent;

        if (folder is null)
            throw new InvalidOperationException("Couldn't find GritTrack.slnx above the test folder.");

        return Path.Combine(folder.FullName, "GritTrack", "Platforms", "Android", "AndroidManifest.xml");
    }

    // every <uses-permission> in the file, keyed by name, so a test can look at its maxSdkVersion too
    private static Dictionary<string, XElement> ReadPermissions()
    {
        XDocument manifest = XDocument.Load(FindManifestPath());

        return manifest.Root!
            .Elements("uses-permission")
            .ToDictionary(permission => (string)permission.Attribute(AndroidNamespace + "name")!, permission => permission);
    }

    // /* ---------- the stuff every feature needs ---------- */

    [Theory]
    [InlineData("android.permission.INTERNET")]
    [InlineData("android.permission.ACCESS_NETWORK_STATE")]
    [InlineData("android.permission.CAMERA")]
    [InlineData("android.permission.READ_MEDIA_IMAGES")]
    [InlineData("android.permission.POST_NOTIFICATIONS")]
    [InlineData("android.permission.RECEIVE_BOOT_COMPLETED")]
    public void Manifest_AsksForThePermission(string permissionName)
    {
        Dictionary<string, XElement> permissions = ReadPermissions();

        Assert.True(permissions.ContainsKey(permissionName), $"{permissionName} is missing from AndroidManifest.xml");
    }

    // /* ---------- old phones (Android 12 and lower) ---------- */

    [Theory]
    [InlineData("android.permission.READ_EXTERNAL_STORAGE")]
    [InlineData("android.permission.WRITE_EXTERNAL_STORAGE")]
    public void OldStoragePermissions_AreThere_ButStopAtAndroid12(string permissionName)
    {
        Dictionary<string, XElement> permissions = ReadPermissions();

        Assert.True(permissions.ContainsKey(permissionName), $"{permissionName} is missing from AndroidManifest.xml");
        // 32 is Android 12L. past that the phone uses READ_MEDIA_IMAGES instead, so these shouldn't show up
        Assert.Equal("32", (string?)permissions[permissionName].Attribute(AndroidNamespace + "maxSdkVersion"));
    }

    // /* ---------- the camera stays optional ---------- */

    [Fact]
    public void Camera_IsNotRequired_SoPhonesWithNoCameraCanStillInstall()
    {
        XDocument manifest = XDocument.Load(FindManifestPath());
        XElement cameraFeature = manifest.Root!
            .Elements("uses-feature")
            .Single(feature => (string?)feature.Attribute(AndroidNamespace + "name") == "android.hardware.camera");

        Assert.Equal("false", (string?)cameraFeature.Attribute(AndroidNamespace + "required"));
    }

    [Fact]
    public void Manifest_CanSeeTheCameraApp()
    {
        // Android 11+ hides other apps unless you say which ones you need. without this, Take Photo finds no camera
        XDocument manifest = XDocument.Load(FindManifestPath());
        List<string> queriedActions = manifest.Root!
            .Elements("queries")
            .Elements("intent")
            .Elements("action")
            .Select(action => (string)action.Attribute(AndroidNamespace + "name")!)
            .ToList();

        Assert.Contains("android.media.action.IMAGE_CAPTURE", queriedActions);
    }
}
