# GritTrack API

The back end for GritTrack. It stores courses in **a JSON file or SQL Server** (you pick), and
every create, update and delete needs a **login token** (you can turn that off for a demo).
The MAUI app only ever talks to this API over HTTP. It never touches SQL Server.

## Endpoints

| Method | URL | Does | Needs login? | Success | Failure |
| --- | --- | --- | --- | --- | --- |
| POST | /api/auth/login | Trade the demo user name and password for a token. | no | 200 | 400, 401, 404 (auth is off) |
| GET | /api/courses | List all courses. Optional `?grade=` filters by letter grade (e.g. `?grade=B+`). | no | 200 | |
| GET | /api/courses/{id} | Get one course. | no | 200 | 404 |
| POST | /api/courses | Add a course. | yes | 201 | 400, 401, 409 |
| PUT | /api/courses/{id} | Replace a course's fields. | yes | 200 | 400, 401, 404, 409 |
| DELETE | /api/courses/{id} | Remove a course. | yes | 204 | 401, 404 |

- 400 = a field broke a rule. The body is a standard ProblemDetails with an `errors` list.
- 401 = no token, a bad token, or an expired token (or a wrong password on the login call).
- 409 = the course code belongs to a different course. Course codes are unique.

## Settings that matter

All in `appsettings.json` unless noted.

| Setting | Values | What it does |
| --- | --- | --- |
| `DataSource` | `Json` or `SqlServer` | Where courses live. Picked once at startup. |
| `Auth:Enabled` | `true` or `false` | `true`: writes need a token. `false`: anyone can write and `/api/auth/login` answers 404. |
| `Auth:DemoUserName` | text (default `demo`) | The one demo account. Not a secret. |
| `Auth:SigningKey` | long random text | **Secret.** Signs the tokens. User Secrets only. |
| `Auth:DemoPassword` | text | **Secret.** The demo account's password. User Secrets only. |
| `MSSQL_SA_PASSWORD` | text | **Secret.** The SQL Server password. `.env` file (or User Secrets). |

You can override any setting for one run with an environment variable, for example
`DataSource=SqlServer` or `Auth__Enabled=false`, without editing a file.

**No secrets are ever committed.** `.env`, `Data/*.json` and `Data/*.tmp` are gitignored.
The signing key and demo password live in `dotnet user-secrets` on your machine only.
If `Auth:Enabled` is `true` and a secret is missing, the app refuses to start and tells you which one.

## From a fresh clone to a running app

### 1. What you need

- .NET 10 SDK
- A container engine that understands `docker compose` (Docker Desktop, or Podman with the docker CLI)
- Node.js, only if you want to run the Postman collection from the command line (newman)
- For the MAUI app: the .NET MAUI workloads (see the root README)

### 2. The SQL Server password (`.env`)

From the repo root (the folder with `compose.yaml`):

```bash
cp .env.example .env
```

Open `.env` and replace the placeholder with your own password: 8 or more characters with upper case,
lower case and a number. Stick to letters and numbers so it is safe inside a connection string.
`.env` is gitignored.

### 3. Start SQL Server

```bash
docker compose up -d
docker compose ps
```

Wait until the container shows **healthy** (about 30 seconds the first time). The data lives in a
named volume (`grittrack-sqldata`), so it survives restarts. To wipe it: `docker compose down -v`.

### 4. The login secrets (User Secrets)

Run these from the `GritTrack.Api` folder. Use your own values. Make the signing key long (32+ characters).

```bash
cd GritTrack.Api
dotnet user-secrets set "Auth:SigningKey" "<long random text, 32+ characters>"
dotnet user-secrets set "Auth:DemoPassword" "<the password you want for the demo user>"
```

User Secrets are only read when `ASPNETCORE_ENVIRONMENT=Development`, which the launch profile already sets.

### 5. Create the tables

Nothing to do: with `DataSource` = `SqlServer` the API applies the migration when it starts (it retries for
about 30 seconds while SQL Server wakes up). To do it by hand instead:

```bash
dotnet tool restore
dotnet tool run dotnet-ef database update --project GritTrack.Api
```

### 6. Run the API

```bash
dotnet run --project GritTrack.Api
```

The first two log lines tell you the mode:

```
Data source: Json
Authentication: ON (writes need a token)
```

It listens on `https://localhost:49227` (and `http://localhost:49228`). The OpenAPI document is at `/openapi/v1.json`.
If your browser or newman complains about the certificate, run `dotnet dev-certs https --trust` once.

### 7. Switch the data source

Edit `"DataSource"` in `appsettings.json` (`"Json"` or `"SqlServer"`) and restart. Or for one run only:

```powershell
$env:DataSource = "SqlServer"; dotnet run --project GritTrack.Api
```

JSON mode keeps its data in `Data/courses.json` (created on first write, gitignored).

### 8. Turn login on or off

