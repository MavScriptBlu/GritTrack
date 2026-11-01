# Course search and what-if GPA simulator

Both features live on the course list page (`Pages/CourseListPage.xaml`) and are driven by `PageModels/CourseListPageModel.cs`.

## Course search

- The search bar sits above the course list and is bound to `SearchQuery`.
- `ApplyFilter()` rebuilds `FilteredCourses` from `Courses`. A course matches when the text is in its name, course code, or instructor (not case sensitive).
- The list shows `FilteredCourses`. `Courses` stays the full list, so GPA math never changes while you search.
- `RecalculateGpa()` calls `ApplyFilter()`, so adding, editing, deleting, or reloading a course keeps the results fresh.
- The Clear button (`ClearSearchCommand`) only shows once something is typed (`HasSearchQuery`).

## What-if GPA simulator

- The **What-if GPA simulator** button (`ToggleSimulatorCommand`) opens or closes the panel (`IsSimulatorOpen`).
- Opening it calls `BuildSimulationItems()`, which makes one `SimulatedAssignmentItem` for every assignment that is not graded yet. Each starts at 90%.
- Each item has a slider for `ExpectedPoints`, clamped between 0 and the assignment's points. Moving it calls `RecalculateSimulatedGpa()`.
- The preset buttons (100, 90, 80, 70) run `ApplyPresetScenarioCommand`, which sets every item to that percent.
- `RecalculateSimulatedGpa()`:
  1. Adds up points earned and points possible from graded work in each course.
  2. Adds the simulated scores on top.
  3. Turns the course percent into a letter grade with `GpaCalculatorService.GetLetterGradeFromPercent` and grade points with `GetGradePoints`.
  4. Calculates the projected GPA (with the starting GPA and credits when they are set) and the projected status.
- `GpaDeltaText` shows the gap between the projected GPA and the current GPA, like `+0.12` or `-0.05`.
- Courses with no graded or simulated points keep their real grade.
- Nothing is saved. The simulator never changes real assignments or grades.
