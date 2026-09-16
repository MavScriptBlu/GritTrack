## 2024-09-17 - MAUI LINQ Overhead
**Learning:** Found widespread use of chained LINQ methods (`.Where().ToList().Sum()`) in critical calculations (`GpaCalculatorService`). This creates unnecessary O(3N) iteration and intermediate heap allocations on mobile devices where memory pressure is critical.
**Action:** Replace chained LINQ aggregations in frequently called calculations with single-pass `foreach` loops to eliminate intermediate allocations and reduce iteration complexity.
