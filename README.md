# GritTrack

GritTrack is a cross-platform student scheduling and GPA tracking application built with **.NET MAUI** and **C#**. It helps students organize courses and assignments, monitor academic progress, and understand how their current work affects their GPA.

> Developed for the Mobile Application Development class at MSTC.

## Features

- **Course management**
  - Add, edit, view, and remove courses.
  - Track course codes, instructors, credit hours, current letter grades, and grade points.
- **Assignment tracking**
  - Organize assignments by type, including homework, quizzes, tests, projects, readings, and other work.
  - Track due dates, completion status, points, estimated effort, and notes.
  - Identify overdue and upcoming assignments.
- **GPA calculation**
  - Calculate a current GPA from tracked courses.
  - Optionally include a starting GPA and completed credits for students who are continuing an existing academic record.
- **Course search**
  - Filter the course list as you type by course name, course code, or instructor.
- **What-if GPA simulator**
  - Pick an expected score for every ungraded assignment (sliders, or one tap for 100/90/80/70%).
  - See the projected GPA and how far it is from your current GPA. See [docs/course-search-and-gpa-simulator.md](docs/course-search-and-gpa-simulator.md).
- **Mid-State Hub**
  - A second tab with quick-launch cards for the MSTC sites (Blackboard, Teams, Jira, Engage, MyApps, MyMSTC).
  - Paste a syllabus schedule (`Title | MM/DD/YYYY | Points`, one per line) and import it as assignments on the course you pick. See [docs/mid-state-hub.md](docs/mid-state-hub.md).
- **Academic status indicators**
  - Classify GPA progress as `Safe`, `Watch`, or `AtRisk`.
  - Dynamically update the application accent color based on GPA status.
- **Cross-platform UI**
  - Built with .NET MAUI and XAML for shared application experiences across supported platforms.
- **Extensible architecture**
  - Uses dependency injection, page models, services, and repository interfaces.
  - Course and GPA storage can be replaced later without rewriting the UI layer.

## Technology Stack

- C#
- .NET 10
- .NET MAUI
- XAML
- MVVM-inspired page models
- `Microsoft.Extensions.DependencyInjection`
- `System.Text.Json`
- ASP.NET Core Web API, EF Core + SQL Server, JWT bearer auth (see `GritTrack.Api`)

## Supported Platforms

The project currently targets the following frameworks:

- Android 21+
- iOS 15+
- Mac Catalyst 15+
- Windows 10.0.17763.0+

Support depends on the operating system and workload configuration of the development machine.

## Project Structure

```text
GritTrack/
├── Controls/       # Reusable UI controls
├── Models/         # Course, assignment, and GPA data models
├── PageModels/     # View models and UI command logic
├── Pages/          # XAML application pages
├── Platforms/      # Platform-specific startup and configuration
├── Resources/      # Fonts, images, styles, and app assets
├── Services/       # GPA calculation, repositories, and theme services
└── Utilities/      # Shared helper functionality
```

Important application components include:

- `MauiProgram.cs` — configures the MAUI application and dependency injection.
- `AppShell.xaml` - hosts the bottom tab bar (Courses and Mid-State Hub). The Courses tab title comes from `AppShellViewModel`. Pushed pages are registered in `AppShell.xaml.cs` using the names in `Services/Routes.cs`.
- `INavigationService` / `ShellNavigationService` — the only place that talks to `Shell`. Page models call it to navigate and show alerts, so the pages themselves have no navigation code.
- `CourseListPageModel` — manages courses, starting GPA data, GPA recalculation, course search, the what-if GPA simulator, and course actions.
- `MidStateHubPageModel` - the Mid-State Hub tab: portal links, course picker, and the syllabus importer.
- `SimulatedAssignmentItem` — one upcoming assignment and its expected score in the what-if simulator.
- `GpaCalculatorService` — calculates GPA values and academic status.
- `ICourseRepository` — abstracts course storage.
- `IGpaProfileRepository` — abstracts starting GPA storage.
- `ThemeService` — updates the application theme according to GPA status.

