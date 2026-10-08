## 2024-10-08 - Benchmarking isolated code due to environment limits
**Learning:** MAUI workloads aren't fully supported in the Linux container environment, making full application builds fail. However, isolated business logic (like services and models) can be successfully profiled by copying them into a temporary `dotnet new console` project.
**Action:** Next time, when optimizing pure logic without UI dependencies, quickly spin up a temporary console app to establish a baseline instead of fighting with MAUI workload errors on the main project.

## 2024-10-08 - Chained LINQ Overhead Avoidance
**Learning:** `.Where(…).ToList()` followed by `.Sum(…)` is a common anti-pattern that creates unnecessary intermediate collections in memory and performs multiple passes over the data.
**Action:** When calculating multiple aggregates or checking conditions concurrently, replace chained LINQ with a single `foreach` loop to reduce heap allocations and complete the operation in O(N) time.
