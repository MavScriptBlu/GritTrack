## 2024-10-08 - Added SemanticProperties to custom interactive elements
**Learning:** In MAUI, when creating interactive elements using `TapGestureRecognizer` on non-button elements like `Border` or `Label`, they are not automatically announced as interactive by screen readers. Furthermore, main page headings need explicit `HeadingLevel` semantics.
**Action:** Always manually add `SemanticProperties.Hint` and `SemanticProperties.Description` to non-button interactive elements, and `SemanticProperties.HeadingLevel` to page titles.
