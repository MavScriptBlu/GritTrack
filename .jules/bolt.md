## 2023-10-25 - LINQ allocation bottleneck in service methods
**Learning:** In C# .NET, using LINQ methods like `.Where().ToList()` inside a method that is frequently called creates an array/list allocation each time and requires multiple O(N) passes (e.g. `Sum()` called multiple times after). This causes significant garbage collection pressure (8.1 KB for 1000 items).
**Action:** Replace multiple chained LINQ extensions (like `.Where().ToList()` then `.Sum()`) with a single `foreach` loop that performs calculations in a single pass without allocating intermediate lists or arrays.
