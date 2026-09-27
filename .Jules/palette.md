## 2026-09-27 - MAUI Interactive Elements Accessibility
**Learning:** In .NET MAUI, non-button interactive elements (like Border with a TapGestureRecognizer) require explicit SemanticProperties.Hint and SemanticProperties.Description for screen readers to recognize them properly, and main page titles should declare SemanticProperties.HeadingLevel="Level1".
**Action:** Always add SemanticProperties to custom interactive components and main headers when designing for MAUI.
