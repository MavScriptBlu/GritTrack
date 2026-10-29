## 2023-10-09 - Chained LINQ Overhead in Services

**Learning:** Chained LINQ operations (e.g., `.Where().ToList().Sum()`) in critical path methods like GPA calculation cause noticeable performance degradation in the MAUI runtime environment due to O(3N) passes and intermediate list heap allocations.

**Action:** Prefer single-pass `foreach` loops for simple aggregations over collections, especially when dealing with hot paths or repeated calculations, to minimize garbage collection pressure and improve execution speed.

## 2023-10-30 - Dictionary String Allocation Overhead

**Learning:** Using `.ToUpperInvariant()` or `.ToLower()` during Dictionary string lookups creates unnecessary string allocations and GC pressure, which can degrade performance in hot paths.

**Action:** Initialize case-insensitive lookup dictionaries with `StringComparer.OrdinalIgnoreCase` to avoid runtime string manipulation and minimize heap allocations.
