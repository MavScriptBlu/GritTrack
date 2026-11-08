# Changelog

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
