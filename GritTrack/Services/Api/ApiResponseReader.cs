using System.Net;
using System.Text.Json;

namespace GritTrack.Services.Api
{
    // turns a failed HTTP answer into one of our own exceptions, and wraps the
    // "couldn't reach the server" cases. shared by the course and login calls
    public static class ApiResponseReader
    {
        public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        // sends the request. a dead network, a refused connection or a timeout
        // all become the same friendly ApiRequestException
        public static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpRequestMessage request)
        {
            try
            {
                return await client.SendAsync(request);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                throw new ApiRequestException("Couldn't reach the GritTrack server. Check that the API is running and try again.");
            }
        }

        // does nothing for a 2xx answer, otherwise throws the matching exception
        public static async Task EnsureSuccessAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    throw new AuthenticationRequiredException();

                case HttpStatusCode.BadRequest:
                    throw new ApiValidationException(await ReadProblemMessagesAsync(response, "Some of the course details aren't valid."));

                case HttpStatusCode.Conflict:
                    throw new ApiValidationException(await ReadProblemMessagesAsync(response, "That course code is already in use."));

                case HttpStatusCode.NotFound:
                    throw new ApiRequestException("That course no longer exists. Pull down to refresh the list.");

                default:
                    throw new ApiRequestException($"The server had a problem (error {(int)response.StatusCode}). Try again in a moment.");
            }
        }

        // reads the ProblemDetails body. a 400 has an "errors" object (field name
        // to a list of messages); a 409 has a "detail" sentence
        private static async Task<IReadOnlyList<string>> ReadProblemMessagesAsync(HttpResponseMessage response, string fallbackMessage)
        {
            try
            {
                using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                var messages = new List<string>();

                if (document.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    foreach (var field in errors.EnumerateObject())
                    {
                        foreach (var message in field.Value.EnumerateArray())
                        {
                            var text = message.GetString();
                            if (!string.IsNullOrWhiteSpace(text))
                                messages.Add(text);
                        }
                    }
                }

                if (messages.Count == 0 && document.RootElement.TryGetProperty("detail", out var detail))
                {
                    var text = detail.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                        messages.Add(text);
                }

                if (messages.Count > 0)
                    return messages;
            }
            catch (Exception exception) when (exception is JsonException or InvalidOperationException)
            {
                // the body wasn't the shape we expected: fall through to the generic line
            }

            return new[] { fallbackMessage };
        }
    }
}
