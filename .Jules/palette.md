## 2024-05-24 - Accessible Interactive Borders
**Learning:** Non-button interactive elements (e.g., borders with TapGestureRecognizer) require explicit SemanticProperties (Hint, Description) for screen readers, and page titles need HeadingLevel="Level1" to be properly announced as main headings.
**Action:** Always apply SemanticProperties to custom clickable elements instead of relying on default behavior.
