## 2023-10-07 - Add SemanticProperties to non-button interactive elements
**Learning:** In MAUI, non-button interactive elements (e.g. labels or borders with a `TapGestureRecognizer`) require `SemanticProperties.Hint` and `SemanticProperties.Description` for screen readers to properly announce them as interactive.
**Action:** Always add `SemanticProperties` to custom interactive elements.
