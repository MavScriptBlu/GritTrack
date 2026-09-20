## 2026-09-20 - [MAUI Accessibility Labels]
**Learning:** In MAUI, when a structural element (like `Border`) is made interactive using a `GestureRecognizer` (e.g. `TapGestureRecognizer`), it doesn't automatically get the interactive traits or context that standard `Button` elements get for screen readers. Titles also need explicitly tagged heading levels.
**Action:** Add `SemanticProperties.Hint`, `SemanticProperties.Description`, and `SemanticProperties.HeadingLevel` to explicitly mark non-standard interactive UI and main titles for screen readers.
