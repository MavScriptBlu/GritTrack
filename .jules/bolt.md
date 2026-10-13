## 2023-10-09 - Chained LINQ Overhead in Services

**Learning:** Chained LINQ operations (e.g., `.Where().ToList().Sum()`) in critical path methods like GPA calculation cause noticeable performance degradation in the MAUI runtime environment due to O(3N) passes and intermediate list heap allocations.

**Action:** Prefer single-pass `foreach` loops for simple aggregations over collections, especially when dealing with hot paths or repeated calculations, to minimize garbage collection pressure and improve execution speed.

## 2023-10-24 - Case-Insensitive Dictionary Lookup Overhead
**Learning:** Using `.ToUpperInvariant()` or `.ToLower()` during Dictionary string lookups in C# creates unnecessary string allocations and garbage collection pressure, measurable even in small applications on a tight loop (roughly ~15-20% slower for lookups).
**Action:** Initialize lookups with `new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase)` and remove `.ToUpperInvariant()` to avoid extra memory allocation.
