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
            var graded = course.Assignments.Where(a => a.PointsEarned.HasValue && a.PointsPossible > 0).ToList();
            if (graded.Count == 0)
                return null;

            var earned = graded.Sum(a => a.PointsEarned!.Value);
            var possible = graded.Sum(a => a.PointsPossible);
            return possible == 0 ? null : (earned / possible) * 100.0;
        }

        /// <summary>GPA across all courses — bigger classes count for more, same as a real transcript.</summary>
        public double CalculateGpa(IEnumerable<Course> courses)
        {
            // skip 0-credit courses so they don't skew the average
            var list = courses.Where(c => c.Credits > 0).ToList();
            if (list.Count == 0)
                return 0.0;

            var totalPoints = list.Sum(c => c.GradePoints * c.Credits);
            var totalCredits = list.Sum(c => c.Credits);
            return totalCredits == 0 ? 0.0 : totalPoints / totalCredits;
        }

        /// <summary>Same as above, but also blends in a starting GPA from before this app (past semesters, transfer credits).</summary>
        public double CalculateGpa(IEnumerable<Course> courses, double startingGpa, double startingCredits)
        {
            var list = courses.Where(c => c.Credits > 0).ToList();

            var totalPoints = list.Sum(c => c.GradePoints * c.Credits);
            var totalCredits = list.Sum(c => c.Credits);

            // only blend it in if there's an actual credit count behind it
            if (startingCredits > 0)
            {
                totalPoints += startingGpa * startingCredits;
                totalCredits += startingCredits;
            }

            return totalCredits == 0 ? 0.0 : totalPoints / totalCredits;
        }

        /// <summary>PTK status: Safe well above target, Watch within 0.15 of it, AtRisk below it.</summary>
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
