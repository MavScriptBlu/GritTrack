## 2024-10-01 - Add SemanticProperties to non-button interactive elements
**Learning:** In .NET MAUI, elements like `Border` or `Label` with `TapGestureRecognizer` need explicit `SemanticProperties.Hint` and `SemanticProperties.Description` for screen readers to recognize them as interactive.
**Action:** Always add semantic properties to custom interactive UI elements that aren't native Buttons.
