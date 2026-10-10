## 2024-10-08 - Added SemanticProperties to custom interactive elements
**Learning:** In MAUI, when creating interactive elements using `TapGestureRecognizer` on non-button elements like `Border` or `Label`, they are not automatically announced as interactive by screen readers. Furthermore, main page headings need explicit `HeadingLevel` semantics.
**Action:** Always manually add `SemanticProperties.Hint` and `SemanticProperties.Description` to non-button interactive elements, and `SemanticProperties.HeadingLevel` to page titles.

## 2024-10-09 - XAML XML Validation
**Learning:** Duplicate attributes in XAML elements (such as `SemanticProperties.HeadingLevel`, `SemanticProperties.Description` and `SemanticProperties.Hint`) will cause invalid XML which might lead to unexpected behavior and compilation issues in MAUI. A standard `python3` `xml.etree.ElementTree` can be used to quickly validate the XML correctness of XAML files.
**Action:** Always ensure no duplicate attributes are added to XAML elements and use Python script validation to prevent regressions.
