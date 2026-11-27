# Final Project: photos, offline sync, reminders

Developer notes for the three features added for the final (Option 1: extend my existing app).
The short pitch is in [final-project-proposal.md](final-project-proposal.md).

## The big picture

```
                     +--------------------+
  CourseListPage --> | CourseListPageModel| --+
  CourseDetailPage ->| CourseDetailPageModel --+--> ICourseRepository
                     +--------------------+   |        (OfflineCourseRepository)
                          |        |          |              |
                          v        v          |              v
                  IPhotoService  IReminderService     ApiCourseRepository --HTTPS--> GritTrack.Api
                  (camera,        (phone                     |
                   gallery)        notifications)            v
                                                     LocalAssignmentStore
                                                     (local-assignments.json)
```

Pages only bind. Page models own the commands. Everything that touches the phone (camera, notifications, files, network) is a service handed in by `MauiProgram.cs`.

## 1. Assignment photos

| Piece | File | Job |
| --- | --- | --- |
| `IPhotoService` | `Services/Photos/IPhotoService.cs` | What page models call: take, pick, path, delete |
| `PhotoService` | `Services/Photos/PhotoService.cs` | The real thing. Uses MAUI `MediaPicker` and `Permissions` |
| `Assignment.PhotoFileName` | `Models/Assignment.cs` | The saved file's name, or null |
| `AssignmentRowItem` | `PageModels/AssignmentRowItem.cs` | One assignment on screen. Updates when its photo changes |
| The card | `Pages/CourseDetailPage.xaml` | Assignment list + the bottom card with the buttons |

How a photo gets saved:

1. Tap an assignment on the course page. `OpenAssignmentCommand` opens the card.
2. **Take Photo** or **Pick From Gallery** runs `AttachPhotoAsync` in `CourseDetailPageModel`.
3. `PhotoService` gets the photo and copies it into the app's own `assignment-photos` folder under a new name. The picker's file is temporary, so the app keeps its own copy.
4. The file name goes on the assignment and `UpdateAssignmentLocalAsync` saves it to `local-assignments.json`.
5. The old photo (if there was one) is deleted.

Only the file **name** is saved, not the full path, because the app's folder can move between installs.

Failure paths: no camera (`NotAvailable`), permission denied (`PermissionDenied`), backed out (`Cancelled`, no message), anything else (`Failed`). Each one comes back as a `PhotoResult` with a message that goes straight on the card.

## 2. Offline mode + sync

| Piece | File | Job |
| --- | --- | --- |
| `OfflineCourseRepository` | `Services/Offline/OfflineCourseRepository.cs` | Wraps the API repository. Falls back to the phone when the server can't be reached |
| `PendingChangeRules` | `Services/Offline/PendingChangeRules.cs` | The waiting list rules. No files, no network |
| `PendingChange` | `Services/Offline/PendingChange.cs` | One waiting change: create, update or delete |
| `JsonFile<T>` | `Services/Offline/JsonFile.cs` | A small JSON file that can't be left half written |
| `ISyncService` | `Services/Offline/ISyncService.cs` | What the course list uses for the card and the Sync button |
| `ApiUnreachableException` | `Services/Api/ApiExceptions.cs` | "The server never answered". This is what flips the app into offline mode |

The rest of the app still asks for `ICourseRepository`. `MauiProgram.cs` hands it the offline wrapper, so no page model had to learn about offline.

Two files on the phone:

- `courses-cache.json`: the last course list the server gave us.
- `pending-changes.json`: changes that haven't reached the server yet.

What happens when the server is down:

- **Load:** show `courses-cache.json` with the waiting changes laid on top.
- **Save or drop:** put it on the waiting list. A new course gets a temporary negative ID (`-1`, `-2`...) since real IDs from the server are always above 0.

The waiting list keeps one entry per course:

- Edit a course that's still waiting to be created: the create just gets the new details.
- Edit twice: only the last edit is kept.
- Drop a course that never reached the server: it just leaves the list.

Sync (`SyncAsync`) sends the list oldest first:

| The server says | What happens to that change |
| --- | --- |
| OK | Off the list |
| Can't be reached | Stop. Everything left keeps waiting |
| 404, the course is gone | Off the list, nothing to do |
| No (duplicate code, bad field) | Stays, with the reason. The rest keep going |
| 401, log in first | The login page opens, then the sync runs again |

