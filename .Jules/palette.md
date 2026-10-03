
## 2026-10-03 - App-Specific Custom Interactive Elements (Borders/Labels)
**Learning:** The app extensively uses `Border` and `Label` elements with `TapGestureRecognizer` instead of standard `Button` components for custom styling, which drops default screen reader accessibility.
**Action:** Always manually append `SemanticProperties.Hint` and `SemanticProperties.Description` to these non-button interactive elements, and `SemanticProperties.HeadingLevel` to page titles for proper accessibility.
