## 2024-06-25 - GpaCalculatorService Chained LINQ Bottleneck
**Learning:** Chained LINQ operations (e.g., `.Where(x => ...).ToList()` followed by multiple `.Sum(y => ...)`) are a significant bottleneck for memory allocation in tight calculation loops like GPA and course percentage generation.
**Action:** Replace sequential LINQ filters and reducers with a single `foreach` loop that accumulates all necessary values in one pass (O(N) instead of O(3N)), completely eliminating intermediate List allocations.
