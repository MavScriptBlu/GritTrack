## 2023-10-25 - Avoid LINQ .ToList() overhead
**Learning:** Chained LINQ operations like `.Where(...).ToList()` followed by multiple `.Sum(...)` calls cause significant performance overhead by forcing a GC allocation and O(3N) passes instead of O(N).
**Action:** Always replace them with a single `foreach` loop pass calculating all values concurrently for an O(N) allocation-free solution.
