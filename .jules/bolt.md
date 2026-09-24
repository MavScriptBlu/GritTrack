## 2026-09-24 - Performance Testing in MAUI on Linux
**Learning:** Benchmarking isolated C# code (like `GpaCalculatorService`) by copying it into a temporary console app works around Linux MAUI workload build issues, but the temporary project *must* be deleted before requesting code reviews or committing, as it triggers maintainability failures due to duplicated source files.
**Action:** When creating a temporary benchmarking environment (`dotnet new console`), completely remove it (`rm -rf TempBench`) immediately after gathering the baseline and post-optimization measurements.

## 2026-09-24 - LINQ Overhead in MAUI Services
**Learning:** Chained LINQ operations (e.g., `.Where(x => ...).ToList().Sum(x => ...)` followed by another `.Sum(x => ...)`) in frequently called services (like `GpaCalculatorService`) create measurable overhead (O(3N) pass, heap allocation for `ToList()`).
**Action:** Replace multiple LINQ passes with a single `foreach` loop that accumulates all necessary totals in one O(N) pass, dropping allocations from ~328B to 0B.
