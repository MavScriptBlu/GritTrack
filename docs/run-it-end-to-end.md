# Run it end to end (Windows, Podman, Android emulator)

From a fresh clone to the API, the database, the Windows app, and the Android emulator all talking to each other.
Written on a brand new Windows machine, so it assumes nothing is installed yet beyond the .NET SDK and the MAUI workloads.
Everything is PowerShell, run from the repo folder (`GritTrack`). For the API settings behind it, see
[GritTrack.Api/README.md](../GritTrack.Api/README.md).

## 0. Get on the right code

```powershell
git switch "i'm-lostr-in-the-sauce"
```

## 1. Start the database (Podman)

```powershell
podman machine init     # only the first time
podman machine start    # every time after a reboot
Copy-Item .env.example .env
notepad .env            # set your own MSSQL_SA_PASSWORD: 8+ chars, upper, lower, number, letters and numbers only
podman compose up -d
podman compose ps
```

Wait until `compose ps` says **healthy** (about 30 seconds the first time). `.env` is gitignored, never commit it.
If `podman compose` says it has no provider, install one (`pip install podman-compose`) or use Docker Desktop.

## 2. Login secrets (once per machine)

User Secrets live on your PC, not in git. Pick your own values. Nothing else depends on them: the key only signs tokens,
and the demo password is whatever you type at the login page.

```powershell
cd GritTrack.Api
dotnet user-secrets set "Auth:SigningKey" ([Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]]))
dotnet user-secrets set "Auth:DemoPassword" "<your own demo password>"
dotnet dev-certs https --trust
cd ..
```

Change the secrets later and old login tokens stop working. You just sign in again.

## 3. Run the API (terminal A, leave it running)

```powershell
dotnet run --project GritTrack.Api
```

The first log lines tell you the mode: `Data source: Json` and `Auth: ON`. It listens on `https://localhost:5227`.

| Want | Run |
| --- | --- |
| SQL Server instead of the JSON file | `$env:DataSource="SqlServer"; dotnet run --project GritTrack.Api` (log shows `Data source: SqlServer`) |
| Login off, for the Postman Auth OFF run | `$env:Auth__Enabled="false"; dotnet run --project GritTrack.Api` |

Environment variables only last for that terminal window.

## 4. Run the Windows app (terminal B)

```powershell
dotnet run --project GritTrack/GritTrack.csproj -f net10.0-windows10.0.19041.0
```

The first build takes a few minutes. Browsing works signed out. Add, edit or delete sends you to the sign in page
(user `demo`, your demo password), then finishes the save.

## 5. Run the app on the Android emulator

No emulator or Android SDK needed for the Windows app. For Android:

1. Start your emulator (Android Studio's Device Manager, or Visual Studio).
2. `adb` usually isn't on the PATH, so call it by full path and check the emulator shows as `device`:

   ```powershell
   & "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" devices
   ```

   If it doesn't list your emulator, try the Visual Studio copy at `C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe`.
   Whichever one lists it is the SDK to use below.
3. First time only, install the Android platform the build asks for (XA5207 means API 36 is missing):

   ```powershell
   dotnet build GritTrack/GritTrack.csproj -t:InstallAndroidDependencies -f net10.0-android "-p:AndroidSdkDirectory=$env:LOCALAPPDATA\Android\Sdk" -p:AcceptAndroidSDKLicenses=True
   ```

4. Build and run it on the emulator (the API keeps running in terminal A):

   ```powershell
   dotnet build GritTrack/GritTrack.csproj -f net10.0-android -t:Run "-p:AndroidSdkDirectory=$env:LOCALAPPDATA\Android\Sdk"
   ```

That puts the app on the emulator instead of the Windows window, so close the Windows app if it's still open and
use the emulator for the demo: browse the list, then add and sign in (user `demo`, your demo password).

The emulator can't see your PC's `localhost`, so the app uses `https://10.0.2.2:5227` on Android (see `ApiSettings.cs`).
No code changes are needed, just keep the API running in terminal A.
Keep the quotes around every `-p:` argument. PowerShell needs them.

In VS Code you can use the **.NET MAUI** extension (needs C# Dev Kit): open the folder, pick the emulator in the
status-bar device picker, press F5.

## 6. Postman

Import all three files from `GritTrack.Api/postman`, pick the environment that matches how the API is running,
and run the collection in the Runner. For Auth ON, type your demo password into the `demoPassword` value in Postman.
**Don't save it into the file in git.** It should pass twice in a row.

## 7. Run the tests

```powershell
dotnet test GritTrack.Api.Tests/GritTrack.Api.Tests.csproj
dotnet test GritTrack.App.Tests/GritTrack.App.Tests.csproj
```

See [testing.md](testing.md).

## When something breaks

| You see | Do this |
| --- | --- |
| `Auth:Enabled is true but Auth:SigningKey is missing` | Step 2, run in the same Windows user session. Or start with `Auth__Enabled=false`. |
| `podman compose` has no provider | `pip install podman-compose`, or use Docker Desktop. |
| `adb` is not recognized | Use the full path in step 5. |
| `XA5207 Could not find android.jar for API level 36` | Run the install command in step 5. |
| Emulator shows an empty list or "can't reach the server" | The API isn't running, or the app is pointed at `localhost`. Start the API (step 3) and check `ApiSettings.cs` uses `10.0.2.2` on Android. |
| Typing, Open cards or Set starting GPA are dead on Windows | Fixed in `BluOSBottomSheet` (see [CHANGELOG.md](CHANGELOG.md), 2026-11-12). Pull the latest. |
| Certificate errors | `dotnet dev-certs https --trust` |
