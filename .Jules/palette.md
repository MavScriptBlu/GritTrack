## 2026-10-02 - Custom Interactive Element Accessibility in MAUI
**Learning:** Non-button interactive elements like Borders and Labels with TapGestureRecognizers lack native screen reader support out of the box in .NET MAUI.
**Action:** Always apply `SemanticProperties.Description` and `SemanticProperties.Hint` to any Border or Label that acts as a button, and ensure main page titles use `SemanticProperties.HeadingLevel='Level1'`.
