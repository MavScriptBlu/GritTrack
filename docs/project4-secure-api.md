# Project 4: secure API + MAUI client

How the pieces fit. For step-by-step setup see [GritTrack.Api/README.md](../GritTrack.Api/README.md).

```
MAUI app  --HTTPS + Bearer token-->  GritTrack.Api  --ICourseRepository-->  JSON file   (DataSource = Json)
                                                                        \->  SQL Server  (DataSource = SqlServer, in a container)
```

## Data source switch
`Program.cs` reads `DataSource` and registers one `ICourseRepository`:
`JsonCourseRepository` (singleton, one file) or `SqlCourseRepository` (scoped, on `AppDbContext`).
Controller and `CourseService` don't know which one they got.

## SQL Server
`compose.yaml` runs `mssql/server:2022` with a healthcheck. The password comes from `.env` (not in git).
The API builds its connection string from `Sql:*` config plus the `Sql:Password` user secret.
The schema comes from the EF Core migration in `GritTrack.Api/Migrations`.

## Auth
`POST /api/auth/login` checks the demo user (from user secrets) and returns a signed JWT.
`CoursesController` puts the `CanWrite` policy on POST/PUT/DELETE. With `Auth:Enabled=true` that policy needs a
valid token. With `false` everybody passes. GETs have no policy, so they're always public.

## Logging
Startup: data source + auth state. `CourseService`: one line per create/update/delete (course id + code only).
Failed logins log "Login failed" with no password. Tokens and connection strings are never logged.

## MAUI client
- `ApiCourseRepository`: HTTP calls, errors turned into `ApiRequestException` messages.
- `AuthService`: login, token in `SecureStorage`, `RunSignedInAsync` wraps every write. No token (or a 401)
  opens `LoginPage`; cancel throws `OperationCanceledException`, which callers swallow.
- `BearerTokenHandler`: adds the header to every request.
- `AddCoursePageModel` saves through the API itself so validation errors (like a duplicate course code)
  show on the form.
