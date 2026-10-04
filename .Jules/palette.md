## 2024-05-18 - Semantic Properties for Interactive Elements
**Learning:** In .NET MAUI, non-button interactive elements (like `Border` and `Label` with `TapGestureRecognizer`) require explicit `SemanticProperties.Hint` and `SemanticProperties.Description` to be accessible to screen readers, and main page titles need `SemanticProperties.HeadingLevel="Level1"`.
**Action:** Always verify that elements with `TapGestureRecognizer` have appropriate `SemanticProperties` applied when creating or modifying custom interactive controls in MAUI.
