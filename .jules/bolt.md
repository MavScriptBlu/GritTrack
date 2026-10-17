## 2023-10-09 - Chained LINQ Overhead in Services

**Learning:** Chained LINQ operations (e.g., `.Where().ToList().Sum()`) in critical path methods like GPA calculation cause noticeable performance degradation in the MAUI runtime environment due to O(3N) passes and intermediate list heap allocations.

**Action:** Prefer single-pass `foreach` loops for simple aggregations over collections, especially when dealing with hot paths or repeated calculations, to minimize garbage collection pressure and improve execution speed.

## 2023-10-25 - Dictionary Initialization with OrdinalIgnoreCase

**Learning:** Initializing dictionaries for string lookups with `.ToUpperInvariant()` or `.ToLower()` during key searches causes an allocation on every lookup, creating significant GC pressure in C# MAUI apps. Using `StringComparer.OrdinalIgnoreCase` eliminates this by performing case-insensitive matching without allocating new string instances.

**Action:** When initializing dictionaries intended for case-insensitive string lookups, always construct them with `new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase)` and remove explicit casing methods like `.ToUpperInvariant()` at the lookup site.
