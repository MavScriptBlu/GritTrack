using System.Text.Json;
using GritTrack.Models;

namespace GritTrack.Services
{
    /// <summary>Persists assignments locally until the API supports them.</summary>
    public class LocalAssignmentStore
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private Dictionary<int, List<Assignment>>? _assignmentsByCourseId;

        private static string FilePath => Path.Combine(FileSystem.AppDataDirectory, "local-assignments.json");

        public async Task<List<Assignment>> GetAsync(int courseId)
        {
            await _gate.WaitAsync();
            try
            {
                Dictionary<int, List<Assignment>> all = await LoadAsync();
                if (!all.TryGetValue(courseId, out List<Assignment>? assignments))
                    return new List<Assignment>();

                foreach (Assignment assignment in assignments)
                    assignment.CourseID = courseId;

                return assignments.ToList();
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<AssignmentImportResult> AddAsync(int courseId, IEnumerable<Assignment> assignments)
        {
            await _gate.WaitAsync();
            try
            {
                Dictionary<int, List<Assignment>> all = await LoadAsync();
                if (!all.TryGetValue(courseId, out List<Assignment>? existing))
                {
                    existing = new List<Assignment>();
                    all[courseId] = existing;
                }

                AssignmentImportResult result = AssignmentDuplicateRule.Filter(existing, assignments);

                if (result.AddedCount > 0)
                {
                    existing.AddRange(result.AddedAssignments);
                    await WriteAsync(all);
                }

                return result;
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task RemoveCourseAsync(int courseId)
        {
            await _gate.WaitAsync();
            try
            {
                Dictionary<int, List<Assignment>> all = await LoadAsync();
                if (all.Remove(courseId))
                    await WriteAsync(all);
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task<Dictionary<int, List<Assignment>>> LoadAsync()
        {
            if (_assignmentsByCourseId is not null)
                return _assignmentsByCourseId;

            try
            {
                if (File.Exists(FilePath))
                {
                    string json = await File.ReadAllTextAsync(FilePath);
                    _assignmentsByCourseId = JsonSerializer.Deserialize<Dictionary<int, List<Assignment>>>(json);
                }
            }
            catch (Exception exception) when (exception is IOException or JsonException)
            {
                _assignmentsByCourseId = null;
            }

            return _assignmentsByCourseId ??= new Dictionary<int, List<Assignment>>();
        }

        private static async Task WriteAsync(Dictionary<int, List<Assignment>> all)
        {
            string json = JsonSerializer.Serialize(all);
            await File.WriteAllTextAsync(FilePath, json);
        }
    }
}
