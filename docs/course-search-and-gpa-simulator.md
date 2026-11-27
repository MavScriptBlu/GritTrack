# Course search and what-if GPA simulator

Both features live on the course list page (`Pages/CourseListPage.xaml`) and are driven by `PageModels/CourseListPageModel.cs`.

## Course search

- The search bar sits above the course list and is bound to `SearchQuery`.
- `ApplyFilter()` rebuilds `FilteredCourses` from `Courses`. A course matches when the text is in its name, course code, or instructor (not case sensitive).
- The list shows `FilteredCourses`. `Courses` stays the full list, so GPA math never changes while you search.
- `RecalculateGpa()` calls `ApplyFilter()`, so adding, editing, deleting, or reloading a course keeps the results fresh.
- The Clear button (`ClearSearchCommand`) only shows once something is typed (`HasSearchQuery`).

## What-if GPA simulator

- The **What-if GPA simulator** button (`ToggleSimulatorCommand`) opens a bottom card (`BluOSBottomSheet`, same one as delete confirm and the grade picker) so it floats over the page instead of squeezing the course list. Backdrop tap or **Done** closes it.
- Opening it first re-reads each course's assignments from the local store (`GetAssignmentsLocalAsync`). The list page loads its courses once, so without this the simulator was empty right after importing from the Mid-State Hub.
- `BuildSimulationItems()` makes one `SimulatedAssignmentItem` per assignment that has no grade yet and is worth more than 0 points. Each starts at 90%.
- The card shows **Now**, **Projected**, and the gap (`GpaDeltaText`, like `+0.12`).
- If nothing is upcoming (`HasSimulatedAssignments` is false) the card says "Nothing to simulate yet" and points to the Hub, instead of showing buttons that do nothing.
- **Set everything upcoming to** buttons (100, 90, 80, 70) run `ApplyPresetScenarioCommand`: "what if I get X% on everything coming up?". `ScenarioText` says what was applied, and flips to "your own scores" once you drag a slider.
- Each slider sets `ExpectedPoints` (clamped 0 to the assignment's points) and calls `RecalculateSimulatedGpa()`.
- `RecalculateSimulatedGpa()`:
  1. Only touches courses that have simulated assignments. Other courses keep their real grade, so the GPA doesn't move before you do anything.
  2. For those, adds graded points plus simulated points, turns the percent into a letter with `GpaCalculatorService`, and lists it in `SimulatedCourseResults` ("CS-160: B to A- (91%)").
  3. Calculates the projected GPA (with starting GPA and credits when set) and the projected status.
- Nothing is saved. The simulator never changes real assignments or grades.
