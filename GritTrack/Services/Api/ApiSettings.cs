namespace GritTrack.Services.Api
{
    // where the GritTrack API lives. the app only ever talks to this address,
    // never to SQL Server directly.
    //
    // local web services need a different address on each kind of device
    // (Microsoft Learn: "Connect to local web services from Android emulators
    // and iOS simulators"):
    //   - the Android emulator can't see the PC's "localhost". it has its own
    //     alias for the PC, 10.0.2.2
    //   - Windows (and the iOS simulator) just use localhost
    // a real phone needs the PC's network address instead of either one
    public static class ApiSettings
    {
        // the https port from GritTrack.Api/Properties/launchSettings.json
        private const int ApiPort = 49227;

        public static string BaseAddress =>
            DeviceInfo.Platform == DevicePlatform.Android
                ? $"https://10.0.2.2:{ApiPort}/"
                : $"https://localhost:{ApiPort}/";

        // how long to wait on the server before giving up and showing "try again"
        public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);
    }
}
