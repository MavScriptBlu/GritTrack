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
        private static readonly Dictionary<string, double> GradeTable = new()
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

            return GradeTable.TryGetValue(letterGrade.Trim().ToUpperInvariant(), out var points)
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
            // Bolt Performance Optimization:
            // Replaced .Where().ToList() followed by multiple .Sum() calls with a single foreach loop.
            // Eliminates intermediate list allocations and computes sums in an O(N) pass, reducing GC pressure.
            double earned = 0;
            double possible = 0;
            int count = 0;

            foreach (var a in course.Assignments)
            {
                if (a.PointsEarned.HasValue && a.PointsPossible > 0)
                {
                    earned += a.PointsEarned.Value;
                    possible += a.PointsPossible;
                    count++;
                }
            }

            if (count == 0)
                return null;

            return possible == 0 ? null : (earned / possible) * 100.0;
        }

        /// <summary>Credit-weighted GPA across a course list.</summary>
        public double CalculateGpa(IEnumerable<Course> courses)
        {
            // Bolt Performance Optimization:
            // Replaced chained LINQ (.Where().ToList() and multiple .Sum() calls) with a single foreach loop.
            // Prevents O(3N) passes and heap allocation for the intermediate list, computing sums in one O(N) pass.
            double totalPoints = 0;
            double totalCredits = 0;
            int count = 0;

            foreach (var c in courses)
            {
                if (c.Credits > 0)
                {
                    totalPoints += c.GradePoints * c.Credits;
                    totalCredits += c.Credits;
                    count++;
                }
            }

            if (count == 0)
                return 0.0;

            return totalCredits == 0 ? 0.0 : totalPoints / totalCredits;
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
