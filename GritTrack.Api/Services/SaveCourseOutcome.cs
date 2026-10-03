namespace GritTrack.Api.Services;

// what happened when the service tried to save a course. The controller
// turns this into the right HTTP status — the service never picks one
public enum SaveCourseOutcome
{
    Created,
    Updated,
    DuplicateCourseCode,
    NotFound
}
