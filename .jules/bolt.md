## 2023-10-09 - Chained LINQ Overhead in Services

**Learning:** Chained LINQ operations (e.g., `.Where().ToList().Sum()`) in critical path methods like GPA calculation cause noticeable performance degradation in the MAUI runtime environment due to O(3N) passes and intermediate list heap allocations.

**Action:** Prefer single-pass `foreach` loops for simple aggregations over collections, especially when dealing with hot paths or repeated calculations, to minimize garbage collection pressure and improve execution speed.
## 2024-05-14 - Dictionary Lookups String Allocations

**Learning:** Using `.ToUpperInvariant()` or `.ToLower()` during Dictionary string lookups creates unnecessary string allocations and GC pressure, which can become a bottleneck in hot paths.

**Action:** To eliminate this overhead, initialize case-insensitive lookup dictionaries with `StringComparer.OrdinalIgnoreCase` (e.g., `new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)`).
