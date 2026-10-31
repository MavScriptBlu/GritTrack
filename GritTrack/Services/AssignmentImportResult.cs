using GritTrack.Models;

namespace GritTrack.Services
{
    // what an import did: how many went in, how many were skipped as
    // duplicates, and the ones that actually got added
    public record AssignmentImportResult(
        int AddedCount,
        int SkippedCount,
        List<Assignment> AddedAssignments);

    // one place that decides "same assignment". same title (trimmed, ignoring
    // case) on the same due DAY (time of day doesn't count) = duplicate
    public static class AssignmentDuplicateRule
    {
        public static (string TitleKey, DateTime DueDay) KeyOf(Assignment assignment)
        {
            return (assignment.Title.Trim().ToLowerInvariant(), assignment.DueDate.Date);
        }

        // keeps the new ones. checks against what's already there AND against
        // earlier lines in the same batch, because HashSet.Add says false
        // when the key is already in the set
        public static AssignmentImportResult Filter(IEnumerable<Assignment> existing, IEnumerable<Assignment> incoming)
        {
            var knownKeys = new HashSet<(string TitleKey, DateTime DueDay)>(existing.Select(KeyOf));
            var added = new List<Assignment>();
            var skippedCount = 0;

            foreach (var assignment in incoming)
            {
                // a blank title can't be matched, so it never gets in
                if (string.IsNullOrWhiteSpace(assignment.Title) || !knownKeys.Add(KeyOf(assignment)))
                {
                    skippedCount++;
                    continue;
                }

                added.Add(assignment);
            }

            return new AssignmentImportResult(added.Count, skippedCount, added);
        }
    }
}
