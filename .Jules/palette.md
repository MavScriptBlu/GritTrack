## 2024-09-17 - .NET MAUI Accessibility Fundamentals
**Learning:** .NET MAUI provides SemanticProperties specifically for screen readers. Notably:
1. When applying a `TapGestureRecognizer` to a non-button element (like a `Border` acting as a card), screen readers don't natively understand it's interactive. Adding `SemanticProperties.Hint` informs the user what tapping does.
2. Main page titles (usually large headers) aren't automatically recognized as structural headings by screen readers. Adding `SemanticProperties.HeadingLevel="Level1"` drastically improves navigation.
**Action:** Always add semantic properties for interactive containers and major headings in XAML pages.