# Project 4: secure API + MAUI client

How the pieces fit. For step-by-step setup see [GritTrack.Api/README.md](../GritTrack.Api/README.md).

```
MAUI app  --HTTPS + Bearer token-->  GritTrack.Api  --ICourseRepository-->  JSON file   (DataSource = Json)
                                                                        \->  SQL Server  (DataSource = SqlServer, in a container)
```

## Data source switch
`Program.cs` reads `DataSource` and registers one `ICourseRepository`:
`JsonCourseRepository` (singleton, one file) or `SqlCourseRepository` (scoped, on `GritTrackDbContext`).
Controller and `CourseService` don't know which one they got.

## SQL Server
`compose.yaml` runs `mssql/server:2022` with a healthcheck. The password comes from `.env` (not in git).
The API builds its connection string from the `Database:*` settings plus `MSSQL_SA_PASSWORD` (read from `.env`).
The schema comes from the EF Core migration in `GritTrack.Api/Migrations`.

## Auth
`POST /api/auth/login` checks the demo user (from user secrets) and returns a signed JWT.
`CoursesController` puts the `WriteAccess` policy on POST/PUT/DELETE. With `Auth:Enabled=true` that policy needs a
valid token. With `false` everybody passes. GETs have no policy, so they're always public.

## Logging
Startup: data source + auth state. `CourseService`: one line per create/update/delete (course id + code only).
Failed logins log "Login failed" with no password. Tokens and connection strings are never logged.

## MAUI client
- `ApiCourseRepository`: HTTP calls, errors turned into `ApiRequestException` messages.
- `AuthService`: login, token in `SecureStorage`, `RunWriteAsync` wraps every write. No token (or a 401)
  opens `LoginPage`; backing out throws `LoginCancelledException`, which the page models catch.
- `BearerTokenHandler`: adds `Authorization: Bearer <token>` to every request once you are signed in.
- `AddCoursePageModel` saves through the API itself so validation errors (like a duplicate course code)
  show on the form.

## What I ran to check it
Every combo, with the Postman collection run twice in a row each time:

| Data source | Login | Result |
| --- | --- | --- |
| Json | off | 39 checks green |
| Json | on | 40 checks green |
| SqlServer (container healthy) | off | 39 checks green |
| SqlServer (container healthy) | on | 40 checks green |

The count is different because with login on the collection also checks the 401s and the token.

## Video order (8 minutes max)
The full script with what to say is the Demo script at the bottom of [GritTrack.Api/README.md](../GritTrack.Api/README.md). Short version:

1. Postman Runner green with login off, then green with login on.
2. Switch `DataSource` from `Json` to `SqlServer`, same requests still green. `docker compose ps` shows healthy.
3. The two startup log lines: `Data source: ...` and `Auth: ...`.
4. App: browse signed out, tap `+ Add`, Save, get sent to the sign in page.
5. Sign in, then create, edit and drop a course. Show the row in SQL Server.
6. One validation error in the app (blank course code, or a made-up one that's already taken).
7. Thirty seconds on a bug.