`"Auth": { "Enabled": true }` in `appsettings.json`, or for one run: `$env:Auth__Enabled = "false"`.

## Postman

Files in `postman/`:

- `GritTrack.postman_collection.json` the requests and tests
- `GritTrack-AuthOn.postman_environment.json` for runs with login on
- `GritTrack-AuthOff.postman_environment.json` for runs with login off

**In Postman:** import all three files, pick the environment that matches how the API is running, and run the
collection in the Collection Runner. For the Auth ON environment, type the demo password into the
`demoPassword` value (it ships empty on purpose). Every request is green in both modes, with either data source.
The only things that change with the switch: login (404 when auth is off) and writes without a token
(401 when auth is on). The collection creates its own test courses and deletes them at the end.

**From the command line** (newman):

```bash
npm install -g newman
newman run postman/GritTrack.postman_collection.json -e postman/GritTrack-AuthOn.postman_environment.json --env-var "demoPassword=<your demo password>" --insecure
newman run postman/GritTrack.postman_collection.json -e postman/GritTrack-AuthOff.postman_environment.json --insecure
```

`--insecure` is only needed when your machine does not trust the ASP.NET development certificate.

## Run the MAUI app

1. Start the API (step 6). Leave it running.
2. Open `GritTrack.slnx` in Visual Studio, or build from the command line:

   ```bash
   dotnet build GritTrack/GritTrack.csproj -f net10.0-windows10.0.19041.0
   ```

3. Run the app. Browsing works with no login. Try to add, edit or drop a course while signed out and the app
   sends you to the sign in page, then finishes the save after you sign in.

Where the app looks for the API (`GritTrack/Services/Api/ApiSettings.cs`):

| Where the app runs | Address it uses |
| --- | --- |
| Windows, Mac Catalyst, iOS simulator | `https://localhost:49227` |
| Android emulator | `https://10.0.2.2:49227` (the emulator's name for your PC) |
| A real phone | Change `ApiSettings` to your PC's network address |

Debug builds accept the ASP.NET development certificate on those addresses. This follows Microsoft Learn's
"Connect to local web services from Android emulators and iOS simulators".

## Troubleshooting

| Problem | Try this |
| --- | --- |
| `Auth:Enabled is true but Auth:SigningKey is missing...` | Do step 4, and run from a Development environment (the launch profile does that). Or set `Auth:Enabled` to `false`. |
| `DataSource is SqlServer but MSSQL_SA_PASSWORD is not set` | Do step 2. The API looks for `.env` in the current folder and every parent folder. |
| `docker compose up` says `Copy .env.example to .env` | Do step 2 first. |
| Container never becomes healthy | `docker compose logs sqlserver`. The password probably breaks SQL Server's rules (too short, no number). After fixing `.env`, run `docker compose down -v` and start again. |
| API logs `Database not ready yet` | Normal for a few seconds. If it never goes away, check `docker compose ps` and that nothing else is using port 1433. |
| Login works in Postman but the app says it cannot reach the server | The API is not running, or the app is on an Android emulator and `ApiSettings` points at `localhost`. |
| Certificate errors | `dotnet dev-certs https --trust`, or add `--insecure` to newman. |
| App shows "Sign in" forever on a save | You backed out of the sign in page. Save again and sign in. |
| Port 49227 is taken | Change `applicationUrl` in `Properties/launchSettings.json`, `ApiPort` in `ApiSettings.cs`, and `baseUrl` in the Postman environments. |

## Demo script

1. **Postman, auth OFF.** Start the API with `Auth__Enabled=false`. Run the collection with the Auth OFF
   environment: all green. Point out that writes without a token succeed and login answers 404.
2. **Postman, auth ON.** Restart with auth on. Run the Auth ON environment: all green. Point out the 401s on
   writes without a token, then the token being saved and used.
3. **Switch Json to SqlServer.** Change `DataSource` (or the env var), restart, show the startup log line
   saying `Data source: SqlServer`, run the collection again: same requests, still green.
4. **Show the container is healthy.** `docker compose ps`.
5. **Startup log lines.** Show `Data source: ...` and `Authentication: ...` at the top of the console.
6. **App: browse, then write.** Open the app signed out and browse the list. Tap `+ Add`, fill the form, Save:
   the app sends you to the sign in page.
7. **Login, then create / edit / drop.** Sign in. The save finishes by itself. Edit it, then drop it. Show the
   rows in SQL Server, and the create / update / delete log lines (id and code only, never a password or token).
8. **One validation error.** Add a course with the course code left blank (or a duplicate code) and show the
   message that came back from the API.
9. **30 second bug story.** Pick one real bug you hit, say what it looked like, what the cause was, and the
   one line that fixed it. (Ideas from this project: Docker Desktop would not start because Podman held the
   docker pipe; SQL Server answered on `127.0.0.1` but not on `localhost` because `localhost` resolved to IPv6.)
