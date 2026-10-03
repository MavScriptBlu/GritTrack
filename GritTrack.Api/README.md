# GritTrack API

Back end for GritTrack. Part 1 stores data in a JSON file (`Data/courses.json`).
Part 2 swaps in SQL Server behind the same `ICourseRepository` interface — nothing
above that layer changes.

## Endpoints

| Method | URL | Does | Success | Failure |
|---|---|---|---|---|
| GET | /api/courses | List all courses. Optional `?grade=` filters by current letter grade (e.g. `?grade=B+`). | 200 | — |
| GET | /api/courses/{id} | Get one course by id. | 200 | 404 |
| POST | /api/courses | Add a course. | 201 | 400, 409 |
| PUT | /api/courses/{id} | Replace a course's fields. | 200 | 400, 404, 409 |
| DELETE | /api/courses/{id} | Remove a course. | 204 | 404 |

409 means the course code in the request already belongs to a different course —
course codes have to be unique.

## Running it

```
dotnet run --project GritTrack.Api
```

The OpenAPI document is at `/openapi/v1.json` in Development.

## Postman

The collection lives in `postman/GritTrack.postman_collection.json`. Import it,
set the `baseUrl` variable to wherever this is running (check the console output
or `launchSettings.json` for the actual port), and run it from the Collection
Runner — it creates its own test data and cleans up after itself, so it passes
however many times in a row.
