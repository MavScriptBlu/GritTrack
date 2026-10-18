## 2024-10-08 - Added SemanticProperties to custom interactive elements
**Learning:** In MAUI, when creating interactive elements using `TapGestureRecognizer` on non-button elements like `Border` or `Label`, they are not automatically announced as interactive by screen readers. Furthermore, main page headings need explicit `HeadingLevel` semantics.
**Action:** Always manually add `SemanticProperties.Hint` and `SemanticProperties.Description` to non-button interactive elements, and `SemanticProperties.HeadingLevel` to page titles.

## 2024-10-08 - Reserved SemanticProperties.HeadingLevel for genuine headings
**Learning:** In .NET MAUI, assigning `SemanticProperties.HeadingLevel` (like Level2 or Level3) to non-heading elements such as form input labels ("Starting GPA") or simple status values clutters the accessibility document outline and degrades the screen reader experience.
**Action:** Only apply `SemanticProperties.HeadingLevel` to actual page or section titles.
