# Changelog

## 2026-11-25

### Fixed

- Newman / Postman run with the **Auth OFF** environment was 40/41. "Login - correct password" got a 400 instead of a 404 because `demoPassword` ships blank, and the API checks for a password in the body before it says the login route is off. That request now has a pre-request script that fills in a stand-in password, only for itself and only when auth is off and the password is blank. Auth OFF is 41/41 out of the box now, Auth ON is still 42/42 with your real password.

- `GritTrack.App.Tests` was 4 red out of 121. The `BearerTokenHandler` read tests still said reads never get the token, but the handler was changed on 2026-11-15 to send it on every request once you're signed in. Swapped them for tests that match: reads get the token when signed in, and still go out with no header when signed out. 121/121 now.

### Added

- Two tests in `PostmanFilesTests.cs` that lock the stand-in in place and make sure it never gets saved into the environment.

## 2026-11-18

### Fixed

- `JsonCourseRepository.ReadAllAsync` now catches `FileNotFoundException` and `DirectoryNotFoundException` (empty list) and `JsonException` (busted file gets moved to `Data/courses.corrupt.json`, then starts fresh). Before, a corrupt `courses.json` would 500 every request.

### Added

- `docs/singleton-vs-scoped.md`: why the JSON repo is a singleton and the SQL repo is scoped.
- Comment in `Program.cs` spelling out the scoped rationale for `SqlCourseRepository`.

## 2026-11-15

### Project 4 cleanup

- `.env.example` still had merge conflict lines (`<<<<<<< HEAD` and friends) in it. Cleaned up, and `SQL_PORT` is kept.
- `BearerTokenHandler` now puts the login token on every request once you are signed in, not only on writes. Reads still work signed out. New API test proves a bad token can't lock you out of browsing.
- `GritTrack.Api/README.md` named things that don't exist: the volume is `gritrack-sql-data`, the compose service is `sql`, and the startup lines say `Auth: ON` / `Auth: OFF`.
- `docs/project4-secure-api.md` now uses the real names (`GritTrackDbContext`, `WriteAccess`, `RunWriteAsync`, `Database:*` + `MSSQL_SA_PASSWORD`).
- Root `README.md`: new Quick Start (compose up to running app), and the platform list only shows what the project really targets (Android, plus Windows on a Windows PC).
- Deleted `.fuse_hidden0000001e00000002`, a junk copy of the README that got committed by accident.

### Fixed

- Duplicate course codes could sneak in with extra spaces. `" CS201 "` was trimmed when it got saved but not when `CourseService` checked for a duplicate, so the JSON data source let a second `CS201` through. The check trims now (create and update).

### Added

- `JsonCourseRepositoryTests.cs`: tests for the JSON file repo (missing file, empty file, ids, 20 saves at once, temp file cleaned up).
- An API test for the fix above (same course code with spaces around it gets a 409).
- Endpoint table in the root `README.md`, and a "what a course has to look like" table in `GritTrack.Api/README.md`.
- `docs/week6-semester-api-part1.md`: where each Week 6 requirement lives, plus the video order.

## 2026-11-14

### Fixed

- What-if GPA simulator opened empty after importing assignments from the Mid-State Hub, because the list page never reloaded them. It now re-reads assignments every time it opens.
- Simulator GPA no longer moves before you touch anything: courses with nothing simulated keep their real grade.
- Simulator skips assignments worth 0 points.

### Changed

- Simulator moved into the shared bottom card, with Now / Projected / change, a "Set everything upcoming to" label on the percent buttons, a scenario line, per course letter grades, and an empty state.
- Validation: bad boxes get a pink outline and their own message; errors show in a big banner (`ErrorBanner`) next to the button you tapped. Course Code is now checked on the phone, matching the API. See `docs/validation-ux.md`.

### Added

- `[ProducesResponseType]` on every `CoursesController` action so the OpenAPI document lists 200/201/204/400/401/404/409.
- Postman: a non-numeric id request (404) and a Problem Details check on the duplicate course 409.
- Grades must be real letter grades (A, A-, B+ ... D-, F); `ZZ` now gets a 400. JSON writes ignore request cancellation so a temp file can't be left half written.
- `Controls/ValidatedField`, `Controls/ErrorBanner`, `PageModels/SimulatedCourseResult`, `docs/validation-ux.md`.

## 2026-11-12

### Added

- `docs/run-it-end-to-end.md`: fresh Windows machine to everything running. Podman for SQL Server, the login secrets, the API (Json / SqlServer / auth off), the Windows app, the Android emulator (finding `adb`, installing the API 36 platform, the `-p:` quoting), Postman, tests, and a "when something breaks" table (including the emulator showing an empty list). Linked from `README.md` and `GritTrack.Api/README.md`.

### Fixed

- Page content was dead on Windows: couldn't type in Add Course, the Mid-State Hub "Open" cards did nothing, and "Set starting GPA" wouldn't tap. Cause: `BluOSBottomSheet` is laid over the whole page, and even hidden, that invisible layer was catching the clicks. It now turns `InputTransparent` on while closed and off while the card is open (`Controls/BluOSBottomSheet.xaml.cs`).

## 2026-11-07

### Added

