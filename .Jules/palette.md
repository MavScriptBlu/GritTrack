## 2026-09-23 - Accessibility properties on non-button MAUI elements
**Learning:** In MAUI, non-button interactive elements like Borders with a TapGestureRecognizer need SemanticProperties.Hint and SemanticProperties.Description for screen readers to announce them properly. Also, main page titles should have SemanticProperties.HeadingLevel="Level1".
**Action:** Always add SemanticProperties to custom interactive elements in XAML.
