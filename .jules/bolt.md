## 2024-05-24 - Avoid Chained LINQ Allocations in GpaCalculatorService

**Learning:** `IEnumerable` processing that chains `.Where(...).ToList()` and multiple `.Sum()` calls inside `GpaCalculatorService` leads to multiple passes over collections and unnecessary List allocations in heap memory. This is particularly wasteful when we can simultaneously compute intermediate states (credits and points) via a simple iterator.

**Action:** Replaced chained LINQ operations with a single `foreach` block when dealing with simultaneous aggregation on lists. This prevents O(N) memory allocations and reduces calculation loops from O(3N) to O(N). Future optimizations over lists in calculation methods should follow this standard pattern.