- `GritTrack.App.Tests`: tests for the MAUI app's non-visual code. Sign in and sign out, saving and restoring the token, `RunWriteAsync` (401, login, same save again), `BearerTokenHandler`, `ApiResponseReader`, `ApiCourseRepository`, and `LocalAssignmentStore` (survives a restart, duplicates, a broken file). It compiles the app's real source files into the test project, so there are no copies. Added to `GritTrack.slnx`.
- `GritTrack.Api.Tests/Sql`: tests for the SQL Server side. Connection string, the `.env` loader, startup migration retries, the `InitialCreate` migration and a "forgot to add a migration" check, and `SqlCourseRepository` plus the migrations against a real SQL Server (LocalDB, skipped on machines without it).
- `GritTrack.Api.Tests/Postman`: checks that the Postman collection and both environments are valid and consistent (no undefined variables, names match the status the script checks, no secrets), and that the four `.github/instructions` files agree with themselves.
- `GritTrack.Api/Data/DatabaseMigrationRunner.cs`: the startup migration retry loop, moved out of `Program.cs` so it can be tested. Same behavior: 6 tries, 5 seconds apart, warning logs only the exception type.
- `docs/testing.md`: what each test project covers and how to run it.

### Fixed

- Student Life card (Mid-State Engage) in `MidStateHubPageModel.cs` was pointing at `https://engage.midstate.edu`, which was a guess. It now opens the real Engage portal: `https://mstc.campuslabs.com/engage/`.
- MyMSTC Student Center card (Portals & Admin: registration, transcripts, financial aid, billing) was pointing at `https://mymstc.mstc.edu`, another guess. It now opens `https://myportal.mywilm.com/psc/MSTC/EMPLOYEE/EMPL/c/NUI_FRAMEWORK.PT_LANDINGPAGE.GBL`.

## 2026-10-27

### Added

- Mid-State Hub tab (`Pages/MidStateHubPage.xaml`, `MidStateHubPageModel.cs`): quick-launch cards for Blackboard, Teams, Jira, Engage, MyApps and MyMSTC, plus a syllabus importer: Parse shows a preview of the assignments, then Import puts them on the course you pick. Import stays locked until a course is picked and something is parsed.
- The course picker for the importer uses the shared `BluOSBottomSheet`, same as the grade picker on `AddCoursePage`.
- `AppShell.xaml` now has a bottom `TabBar` with Courses and Mid-State Hub, styled with the dynamic `AccentColor`.
- `MauiProgram.cs` registers `MidStateHubPageModel` and `MidStateHubPage`.
- `ICourseRepository.SaveAssignmentsLocalAsync` and `GetAssignmentsLocalAsync`, implemented in `ApiCourseRepository` and `InMemoryCourseRepository`.
- Duplicate check on import (`AssignmentDuplicateRule`, `AssignmentImportResult` in `Services/AssignmentImportResult.cs`): same title (trimmed, no case) + same due day is skipped, including repeats inside one paste. `SaveAssignmentsLocalAsync` now returns added / skipped counts and the hub status shows them.
- `Services/LocalAssignmentStore.cs`, registered in `MauiProgram.cs`, saving assignments to `local-assignments.json` in the app data folder.
- `docs/mid-state-hub.md`.

### Heads up

- The API only stores name, code, instructor, credits and grade. Imported assignments are saved to `local-assignments.json` by `LocalAssignmentStore` (through `ICourseRepository.SaveAssignmentsLocalAsync`) and put back on each course by `ApiCourseRepository`, so they survive restarts. Swap to the real endpoint at the TODO in `ApiCourseRepository` once it exists.
- Import now checks sign-in itself (`IsSignedIn` / `RequestLoginAsync`), since a local save never gives `RunWriteAsync` a 401 to react to.
- `SaveCourseAsync` no longer handles assignments.

## 2026-10-26

### Changed

- `Colors.xaml` now holds the BluOS palette (scratch blacks, text tones, 6 pastel stops, shared gradient brushes). Default MAUI `Primary`, `Secondary` and friends are remapped so no template purple shows up. `AccentColor` fallback is mint, `ThemeService` still overwrites it at runtime.
- Pages can keep their own gradient brush copies or switch to the shared ones, nothing breaks either way.

### Added

- Course search bar on the course list (name, course code, instructor).
- What-if GPA simulator with sliders and 100/90/80/70% presets, projected GPA, and change from current GPA.
- `SimulatedAssignmentItem` in its own file.
- `docs/course-search-and-gpa-simulator.md`.

### Fixed

- `CourseDetailPage`: course name label is now a Level1 heading and the Try Again button is 44 tall, clearing the a11y-heading and touch target warnings.
- `CourseListPage` and `AddCoursePage` a11y warnings: Try Again, Save Starting GPA, simulator and preset buttons are 44 tall, the simulator sliders have a 44 minimum height, GPA numbers are Level2 headings, the small course code label is 12, and the Credits entry has a semantic label.
- Compiled binding warnings: `CourseListPage`, `CourseDetailPage` and `AddCoursePage` now set `x:DataType`, and the swipe-delete, tap and grade-pill bindings point at the page model type directly.
- Deleting a course from the detail page popped two levels (`Routes.BackTwice`), now pops one (`Routes.Back`) back to the list.
- Projected GPA delta no longer shows mojibake for the plus-minus sign, uses `±`.
- `CourseListPageModel.cs` had a second pasted copy of the file inside it, which caused the CS1529, CS0101, and CS0111 build errors. The duplicate was removed and the navigation-service version was kept.
