# Changelog

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
- A made-up grade like `Z` used to save with 0 grade points and drag the GPA down. `CourseRequestDto` now only takes real letter grades (A to F) and answers 400 with a message.

### Added

- `JsonCourseRepositoryTests.cs`: tests for the JSON file repo (missing file, empty file, ids, 20 saves at once, temp file cleaned up).
- Two API tests for the fixes above. 30 tests total.
- Endpoint table in the root `README.md`, and a "what a course has to look like" table in `GritTrack.Api/README.md`.
- `docs/week6-semester-api-part1.md`: where each Week 6 requirement lives, plus the video order.

## 2026-11-07

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
