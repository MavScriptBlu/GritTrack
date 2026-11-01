using System.Net.Http.Json;
using GritTrack.Models;

namespace GritTrack.Services.Api
{
    // the real ICourseRepository: every call goes to the GritTrack API over
    // HTTP. the app never touches SQL Server. whether the API keeps the
    // courses in a JSON file or SQL Server is the API's business
    public class ApiCourseRepository : ICourseRepository
    {
        private readonly HttpClient _client;

        // the HttpClient comes from the factory in MauiProgram, already pointed
        // at the API and wired to add the login token on writes
        public ApiCourseRepository(HttpClient client)
        {
            _client = client;
        }

        public async Task<List<Course>> GetCoursesAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/courses");
            using var response = await ApiResponseReader.SendAsync(_client, request);
            await ApiResponseReader.EnsureSuccessAsync(response);

            var courses = await response.Content.ReadFromJsonAsync<List<CourseResponse>>(ApiResponseReader.JsonOptions);
            return courses?.Select(course => course.ToCourse()).ToList() ?? new List<Course>();
        }

        // ID 0 means "brand new" (POST); any other ID is an edit (PUT).
        // gives back the course as the server saved it, with its real ID
        public async Task<Course> SaveCourseAsync(Course course)
        {
            var isNew = course.ID == 0;

            using var request = new HttpRequestMessage(
                isNew ? HttpMethod.Post : HttpMethod.Put,
                isNew ? "api/courses" : $"api/courses/{course.ID}")
            {
                Content = JsonContent.Create(CourseRequest.From(course), options: ApiResponseReader.JsonOptions)
            };

            using var response = await ApiResponseReader.SendAsync(_client, request);
            await ApiResponseReader.EnsureSuccessAsync(response);

            var saved = await response.Content.ReadFromJsonAsync<CourseResponse>(ApiResponseReader.JsonOptions);
            return saved?.ToCourse() ?? throw new ApiRequestException("The server answered, but not with the saved course. Try again.");
        }

        public async Task DeleteCourseAsync(int courseId)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/courses/{courseId}");
            using var response = await ApiResponseReader.SendAsync(_client, request);
            await ApiResponseReader.EnsureSuccessAsync(response);
        }
    }
}
