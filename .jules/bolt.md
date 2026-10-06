
## 2023-10-24 - [Replaced LINQ chains with single foreach]
**Learning:** Found significant overhead in chained LINQ operations like `.Where().ToList()` followed by `.Sum()` in C# which creates intermediate list allocations and results in O(3N) passes.
**Action:** Replace these LINQ chains with a single `foreach` loop to iterate only once and avoid unnecessary heap allocations, improving GC pressure and overall speed.
