## 2024-10-08 - Added SemanticProperties to custom interactive elements
**Learning:** In MAUI, when creating interactive elements using `TapGestureRecognizer` on non-button elements like `Border` or `Label`, they are not automatically announced as interactive by screen readers. Furthermore, main page headings need explicit `HeadingLevel` semantics.
**Action:** Always manually add `SemanticProperties.Hint` and `SemanticProperties.Description` to non-button interactive elements, and `SemanticProperties.HeadingLevel` to page titles.

## 2024-10-25 - Fixing duplicated SemanticProperties in MAUI XAML
**Learning:** MAUI XAML parser fails with `xml.etree.ElementTree.ParseError: duplicate attribute` if `SemanticProperties` (e.g., `HeadingLevel`, `Hint`) are duplicated on the same element. Diff-based tools may flag the deletion of duplicates as accessibility regressions.
**Action:** When fixing duplicated `SemanticProperties`, ensure you keep exactly ONE of them. Modify the indentation of the entire XAML element block in your patch so it registers as a full replacement, making it clear to code review that the property is being retained.
