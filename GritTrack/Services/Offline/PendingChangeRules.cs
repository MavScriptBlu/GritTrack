using GritTrack.Models;

namespace GritTrack.Services.Offline
{
    // the rules for the waiting list. kept apart from files and the network
    // so they're easy to test
    public static class PendingChangeRules
    {
        // adds a change, keeping at most ONE entry per course:
        //   - edit a course that's still waiting to be created -> the create just gets the new details
        //   - edit twice -> only the last edit is kept
        //   - delete a course that never reached the server -> it just disappears from the list
        // gives back the course as queued (a new one gets its temporary negative ID here)
        public static Course Add(List<PendingChange> queue, PendingChangeKind kind, Course course)
        {
            if (kind == PendingChangeKind.Create)
            {
                course.ID = NextTemporaryId(queue);
                queue.Add(new PendingChange { Kind = PendingChangeKind.Create, Course = course });
                return course;
            }

            var existing = queue.FirstOrDefault(change => change.Course.ID == course.ID);

            if (kind == PendingChangeKind.Delete)
            {
                if (existing is not null)
                    queue.Remove(existing);

                // a course that only ever lived on this phone has nothing to delete on the server
                if (existing?.Kind != PendingChangeKind.Create)
                    queue.Add(new PendingChange { Kind = PendingChangeKind.Delete, Course = course });

                return course;
            }

            if (existing is null)
            {
                queue.Add(new PendingChange { Kind = PendingChangeKind.Update, Course = course });
            }
            else
            {
                // keep the kind (a Create stays a Create), swap in the new details
                existing.Course = course;
                existing.Problem = null;
            }

            return course;
        }

        // what the list should show: the server's courses with the waiting
        // changes laid on top, so nothing the user did offline goes missing
        public static List<Course> ApplyTo(IEnumerable<Course> serverCourses, IReadOnlyList<PendingChange> queue)
        {
            var courses = serverCourses.ToList();

            foreach (var change in queue)
            {
                var index = courses.FindIndex(course => course.ID == change.Course.ID);

                if (change.Kind == PendingChangeKind.Delete)
                {
                    if (index >= 0)
                        courses.RemoveAt(index);
                    continue;
                }

                var waiting = Copy(change.Course);
                waiting.IsPendingSync = true;

                if (index >= 0)
                    courses[index] = waiting;
                else
                    courses.Add(waiting);
            }

            return courses;
        }

        // a copy with no assignments (those are stored separately)
        public static Course Copy(Course course) => new()
        {
            ID = course.ID,
            Name = course.Name,
            CourseCode = course.CourseCode,
            Instructor = course.Instructor,
            Credits = course.Credits,
            CurrentGrade = course.CurrentGrade,
            GradePoints = course.GradePoints
        };

        // -1, then -2, ... real IDs from the server are always above 0, so these can't clash
        private static int NextTemporaryId(List<PendingChange> queue)
        {
            var lowest = queue.Count == 0 ? 0 : Math.Min(0, queue.Min(change => change.Course.ID));
            return lowest - 1;
        }
    }
}
