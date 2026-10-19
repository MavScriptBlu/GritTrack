## 2023-10-09 - Chained LINQ Overhead in Services

**Learning:** Chained LINQ operations (e.g., `.Where().ToList().Sum()`) in critical path methods like GPA calculation cause noticeable performance degradation in the MAUI runtime environment due to O(3N) passes and intermediate list heap allocations.

**Action:** Prefer single-pass `foreach` loops for simple aggregations over collections, especially when dealing with hot paths or repeated calculations, to minimize garbage collection pressure and improve execution speed.
## 2024-05-17 - Dictionary Lookups in High-Frequency Paths
**Learning:** Using `.ToUpperInvariant()` or `.ToLower()` to normalize strings before looking them up in a standard Dictionary causes an unnecessary string allocation on the heap for every lookup. In high-frequency paths (like iterating over thousands of grades), this creates noticeable GC pressure and slows execution.
**Action:** When creating lookup dictionaries for known string categories (like grades or statuses), initialize the dictionary with `StringComparer.OrdinalIgnoreCase` (e.g., `new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)`). This completely eliminates the allocation overhead during lookups while maintaining the exact same functional behavior.
