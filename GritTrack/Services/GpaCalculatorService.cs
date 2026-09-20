using GritTrack.Models;

namespace GritTrack.Services
{
    public enum GpaStatus
    {
        Safe,
        Watch,
        AtRisk
    }

    public class GpaCalculatorService
    {
        // ⚡ Bolt: Use StringComparer.OrdinalIgnoreCase to avoid ToUpperInvariant string allocations
        private static readonly Dictionary<string, double> GradeTable = new(StringComparer.OrdinalIgnoreCase)
        {
            { "A",  4.0 }, { "A-", 3.7 },
            { "B+", 3.3 }, { "B",  3.0 }, { "B-", 2.7 },
            { "C+", 2.3 }, { "C",  2.0 }, { "C-", 1.7 },
            { "D+", 1.3 }, { "D",  1.0 }, { "D-", 0.7 },
            { "F",  0.0 }
        };

        public double GetGradePoints(string letterGrade)
        {
            if (string.IsNullOrWhiteSpace(letterGrade))
                return 0.0;

            return GradeTable.TryGetValue(letterGrade.Trim(), out var points)
                ? points
                : 0.0;
        }

        public string GetLetterGradeFromPercent(double percent) => percent switch
        {
            >= 93 => "A",
            >= 90 => "A-",
            >= 87 => "B+",
            >= 83 => "B",
            >= 80 => "B-",
            >= 77 => "C+",
            >= 73 => "C",
            >= 70 => "C-",
            >= 67 => "D+",
            >= 63 => "D",
            >= 60 => "D-",
            _ => "F"
        };

        public double? GetCoursePercent(Course course)
        {
            // ⚡ Bolt: Replace multiple LINQ passes and list allocation with a single loop
            double earned = 0.0;
            double possible = 0.0;
            bool hasGraded = false;

            foreach (var a in course.Assignments)
            {
                if (a.PointsEarned.HasValue && a.PointsPossible > 0)
                {
                    earned += a.PointsEarned.Value;
                    possible += a.PointsPossible;
                    hasGraded = true;
                }
            }

            if (!hasGraded || possible == 0)
                return null;

            return (earned / possible) * 100.0;
        }

        /// <summary>Credit-weighted GPA across a course list.</summary>
        public double CalculateGpa(IEnumerable<Course> courses)
        {
            // ⚡ Bolt: Replace multiple LINQ passes and list allocation with a single loop
            double totalPoints = 0.0;
            double totalCredits = 0.0;
            bool hasCredits = false;

            foreach (var c in courses)
            {
                if (c.Credits > 0)
                {
                    totalPoints += c.GradePoints * c.Credits;
                    totalCredits += c.Credits;
                    hasCredits = true;
                }
            }

            if (!hasCredits || totalCredits == 0)
                return 0.0;

            return totalPoints / totalCredits;
        }

        /// <summary>
        /// PTK status: Safe if comfortably above target, Watch if close,
        /// AtRisk if below. "Close" = within 0.15 of the target GPA.
        /// </summary>
        public GpaStatus GetStatus(double currentGpa, double targetGpa)
        {
            if (currentGpa < targetGpa)
                return GpaStatus.AtRisk;

            if (currentGpa - targetGpa <= 0.15)
                return GpaStatus.Watch;

            return GpaStatus.Safe;
        }
    }
}
