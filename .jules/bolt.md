## 2023-10-09 - Chained LINQ Overhead in Services

**Learning:** Chained LINQ operations (e.g., `.Where().ToList().Sum()`) in critical path methods like GPA calculation cause noticeable performance degradation in the MAUI runtime environment due to O(3N) passes and intermediate list heap allocations.

**Action:** Prefer single-pass `foreach` loops for simple aggregations over collections, especially when dealing with hot paths or repeated calculations, to minimize garbage collection pressure and improve execution speed.

## 2023-10-25 - Dictionary String Lookups

**Learning:** Using `.ToUpperInvariant()` or `.ToLower()` repeatedly during Dictionary lookups (like in `GpaCalculatorService.GetGradePoints`) creates unnecessary string allocations on the heap, increasing Garbage Collection pressure when called frequently or in loops.

**Action:** When a dictionary uses string keys and requires case-insensitive lookups, always initialize it with a case-insensitive equality comparer (e.g., `new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)`) to eliminate the overhead of explicitly mutating the string before the lookup.
