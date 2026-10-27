## 2023-10-09 - Chained LINQ Overhead in Services

**Learning:** Chained LINQ operations (e.g., `.Where().ToList().Sum()`) in critical path methods like GPA calculation cause noticeable performance degradation in the MAUI runtime environment due to O(3N) passes and intermediate list heap allocations.

**Action:** Prefer single-pass `foreach` loops for simple aggregations over collections, especially when dealing with hot paths or repeated calculations, to minimize garbage collection pressure and improve execution speed.

## 2023-10-24 - OrdinalIgnoreCase for Lookup Optimizations
**Learning:** During GPA calculation, mapping string letter grades to points using `.ToUpperInvariant()` before dictionary lookup forces an unnecessary string allocation on every single lookup. In tight loops or large iterations, this increases GC pressure and decreases execution speed significantly.
**Action:** Initialize standard string-keyed lookups (like static grade tables) using `new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)`. This pushes case-insensitivity down into the dictionary's internal hashing algorithm, entirely avoiding the need for per-request intermediate string allocations.
