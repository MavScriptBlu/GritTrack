## 2024-05-18 - LINQ Allocation Overhead Replacement
**Learning:** In C#, chained LINQ operations like `.Where().ToList()` followed by multiple `.Sum()` calls over the resulting list creates significant overhead via O(3N) passes and heap allocation for the intermediate list.
**Action:** When performing multiple aggregations over the same filtered dataset, replace chained LINQ passes and `.ToList()` allocations with a single `foreach` loop to calculate all sums in an O(N) pass, avoiding garbage collection pressure.
