## 2024-10-01 - Chained LINQ operations Performance Optimization
**Learning:** Chained LINQ operations such as `.Where().ToList().Sum()` create significant overhead through multiple O(N) passes and intermediate list heap allocations.
**Action:** Replace multiple LINQ passes with a single `foreach` loop to calculate sums or counts in an O(N) single pass, reducing garbage collection pressure and execution time.
