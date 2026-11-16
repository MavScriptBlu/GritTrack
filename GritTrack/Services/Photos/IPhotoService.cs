namespace GritTrack.Services.Photos
{
    public enum PhotoStatus
    {
        Saved,
        Cancelled,        // they backed out of the camera or gallery
        PermissionDenied, // camera access is turned off for the app
        NotAvailable,     // this device has no camera we can use
        Failed
    }

    // what happened, the saved file's name when it worked, and a message
    // that's safe to show on screen when it didn't
    public record PhotoResult(PhotoStatus Status, string? FileName, string Message);

    // takes or picks a photo and keeps a copy inside the app's own folder.
    // PageModels use this instead of touching the camera themselves
    public interface IPhotoService
    {
        Task<PhotoResult> TakePhotoAsync();

        Task<PhotoResult> PickFromGalleryAsync();

        // where a saved photo lives on this device, for showing it
        string GetFullPath(string fileName);

        // removes a saved photo. fine to call with null or a file that's already gone
        void Delete(string? fileName);
    }
}
