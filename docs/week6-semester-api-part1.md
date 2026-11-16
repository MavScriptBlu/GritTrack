# Week 6: Semester API, Part 1

My app is GritTrack (approved instead of the recipe / finance / fitness options). The primary resource is **courses**.
This page is the cheat sheet: where every requirement lives, and what to show in the video.

## Where everything lives

| Requirement | Where to look |
| --- | --- |
| Endpoint table | Root `README.md` (API Endpoints) and `GritTrack.Api/README.md` |
| Folders | `GritTrack.Api/` has `Controllers`, `DTOs`, `Models`, `Repositories`, `Services`, `Data` |
| Model with fields the client can't set | `Models/Course.cs`: `Id`, `GradePoints`, `CreatedAt` |
| Request and response DTOs | `DTOs/CourseRequestDto.cs`, `DTOs/CourseResponseDto.cs` |
| Interfaces | `Services/ICourseService.cs`, `Repositories/ICourseRepository.cs` |
| Business rule (409) | `Services/CourseService.cs`: no two courses with the same course code. It hands back a `SaveCourseOutcome`, the controller picks the status code |
| DI registrations | `Program.cs`: repository is a singleton, service is scoped |
| JSON file repo | `Repositories/JsonCourseRepository.cs`, data in `Data/courses.json` |
| File lock | One `SemaphoreSlim(1, 1)`, `WaitAsync` above the `try`, `Release` in the `finally` |
| Safe writes | `WriteAllAsync`: temp file first, then `File.Move(..., overwrite: true)` |
| Missing or empty file | `ReadAllAsync` gives back an empty list |
| Ids | Picked in `AddAsync`, inside the lock |
| Controller | `Controllers/CoursesController.cs`: `[Route("api/courses")]`, `{id:int}` routes |
| Query filter | `GET /api/courses?grade=B+` |
| Validation | `DTOs/CourseRequestDto.cs`: 9 rules with my own messages |
| JSON names in errors | `SystemTextJsonValidationMetadataProvider` in `Program.cs` |
| Error handling | `AddProblemDetails()` and `UseExceptionHandler()` in `Program.cs`. No try/catch in the actions |
| Postman | `GritTrack.Api/postman/` |
| Git ignore | `Data/*.json` and `Data/*.tmp` in `.gitignore` |

## Run it

```bash
dotnet run --project GritTrack.Api
```

Part 1 is JSON storage, so make sure `"DataSource"` is `"Json"` in `appsettings.json`.
For the Part 1 video, turn login off so Postman doesn't need a token: set `"Auth": { "Enabled": false }`
and use the `GritTrack-AuthOff` Postman environment.

## Tests

```bash
dotnet test GritTrack.Api.Tests
```

30 tests: the endpoints, the login rules, and the JSON file repo.

## Video order (5 minutes max)

1. The endpoint table in the README.
2. The folders in Visual Studio, then the DI registrations in `Program.cs`.
3. The whole Postman collection in the Collection Runner, all green. Run it twice.
4. One POST in Postman, then open `Data/courses.json`. Point at `Id`, `GradePoints` and `CreatedAt`: the client never sent those.
5. One validation failure with the error body showing (send `"currentGrade": "Z"`), then one 409 (POST the same course code twice).
6. Thirty seconds on a bug.

## Bug story I can use

Course codes had to be unique, but `" CS201 "` (with spaces) got past the check. The service trimmed the code
before saving it, but checked for a duplicate with the untrimmed one. So the file ended up with two `CS201`s.
I found it by writing a test that posts the same code with spaces and expects a 409. It came back 201.
The fix was one `.Trim()` on the lookup in `CourseService`.