### Navigation (MVVM)

Pages only set their `BindingContext`. Each page model owns its commands, navigates through `INavigationService`, and receives data from Shell through `IQueryAttributable` (`SavedCourse` / `DeletedCourse` on the list, `Course` on the detail page, `EditCourse` on the add/edit page). The add button lives in the top toolbar, and the list shows an empty state and a Try Again error card.

## Prerequisites

Install the following before building the project:

1. The .NET 10 SDK.
2. Visual Studio 2022 with the **.NET Multi-platform App UI development** workload, or an equivalent .NET MAUI development environment.
3. The platform SDK or emulator required for your target platform.

You can verify the .NET installation with:

```bash
dotnet --version
dotnet workload list
```

## Getting Started

Clone the repository and move into the project directory:

```bash
git clone https://github.com/MavScriptBlu/GritTrack.git
cd GritTrack
```

Restore dependencies:

```bash
dotnet restore
```

Build the solution:

```bash
dotnet build GritTrack.slnx
```

To run the application, open the solution in Visual Studio, select an available target device or emulator, and start the project. You can also use the .NET CLI with a specific target framework and runtime identifier when your development environment is configured for that platform.

For example, Android builds commonly use a command similar to:

```bash
dotnet build GritTrack/GritTrack.csproj -f net10.0-android
```

## Using the App

1. Launch GritTrack.
2. Add your current courses, including credits and grade information.
3. Select a course to review or manage its assignments.
4. Add assignments with due dates, types, points, estimated hours, and notes.
5. Mark assignments complete as you finish them.
6. Review the calculated GPA and academic status indicator.
7. If needed, enter your existing GPA and completed credits so GritTrack can calculate a cumulative GPA.

## Data Storage

Courses now live behind the **GritTrack API** (`GritTrack.Api`). The app talks to it over HTTP through `ApiCourseRepository` and never touches SQL Server itself. The API keeps courses in a JSON file or in SQL Server (a setting), and every create, update and delete needs a login token. Browsing is public.

- Start the API first. Setup, secrets, Docker, Postman and troubleshooting are in [GritTrack.Api/README.md](GritTrack.Api/README.md).
- The login token is stored with `SecureStorage`. Trying to add, edit or drop a course while signed out opens the sign in page and then finishes the action.
- The starting GPA (`IGpaProfileRepository`) is still in memory and resets when the app fully restarts.
- Imported assignments are saved on the phone in `local-assignments.json` (`LocalAssignmentStore`, via `ICourseRepository.SaveAssignmentsLocalAsync`) because the API doesn't store them yet.
- `InMemoryCourseRepository` is still in `Services/` for tests but is no longer registered in `MauiProgram.cs`.

## Architecture Notes

GritTrack separates UI, application logic, and storage responsibilities:

- **Pages** provide the XAML-based user interface.
- **Page models** expose bindable properties and commands to the UI.
- **Services** handle GPA calculations, theme updates, and data access abstractions.
- **Repositories** isolate storage details from the rest of the application.
- **Models** represent courses, assignments, and GPA profiles.

This structure makes it easier to add features such as persistent local storage, cloud synchronization, notifications, authentication, or additional academic analytics.

## Development Notes

- The project uses nullable reference types and implicit usings.
- XAML source generation is enabled in the project file.
- Custom fonts are included under `GritTrack/Resources/Fonts`.
- Debug logging is enabled through `Microsoft.Extensions.Logging.Debug` in debug builds.

## Contributing

Contributions and improvements are welcome.

1. Create a feature branch.
2. Make focused changes.
3. Build and test the application on the relevant target platform.
4. Open a pull request with a clear description of the change.

## License

GritTrack is distributed under the MIT License. See [LICENSE.txt](LICENSE.txt) for the full license text.

## Project Links

- [Repository](https://github.com/MavScriptBlu/GritTrack)
- [Issues](https://github.com/MavScriptBlu/GritTrack/issues)
