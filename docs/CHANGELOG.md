# Changelog

## 2026-10-26

### Changed
- `Colors.xaml` now holds the BluOS palette (scratch blacks, text tones, 6 pastel stops, shared gradient brushes). Default MAUI `Primary`, `Secondary` and friends are remapped so no template purple shows up. `AccentColor` fallback is mint, `ThemeService` still overwrites it at runtime.
- Pages can drop their own copies of the gradient brushes and use the shared ones whenever, nothing breaks if they keep them.

### Added
- Course search bar on the course list (name, course code, instructor).
- What-if GPA simulator with sliders and 100/90/80/70% presets, projected GPA, and change from current GPA.
- `SimulatedAssignmentItem` in its own file.
- `docs/course-search-and-gpa-simulator.md`.

### Fixed
- Compiled binding warnings: `CourseListPage`, `CourseDetailPage` and `AddCoursePage` now set `x:DataType`, and the swipe-delete, tap and grade-pill bindings point at the page model type directly.
- Deleting a course from the detail page popped two levels (`Routes.BackTwice`), now pops one (`Routes.Back`) back to the list.
- Projected GPA delta no longer shows mojibake for the plus-minus sign, uses `\u00B1`.
- `CourseListPageModel.cs` had a second pasted copy of the file inside it, which caused the CS1529, CS0101, and CS0111 build errors. The duplicate was removed and the navigation-service version was kept.
