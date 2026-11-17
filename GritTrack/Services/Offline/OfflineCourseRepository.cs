using GritTrack.Models;
using GritTrack.Services.Api;

namespace GritTrack.Services.Offline
{
    // wraps the real API repository and adds offline mode. when the server
    // can't be reached:
    //   - loading shows the last list we got, saved on the phone
    //   - saving or deleting goes on a waiting list instead of failing
    // Sync sends the waiting list later. everything else in the app still
    // just sees an ICourseRepository
    public class OfflineCourseRepository : ICourseRepository, ISyncService
    {
        private readonly ICourseRepository _inner;
        private readonly JsonFile<List<PendingChange>> _pendingFile;
        private readonly JsonFile<List<Course>> _cacheFile;
        private readonly Func<string, double> _gradePointsFor;
        private readonly Func<int, int, Task>? _onCourseIdAssigned;

        private readonly SemaphoreSlim _gate = new(1, 1);
        private List<PendingChange>? _pending;

        // gradePointsFor: letter grade -> points, because offline there's no server to work it out
        // onCourseIdAssigned: (temporary id, real id) once a course made offline reaches the server
        public OfflineCourseRepository(
            ICourseRepository inner,
            JsonFile<List<PendingChange>> pendingFile,
            JsonFile<List<Course>> cacheFile,
            Func<string, double> gradePointsFor,
            Func<int, int, Task>? onCourseIdAssigned = null)
        {
            _inner = inner;
            _pendingFile = pendingFile;
            _cacheFile = cacheFile;
            _gradePointsFor = gradePointsFor;
            _onCourseIdAssigned = onCourseIdAssigned;
        }

        public bool IsOffline { get; private set; }

        public int PendingCount => _pending?.Count ?? 0;

        public IReadOnlyList<string> Problems => _pending is null
            ? Array.Empty<string>()
            : _pending.Where(change => change.Problem is not null)
                .Select(change => $"{change.Course.CourseCode}: {change.Problem}")
                .ToList();

        public event EventHandler? Changed;

        public async Task<List<Course>> GetCoursesAsync()
        {
            List<Course> courses;

            await _gate.WaitAsync();
            try
            {
                var pending = await LoadPendingAsync();
                List<Course> serverCourses;

                try
                {
                    serverCourses = await _inner.GetCoursesAsync();
                    IsOffline = false;
                    await _cacheFile.WriteAsync(serverCourses.Select(PendingChangeRules.Copy).ToList());
                }
                catch (ApiUnreachableException)
                {
                    serverCourses = await _cacheFile.ReadAsync();
                    IsOffline = true;
                }

                courses = PendingChangeRules.ApplyTo(serverCourses, pending);

                // courses from the saved copy or the waiting list come without assignments
                foreach (var course in courses.Where(course => course.Assignments.Count == 0))
                    course.Assignments = await _inner.GetAssignmentsLocalAsync(course.ID);
            }
            finally
            {
                _gate.Release();
            }

            Changed?.Invoke(this, EventArgs.Empty);
            return courses;
        }

        public async Task<Course> SaveCourseAsync(Course course)
        {
            Course saved;

            await _gate.WaitAsync();
            try
            {
                var pending = await LoadPendingAsync();
                var isNew = course.ID == 0;

                // a negative ID means the server has never seen this course, so
                // there's nothing to call: just update what's waiting
                if (course.ID < 0)
                {
                    saved = await QueueAsync(pending, PendingChangeKind.Update, course);
                }
                else
                {
                    try
                    {
                        saved = await _inner.SaveCourseAsync(course);
                        IsOffline = false;

                        // this save is newer than anything that was waiting for the same course
                        if (pending.RemoveAll(change => change.Course.ID == saved.ID) > 0)
                            await _pendingFile.WriteAsync(pending);

                        await UpdateCacheAsync(cache =>
                        {
                            cache.RemoveAll(cached => cached.ID == saved.ID);
                            cache.Add(PendingChangeRules.Copy(saved));
                        });
                    }
                    catch (ApiUnreachableException)
                    {
                        IsOffline = true;
                        saved = await QueueAsync(pending, isNew ? PendingChangeKind.Create : PendingChangeKind.Update, course);
                    }
                }
            }
            finally
            {
                _gate.Release();
            }

            Changed?.Invoke(this, EventArgs.Empty);
            return saved;
        }

