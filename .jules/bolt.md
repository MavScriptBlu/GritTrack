## 2023-10-09 - Chained LINQ Overhead in Services

**Learning:** Chained LINQ operations (e.g., `.Where().ToList().Sum()`) in critical path methods like GPA calculation cause noticeable performance degradation in the MAUI runtime environment due to O(3N) passes and intermediate list heap allocations.

**Action:** Prefer single-pass `foreach` loops for simple aggregations over collections, especially when dealing with hot paths or repeated calculations, to minimize garbage collection pressure and improve execution speed.

## 2024-05-14 - String Allocation in Case-Insensitive Lookups
**Learning:** Performing case-insensitive string lookups in dictionaries using `.ToUpperInvariant()` or `.ToLower()` inside high-frequency methods (like GPA calculation) causes unnecessary heap allocations and GC pressure.
**Action:** Always initialize case-insensitive dictionaries with `new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase)` to delegate comparison logic without creating intermediate string instances.
