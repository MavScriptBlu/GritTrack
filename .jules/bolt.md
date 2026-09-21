## 2024-10-26 - [LINQ Optimization]
**Learning:** Replaced multiple chained LINQ operations (`.Where().ToList()` followed by multiple `.Sum()`) with a single `foreach` loop to eliminate redundant passes over the same list and avoid an intermediate list allocation.
**Action:** Always prefer a single pass `foreach` over multi-pass LINQ methods when computing multiple aggregates over the same sequence.