        public async Task DeleteCourseAsync(int courseId)
        {
            await _gate.WaitAsync();
            try
            {
                var pending = await LoadPendingAsync();

                if (courseId < 0)
                {
                    await QueueAsync(pending, PendingChangeKind.Delete, new Course { ID = courseId });
                }
                else
                {
                    try
                    {
                        await _inner.DeleteCourseAsync(courseId);
                        IsOffline = false;

                        if (pending.RemoveAll(change => change.Course.ID == courseId) > 0)
                            await _pendingFile.WriteAsync(pending);

                        await UpdateCacheAsync(cache => cache.RemoveAll(cached => cached.ID == courseId));
                    }
                    catch (ApiUnreachableException)
                    {
                        IsOffline = true;

                        var cached = (await _cacheFile.ReadAsync()).FirstOrDefault(course => course.ID == courseId);
                        await QueueAsync(pending, PendingChangeKind.Delete, cached ?? new Course { ID = courseId });
                    }
                }
            }
            finally
            {
                _gate.Release();
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        // assignments already live on the phone, so these go straight through
        public Task<List<Assignment>> GetAssignmentsLocalAsync(int courseId)
            => _inner.GetAssignmentsLocalAsync(courseId);

        public Task<AssignmentImportResult> SaveAssignmentsLocalAsync(int courseId, IEnumerable<Assignment> assignments)
            => _inner.SaveAssignmentsLocalAsync(courseId, assignments);

        public Task UpdateAssignmentLocalAsync(int courseId, Assignment assignment)
            => _inner.UpdateAssignmentLocalAsync(courseId, assignment);

        public async Task<SyncResult> SyncAsync()
        {
            var sentCount = 0;
            var problemCount = 0;
            var waitingCount = 0;
            var stillOffline = false;

            await _gate.WaitAsync();
            try
            {
                var pending = await LoadPendingAsync();

                // go through a copy, because changes come off the real list as they go through
                foreach (var change in pending.ToList())
                {
                    var outcome = await TrySendAsync(change);

                    if (outcome == SendOutcome.Sent)
                        sentCount++;

                    if (outcome is SendOutcome.Sent or SendOutcome.AlreadyGone)
                        pending.Remove(change);

                    // save after every change, so a sync that stops halfway never sends one twice
                    await _pendingFile.WriteAsync(pending);

                    if (outcome == SendOutcome.ServerUnreachable)
                    {
                        // still no server. stop here, the rest keeps waiting
                        stillOffline = true;
                        break;
                    }
                }

                IsOffline = stillOffline;
                problemCount = pending.Count(change => change.Problem is not null);
                waitingCount = pending.Count - problemCount;
            }
            finally
            {
                _gate.Release();
                Changed?.Invoke(this, EventArgs.Empty);
            }

            return new SyncResult(sentCount, problemCount, waitingCount, stillOffline);
        }

        public async Task<int> DiscardProblemsAsync()
        {
            int removed;

            await _gate.WaitAsync();
            try
            {
                var pending = await LoadPendingAsync();
                removed = pending.RemoveAll(change => change.Problem is not null);
                if (removed > 0)
                    await _pendingFile.WriteAsync(pending);
            }
            finally
            {
                _gate.Release();
            }

            Changed?.Invoke(this, EventArgs.Empty);
            return removed;
        }

        private enum SendOutcome
        {
            Sent,
            AlreadyGone,       // 404: the course isn't on the server anymore, nothing to do
            TurnedDown,        // the server said no (duplicate code, bad field...)
            ServerUnreachable
        }

        // sends one change and says how it went. a "no" from the server is
        // written on the change so the user can see why
        private async Task<SendOutcome> TrySendAsync(PendingChange change)
        {
            try
            {
                await SendAsync(change);
                return SendOutcome.Sent;
            }
            catch (ApiUnreachableException)
            {
                return SendOutcome.ServerUnreachable;
            }
            catch (ApiNotFoundException)
            {
                return SendOutcome.AlreadyGone;
            }
            catch (ApiRequestException exception)
            {
                change.Problem = exception.Message;
                return SendOutcome.TurnedDown;
            }
        }

        private async Task SendAsync(PendingChange change)
        {
            switch (change.Kind)
            {
                case PendingChangeKind.Create:
                    var temporaryId = change.Course.ID;
                    var toCreate = PendingChangeRules.Copy(change.Course);
                    toCreate.ID = 0; // 0 tells the API "this one is new"

                    var created = await _inner.SaveCourseAsync(toCreate);

                    // move anything saved under the temporary ID (assignments) to the real one
                    if (_onCourseIdAssigned is not null)
                        await _onCourseIdAssigned(temporaryId, created.ID);
                    break;

                case PendingChangeKind.Update:
                    await _inner.SaveCourseAsync(PendingChangeRules.Copy(change.Course));
                    break;

                case PendingChangeKind.Delete:
                    await _inner.DeleteCourseAsync(change.Course.ID);
                    break;
            }
        }

        private async Task<Course> QueueAsync(List<PendingChange> pending, PendingChangeKind kind, Course course)
        {
            var snapshot = PendingChangeRules.Copy(course);
            snapshot.GradePoints = _gradePointsFor(snapshot.CurrentGrade);

            var queued = PendingChangeRules.Add(pending, kind, snapshot);
            await _pendingFile.WriteAsync(pending);

            var result = PendingChangeRules.Copy(queued);
            result.IsPendingSync = true;

            // the edit form sends a course with no assignments on it, so put them back
            result.Assignments = course.Assignments.Count > 0
                ? course.Assignments
                : await _inner.GetAssignmentsLocalAsync(queued.ID);
            return result;
        }

        private async Task UpdateCacheAsync(Action<List<Course>> change)
        {
            var cache = await _cacheFile.ReadAsync();
            change(cache);
            await _cacheFile.WriteAsync(cache);
        }

        private async Task<List<PendingChange>> LoadPendingAsync()
            => _pending ??= await _pendingFile.ReadAsync();
    }
}
