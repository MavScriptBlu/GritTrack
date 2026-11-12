# Testing

Two test projects, both plain xUnit. Run everything from the repo folder:

```bash
dotnet test GritTrack.Api.Tests/GritTrack.Api.Tests.csproj
dotnet test GritTrack.App.Tests/GritTrack.App.Tests.csproj
```

`dotnet test GritTrack.slnx` runs both as well, but it also builds the MAUI app, so it needs the MAUI workload installed. The two commands above don't need MAUI at all.

## GritTrack.Api.Tests

Tests for the API and for the files around it.

| Folder / file | What it checks |
| --- | --- |
| `CoursesApiTests.cs` | The courses and login endpoints, boot the real API in memory. |
| `PortSettingsTests.cs` | The API port is the same in launchSettings, the app, and the Postman files. |
| `Sql/SqlConnectionStringFactoryTests.cs` | The SQL connection string: defaults, overrides, encryption, and the clear error when the password is missing. |
| `Sql/DotEnvLoaderTests.cs` | The `.env` loader: finds parent folders, skips comments, strips quotes, never beats a real environment variable. |
| `Sql/DatabaseMigrationRunnerTests.cs` | Startup migration retries: how many tries, the wait between them, the warning text, and that the real error shows up on the last try. Nothing sleeps for real. |
| `Sql/MigrationTests.cs` | The `InitialCreate` migration and the model, with no database at all. Fails if you change `Course` and forget to add a migration. |
| `Sql/SqlServerIntegrationTests.cs` | The real `SqlCourseRepository` and the real migrations against a real SQL Server. |
| `Postman/PostmanFilesTests.cs` | The collection and both environments: valid JSON, no undefined `{{variables}}`, request names that match the status the script checks, no secrets. |
| `Postman/PostmanInstructionFilesTests.cs` | The four files in `.github/instructions`: front matter, sections, and example scripts that follow their own rules. |

### The real SQL Server tests

`SqlServerIntegrationTests` uses **SQL Server LocalDB** (it comes with Visual Studio). Each run makes a throwaway database named `GritTrackTests_<random>`, migrates it, runs the tests, and deletes it. It uses your Windows login, so no password and no `.env` is needed, and it never touches the `GritTrack` database.

No LocalDB on the machine (Mac, Linux, or a Windows box without it)? Those tests show up as **skipped**, not failed, and everything else still runs.

The Docker SQL Server from `compose.yaml` is not used by the tests.

## GritTrack.App.Tests

Tests for the MAUI app's non-visual code: sign in, the bearer token handler, reading API responses, the courses API client, and saving imported assignments on the phone.

The MAUI app only builds for Android and Windows, so a test project can't reference it. Instead `GritTrack.App.Tests.csproj` compiles the app's real source files straight into the test assembly (the `<Compile Include="..\GritTrack\...">` lines). No copies, so the tests always run the code that ships.

`SecureStorage` and `FileSystem.AppDataDirectory` only exist inside a running MAUI app. `Support/MauiStandIns.cs` has same-named stand-ins, and each test gets a fresh fake storage and an empty temporary app data folder (`PhoneStaticsTestBase`).

| File | What it checks |
| --- | --- |
| `AuthServiceTests.cs` | Sign in and sign out, saving and restoring the token, expired tokens, and `RunWriteAsync` (a 401 opens the login page, then the same save runs again). |
| `BearerTokenHandlerTests.cs` | Writes get the bearer token, reads never do. |
| `ApiResponseReaderTests.cs` | Every status turned into the right exception and message, and bad or empty bodies. |
| `ApiCourseRepositoryTests.cs` | What the app sends to `/api/courses` and how it reads the answer, plus the phone-only assignments. |
| `LocalAssignmentStoreTests.cs` | `local-assignments.json`: saving, surviving a restart, duplicates, deleted courses, and a broken file. |

### Adding another app file to the tests

If a new file in `GritTrack/` doesn't need the phone, add one `<Compile Include=... Link=... />` line to `GritTrack.App.Tests.csproj`. If it uses another MAUI API, add a same-shaped stand-in to `Support/MauiStandIns.cs`.

### What isn't tested

Pages, page models, and anything that needs a screen or Shell are not covered here. Those still need a run on an emulator or Windows.
