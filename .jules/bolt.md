## 2023-10-09 - Chained LINQ Overhead in Services

**Learning:** Chained LINQ operations (e.g., `.Where().ToList().Sum()`) in critical path methods like GPA calculation cause noticeable performance degradation in the MAUI runtime environment due to O(3N) passes and intermediate list heap allocations.

**Action:** Prefer single-pass `foreach` loops for simple aggregations over collections, especially when dealing with hot paths or repeated calculations, to minimize garbage collection pressure and improve execution speed.

## 2023-10-30 - Dictionary String Allocation Overhead

**Learning:** Using `.ToUpperInvariant()` or `.ToLower()` repeatedly during Dictionary lookups creates unnecessary string allocations on the heap, increasing Garbage Collection pressure, especially in hot paths like iterating over grades.

**Action:** Initialize lookups for predefined string keys using case-insensitive comparers (e.g., `new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)`) to completely avoid the allocation overhead of converting casing per-lookup.
