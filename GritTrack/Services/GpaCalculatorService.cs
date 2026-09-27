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

        /// <summary>
        /// Works out the GPA for a list of courses. Bigger classes (more
        /// credits) count for more, same as a real transcript.
        /// </summary>
        public double CalculateGpa(IEnumerable<Course> courses)
        {
            // only count courses that actually have credits set — a course
            // with 0 credits shouldn't drag the average down or up
            var list = courses.Where(c => c.Credits > 0).ToList();
            if (list.Count == 0)
                return 0.0;

            // "points" here means grade points times credits — the school
            // math for GPA. add it all up, divide by total credits.
            var totalPoints = list.Sum(c => c.GradePoints * c.Credits);
            var totalCredits = list.Sum(c => c.Credits);
            return totalCredits == 0 ? 0.0 : totalPoints / totalCredits;
        }

        /// <summary>
        /// Same GPA math as above, but also mixes in a starting GPA the
        /// student already had before they started using this app — like
        /// if you're a 2nd-year student and already have a GPA from past
        /// semesters. Both the old GPA and the new courses get weighted by
        /// how many credits they're each worth, then averaged together.
        /// </summary>
        public double CalculateGpa(IEnumerable<Course> courses, double startingGpa, double startingCredits)
        {
            var list = courses.Where(c => c.Credits > 0).ToList();

            var totalPoints = list.Sum(c => c.GradePoints * c.Credits);
            var totalCredits = list.Sum(c => c.Credits);

            // only mix in the starting GPA if there are actual credits
            // behind it — otherwise it'd be adding zero credits worth of
            // "weight" and messing with the math for no reason
            if (startingCredits > 0)
            {
                totalPoints += startingGpa * startingCredits;
                totalCredits += startingCredits;
            }

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
