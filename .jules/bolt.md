## 2026-09-29 - Optimize LINQ chaining in GPA calculation
**Learning:** Chained LINQ operations like .Where().ToList() followed by multiple .Sum() calls create significant overhead via O(3N) passes and intermediate list heap allocation in hot paths like GPA calculations.
**Action:** Replace with a single foreach loop to calculate sums in an O(N) pass, avoiding garbage collection pressure.
