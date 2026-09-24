## 2026-09-21 - [Date access optimization]
**Learning:** Replacing repeated DateTime.Today and DueDate.Date accesses with local variables within a getter provides a considerable performance bump due to bypassing multiple system clock checks and date conversions.
**Action:** Always capture DateTime.Now/Today and other time-sensitive or relatively expensive properties into local variables if used multiple times in the same logical evaluation.
