# Final Project Proposal: GritTrack

## Option

**Option 1: Extend my existing app.** GritTrack already has the API, SQL Server in a container, login, and the MVVM MAUI app from Project 4. I'm adding to it instead of starting over.

## Project

**GritTrack** is a course and GPA tracker for students. You keep your courses and assignments in one place and see what your grades are doing to your GPA.

## What I'm adding

### 1. Assignment photos (device hardware: camera + photo gallery)

Snap a photo of the assignment sheet or the whiteboard, or pick one you already took, and attach it to an assignment. It's saved on the phone and shows up on the assignment later.

Workflow: open a course, tap an assignment, **Take Photo** or **Pick From Gallery**, photo shows on the card and as a small preview in the list. **Remove Photo** takes it off.

What can go wrong and what the app does:

- No camera on the device: says so and points to the gallery.
- Camera permission denied: says how to turn it on, gallery still works.
- Backed out of the camera: nothing happens, no error.

### 2. Offline mode + sync (new to me)

If the API can't be reached, the app keeps working. The course list comes from the last copy saved on the phone, and anything I add, edit or drop goes on a waiting list instead of failing. A card on the course list shows how many changes are waiting, with a **Sync Now** button to send them when the server is back.

What can go wrong and what the app does:

- Still offline when I sync: nothing is lost, everything keeps waiting.
- The server turns a change down (like a course code that got taken): that one stays on the list with the reason, the rest still go through, and I can throw the bad one away.
- Not signed in: sync is a write, so the login page opens and the sync carries on after.

### 3. Due date reminders (new to me)

Turn on a reminder for an assignment and the phone shows a notification at 9 AM the day before it's due (or 9 AM that day if the day before already passed).

What can go wrong and what the app does:

- Notifications turned off for the app: says how to turn them on.
- Already due: says there's nothing left to remind about.
- Marked done or the course is dropped: the reminder is cancelled.
- Windows: reminders aren't available there, and the button says so.

## Device features, libraries, new tech

- `MediaPicker` (.NET MAUI) for the camera and the photo gallery.
- `Permissions` (.NET MAUI) for asking for the camera.
- `Plugin.LocalNotification` (NuGet) for the notifications. Android only.
- Everything else is what the app already uses: MVVM page models, services through dependency injection, `System.Text.Json` files on the phone.

## Security

Nothing about login changes. Browsing is still public, every write to the API still needs a token, and syncing offline changes counts as a write.

## Testing

xUnit. Unit tests for the offline rules, sync, and reminder timing, on top of the API and app tests that are already there. See [final-project.md](final-project.md).
