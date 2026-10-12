## 2023-10-09 - Chained LINQ Overhead in Services

**Learning:** Chained LINQ operations (e.g., `.Where().ToList().Sum()`) in critical path methods like GPA calculation cause noticeable performance degradation in the MAUI runtime environment due to O(3N) passes and intermediate list heap allocations.

**Action:** Prefer single-pass `foreach` loops for simple aggregations over collections, especially when dealing with hot paths or repeated calculations, to minimize garbage collection pressure and improve execution speed.

## 2024-05-18 - Dictionary lookup performance with ToUpperInvariant
**Learning:** Using `.ToUpperInvariant()` or `.ToLower()` during Dictionary string lookups in C# creates unnecessary intermediate string allocations and GC pressure. This overhead becomes significant in hot paths or tight loops.
**Action:** When creating dictionaries intended for case-insensitive string lookups, initialize them with `StringComparer.OrdinalIgnoreCase` (e.g., `new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)`) to eliminate allocation overhead during `TryGetValue` or indexer access.
