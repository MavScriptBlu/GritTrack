using System.Net.Http.Json;
using GritTrack.Models;

namespace GritTrack.Services.Api
{
    public class ApiCourseRepository : ICourseRepository
    {
        private readonly HttpClient _client;

        private readonly LocalAssignmentStore _assignmentStore;

        public ApiCourseRepository(HttpClient client, LocalAssignmentStore assignmentStore)
        {
            _client = client;
            _assignmentStore = assignmentStore;
        }

        public async Task<List<Course>> GetCoursesAsync()
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/courses");
            using HttpResponseMessage response = await ApiResponseReader.SendAsync(_client, request);
            await ApiResponseReader.EnsureSuccessAsync(response);

            List<CourseResponse>? courses = await response.Content.ReadFromJsonAsync<List<CourseResponse>>(ApiResponseReader.JsonOptions);
            List<Course> loadedCourses = courses?.Select(course => course.ToCourse()).ToList() ?? new List<Course>();

            foreach (Course loadedCourse in loadedCourses)
                loadedCourse.Assignments = await _assignmentStore.GetAsync(loadedCourse.ID);

            return loadedCourses;
        }

        public async Task<Course> SaveCourseAsync(Course course)
        {
            bool isNew = course.ID == 0;

            using HttpRequestMessage request = new HttpRequestMessage(
                isNew ? HttpMethod.Post : HttpMethod.Put,
                isNew ? "api/courses" : $"api/courses/{course.ID}")
            {
                Content = JsonContent.Create(CourseRequest.From(course), options: ApiResponseReader.JsonOptions)
            };

            using HttpResponseMessage response = await ApiResponseReader.SendAsync(_client, request);
            await ApiResponseReader.EnsureSuccessAsync(response);

            CourseResponse? saved = await response.Content.ReadFromJsonAsync<CourseResponse>(ApiResponseReader.JsonOptions);
            Course savedCourse = saved?.ToCourse() ?? throw new ApiRequestException("The server answered, but not with the saved course. Try again.");

            savedCourse.Assignments = await _assignmentStore.GetAsync(savedCourse.ID);
            return savedCourse;
        }

        public async Task DeleteCourseAsync(int courseId)
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, $"api/courses/{courseId}");
            using HttpResponseMessage response = await ApiResponseReader.SendAsync(_client, request);
            await ApiResponseReader.EnsureSuccessAsync(response);

            await _assignmentStore.RemoveCourseAsync(courseId);
        }

        public Task<List<Assignment>> GetAssignmentsLocalAsync(int courseId)
        {
            return _assignmentStore.GetAsync(courseId);
        }

        public async Task<AssignmentImportResult> SaveAssignmentsLocalAsync(int courseId, IEnumerable<Assignment> assignments)
        {
            List<Assignment> assignmentList = assignments.ToList();
            foreach (Assignment assignment in assignmentList)
                assignment.CourseID = courseId;

            // TODO: Swap to batch POST /api/courses/{courseId}/assignments once backend endpoint is live
            return await _assignmentStore.AddAsync(courseId, assignmentList);
        }
    }
}
