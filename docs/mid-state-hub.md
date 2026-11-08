# Mid-State Hub

A second tab next to Courses. It lives in `Pages/MidStateHubPage.xaml` and is driven by `PageModels/MidStateHubPageModel.cs`.

## Campus portal cards

- `Portals` holds six `CampusPortalItem`s: Blackboard, Teams, Jira, Mid-State Engage (opens `https://mstc.campuslabs.com/engage/`), MyApps (SSO) and MyMSTC (opens the MSTC student portal landing page at `myportal.mywilm.com`).
- Each card is colored from its `AccentHex` (the page uses `AccentTint` and `AccentBrush`, which turn that hex into a real color and brush).
- Tapping a card runs `OpenUrlCommand`, which opens the link in the phone's browser. If the link can't open, the status line says so.

## Quick syllabus importer

Paste one assignment per line:

```text
Lab 4: API Auth | 11/05/2026 | 50
Midterm Project | 11/12/2026 | 100
```

- **Title** is required. Blank titles are skipped.
- **Date** is optional. A missing or bad date becomes today plus 5 days.
- **Points** is optional. Only digits and dots are read, so `50 pts` works. Missing, bad or zero becomes 50.
- Every line becomes a `Homework` assignment with the note "Imported from mstc.blackboard.com schedule".

### Picking the course

- The **Target Course** box shows `SelectedCourseDisplay`. Tapping it runs `ToggleCoursePickerCommand`, which opens the shared `BluOSBottomSheet`.
- Tapping a course in the sheet runs `SelectCourseCommand`, which picks it and closes the sheet. Cancel or tapping the dim area closes it too.
- `LoadCoursesAsync()` fills `AvailableCourses`. The page calls it every time the tab shows up (`OnAppearing`), so a course added on the Courses tab is there right away. It keeps your selection if that course still exists, otherwise it picks the first one.
- No courses at all? The box and the sheet both say to add one on the Courses tab.

### Parse, preview, import

1. **Parse Assignments** (`ParseSyllabusCommand`) turns the pasted lines into `ParsedAssignments`, shown as a preview list (title, due date, points). Nothing is saved yet.
2. **Import To Selected Course** (`ImportSyllabusCommand`) stays locked and dimmed until a course is picked and there is a preview. `CanImport()` checks both, and it also locks while a save is running so a double tap can't import twice.
3. Editing the pasted text after parsing wipes the preview, so you can't import stale lines.

### Saving

1. The previewed assignments get the target course's ID and are added to it.
2. The save runs through `IAuthService.RunWriteAsync`, so if you're signed out the login page opens and the save finishes after sign in.
3. If the save fails (`ApiRequestException`) or the login is cancelled (`LoginCancelledException`), the assignments are taken back off the course and the message shows in pink. The text box and preview are left alone so nothing is lost.
4. On success the text box and preview clear and the status line shows in mint.

## Where imported assignments live (for now)

`CourseRequest` and `CourseResponse` (in `Services/Api/CourseApiModels.cs`) only carry name, course code, instructor, credits and grade, so the API can't store assignments.

Until it can, Import saves them on the phone. The hub calls `ICourseRepository.SaveAssignmentsLocalAsync(courseId, assignments)`, which stamps the course ID on each assignment and hands them to `Services/LocalAssignmentStore.cs`. The store writes `local-assignments.json` in `FileSystem.AppDataDirectory`, one list per course ID, so **they survive closing the app**. `ApiCourseRepository` puts them back on every course it loads, so the course list, the simulator and the hub all see them. `GetAssignmentsLocalAsync(courseId)` reads one course's list. Deleting a course drops its saved assignments. `SaveCourseAsync` doesn't touch assignments at all.

**Re-importing is safe.** An assignment counts as a duplicate when its title (trimmed, ignoring upper/lower case) and its due day (time of day ignored) match one the course already has. The same rule also catches two identical lines in one paste. `SaveAssignmentsLocalAsync` gives back an `AssignmentImportResult` (`AddedCount`, `SkippedCount`, `AddedAssignments`) and the status says things like "Imported 12 assignment(s) to Web Development I (3 duplicate(s) skipped)." If everything was already there it says nothing new went in. The rule lives in one place, `AssignmentDuplicateRule` in `Services/AssignmentImportResult.cs`, so the future batch endpoint can copy it.

Import checks sign-in first (`IsSignedIn`, then `RequestLoginAsync`), because a local save never hits the server, so `RunWriteAsync` would never ask for a login. After a good save the preview and the paste box clear, Import locks again, and the status says how many were saved to which course.

When the API gets an assignments endpoint, swap the body of `SaveAssignmentsLocalAsync` (the `// TODO: Swap to batch POST /api/courses/{courseId}/assignments` marker) for the HTTP call, then delete `LocalAssignmentStore`. The page model doesn't change.

## Navigation

`AppShell.xaml` wraps both pages in a `TabBar`, styled with the dynamic `AccentColor`. To go back to the hamburger menu, delete the `TabBar` tags and keep the two `ShellContent` items. `MidStateHubPageModel` and `MidStateHubPage` are registered in `MauiProgram.cs`.
