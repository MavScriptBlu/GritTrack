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
            double earned = 0;
            double possible = 0;
            bool hasGraded = false;

            // ⚡ Bolt: Replaced chained LINQ operations (.Where.ToList followed by multiple .Sum)
            // with a single foreach loop. This avoids intermediate list heap allocation
            // and reduces passes from O(3N) to O(N), significantly lowering GC pressure.
            foreach (var a in course.Assignments)
            {
                if (a.PointsEarned.HasValue && a.PointsPossible > 0)
                {
                    hasGraded = true;
                    earned += a.PointsEarned.Value;
                    possible += a.PointsPossible;
                }
            }

            if (!hasGraded || possible == 0)
                return null;

            return (earned / possible) * 100.0;
        }

        /// <summary>GPA across all courses — bigger classes count for more, same as a real transcript.</summary>
        public double CalculateGpa(IEnumerable<Course> courses)
        {
            double totalPoints = 0;
            double totalCredits = 0;

            // ⚡ Bolt: Replaced O(3N) LINQ passes with a single O(N) foreach loop
            // to eliminate heap allocation overhead and improve speed.
            foreach (var c in courses)
            {
                if (c.Credits > 0)
                {
                    totalPoints += c.GradePoints * c.Credits;
                    totalCredits += c.Credits;
                }
            }

            return totalCredits == 0 ? 0.0 : totalPoints / totalCredits;
        }

        /// <summary>Same as above, but also blends in a starting GPA from before this app (past semesters, transfer credits).</summary>
        public double CalculateGpa(IEnumerable<Course> courses, double startingGpa, double startingCredits)
        {
            double totalPoints = 0;
            double totalCredits = 0;

            // ⚡ Bolt: Replaced O(3N) LINQ passes with a single O(N) foreach loop
            // to eliminate heap allocation overhead and improve speed.
            foreach (var c in courses)
            {
                if (c.Credits > 0)
                {
                    totalPoints += c.GradePoints * c.Credits;
                    totalCredits += c.Credits;
                }
            }

            // only blend it in if there's an actual credit count behind it
            if (startingCredits > 0)
            {
                totalPoints += startingGpa * startingCredits;
                totalCredits += startingCredits;
            }

            return totalCredits == 0 ? 0.0 : totalPoints / totalCredits;
        }

        /// <summary>
        /// PTK has two real numbers, not one: 3.5 to get invited, 3.0 to stay
        /// a member in good standing (per MSTC's Beta Chi Theta bylaws).
        /// Safe = at/above the invite GPA. Watch = a member in good standing
        /// but below the invite bar. AtRisk = below good standing, membership
        /// is actually on the line.
        /// </summary>
        public GpaStatus GetStatus(double currentGpa, double goodStandingGpa, double initiationGpa)
        {
            if (currentGpa >= initiationGpa)
                return GpaStatus.Safe;

            if (currentGpa >= goodStandingGpa)
                return GpaStatus.Watch;

            return GpaStatus.AtRisk;
        }
    }
}