The list is saved after every single change, so a sync that stops halfway never sends the same thing twice.

When a course made offline finally gets its real ID, `LocalAssignmentStore.MoveCourseAsync` moves its assignments from the temporary ID to the real one.

## 3. Due date reminders

| Piece | File | Job |
| --- | --- | --- |
| `ReminderPlanner` | `Services/Reminders/ReminderPlanner.cs` | Decides when and what. No phone code |
| `IReminderService` | `Services/Reminders/IReminderService.cs` | What page models call: schedule, cancel |
| `AndroidReminderService` | `Platforms/Android/AndroidReminderService.cs` | Real notifications through `Plugin.LocalNotification` |
| `NoReminderService` | `Services/Reminders/NoReminderService.cs` | Windows. Says reminders aren't available there |

When: 9 AM the day before it's due. If that already passed, 9 AM on the due day. If that passed too, or it's done, no reminder.

The notification uses the assignment's own ID, so setting it again replaces the old one instead of stacking up. Marking an assignment done or dropping the course cancels it.

The package is only referenced for Android in `GritTrack.csproj`, and the Android class lives in `Platforms/Android`, so the Windows build never touches it.

## Permissions (`Platforms/Android/AndroidManifest.xml`)

- `CAMERA`, with the camera marked not required so phones without one can still install.
- `READ_EXTERNAL_STORAGE` and `WRITE_EXTERNAL_STORAGE`, capped at `maxSdkVersion="32"`. MAUI's MediaPicker docs say Android 12 and older need these for the camera and the gallery. Android 13+ skips them.
- `READ_MEDIA_IMAGES`, the Android 13+ version. Images only, because the app never picks video or audio.
- `POST_NOTIFICATIONS` for reminders, `RECEIVE_BOOT_COMPLETED` so they come back after a restart.
- `AndroidManifestTests.cs` reads the real file and goes red if any of these go missing.

## Test scenarios

Run them:

```bash
dotnet test GritTrack.App.Tests/GritTrack.App.Tests.csproj
```

```bash
dotnet test GritTrack.Api.Tests/GritTrack.Api.Tests.csproj
```

Unit tests (one class, no network):

| File | Scenarios |
| --- | --- |
| `PendingChangeRulesTests.cs` | Temporary IDs, one entry per course, edit after create, delete after create, delete after edit, the list overlay |
| `ReminderPlannerTests.cs` | Day before, due tomorrow, due today, overdue, already done, the title and message |
| `GpaCalculatorServiceTests.cs` | Letter grades to points, credits weighting, no courses |
| `AndroidManifestTests.cs` | Every permission the photos and reminders need, old storage ones stop at Android 12, camera stays optional, the camera app is visible |

Integration tests (several pieces working together, real files, a fake server):

| File | Scenarios |
| --- | --- |
| `OfflineCourseRepositoryTests.cs` | Online save, offline save, list from the saved copy, changes surviving an app restart, full sync, sync while still offline, server says no, sync twice, offline delete, course already gone, offline edit keeps assignments |
| `LocalAssignmentStoreTests.cs` | Photo and reminder saved and still there after a restart, moving assignments off a temporary course ID |
| `GritTrack.Api.Tests` | The real API in memory: endpoints, login, validation, JSON storage |

## What only a phone can prove

The tests can't open a camera or show a notification. Before the video, on the Android emulator:

1. Take a photo, see it on the card and in the list. Close and reopen the app, still there.
2. Pick from the gallery. Remove the photo.
3. Deny the camera permission, see the message.
4. Stop the API. Add a course, see **Waiting to sync**. Start the API, tap **Sync Now**.
5. Turn on a reminder for something due in a couple of days. Deny notifications once to see that message too.

## Video order (5 to 8 minutes)

1. The app: browse, sign in, one normal create so login is shown working.
2. Photos: camera, then gallery, then reopen the app to show it stuck.
3. Offline: stop the API, make a change, show the card, start the API, **Sync Now**. Show the row in the database.
4. Reminder: turn one on, show the message with the date.
5. Tests: run `dotnet test` on both projects.
6. Code walk: `OfflineCourseRepository` and `PendingChangeRules` (the part I'm proudest of), then `PhotoService`.
