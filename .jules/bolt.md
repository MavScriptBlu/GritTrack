## 2023-10-09 - Chained LINQ Overhead in Services

**Learning:** Chained LINQ operations (e.g., `.Where().ToList().Sum()`) in critical path methods like GPA calculation cause noticeable performance degradation in the MAUI runtime environment due to O(3N) passes and intermediate list heap allocations.

**Action:** Prefer single-pass `foreach` loops for simple aggregations over collections, especially when dealing with hot paths or repeated calculations, to minimize garbage collection pressure and improve execution speed.

## 2024-10-11 - Dictionary String Allocations

**Learning:** Using `.ToUpperInvariant()` or `.ToLower()` during Dictionary lookups in high-traffic paths creates unnecessary string allocations and GC pressure. Using a `Dictionary` initialized with `StringComparer.OrdinalIgnoreCase` eliminates the allocation and provides significantly faster lookups.
**Action:** Always initialize case-insensitive lookup dictionaries with `StringComparer.OrdinalIgnoreCase` rather than allocating new strings on every query.
