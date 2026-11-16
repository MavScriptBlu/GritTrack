namespace GritTrack.Services.Photos
{
    // the real camera + gallery, through MAUI's MediaPicker
    public class PhotoService : IPhotoService
    {
        private static string PhotoFolder => Path.Combine(FileSystem.AppDataDirectory, "assignment-photos");

        public async Task<PhotoResult> TakePhotoAsync()
        {
            if (!MediaPicker.Default.IsCaptureSupported)
                return new PhotoResult(PhotoStatus.NotAvailable, null, "This device doesn't have a camera GritTrack can use. Pick a photo from the gallery instead.");

            try
            {
                // ask for the camera the first time. if they say no, tell them how to fix it
                var permission = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (permission != PermissionStatus.Granted)
                    permission = await Permissions.RequestAsync<Permissions.Camera>();

                if (permission != PermissionStatus.Granted)
                    return new PhotoResult(PhotoStatus.PermissionDenied, null, "Camera access is off for GritTrack. Turn it on in your device Settings, or pick a photo from the gallery.");

                var photo = await MediaPicker.Default.CapturePhotoAsync();
                return await KeepCopyAsync(photo);
            }
            catch (FeatureNotSupportedException)
            {
                return new PhotoResult(PhotoStatus.NotAvailable, null, "The camera isn't available right now. Pick a photo from the gallery instead.");
            }
            catch (PermissionException)
            {
                return new PhotoResult(PhotoStatus.PermissionDenied, null, "Camera access is off for GritTrack. Turn it on in your device Settings, or pick a photo from the gallery.");
            }
            catch (Exception)
            {
                return new PhotoResult(PhotoStatus.Failed, null, "Couldn't save that photo. Try again.");
            }
        }

        public async Task<PhotoResult> PickFromGalleryAsync()
        {
            try
            {
                var photos = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { SelectionLimit = 1 });
                return await KeepCopyAsync(photos?.FirstOrDefault());
            }
            catch (PermissionException)
            {
                return new PhotoResult(PhotoStatus.PermissionDenied, null, "GritTrack isn't allowed to open your photos. Turn that on in your device Settings.");
            }
            catch (Exception)
            {
                return new PhotoResult(PhotoStatus.Failed, null, "Couldn't open that photo. Try a different one.");
            }
        }

        public string GetFullPath(string fileName) => Path.Combine(PhotoFolder, fileName);

        public void Delete(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return;

            try
            {
                File.Delete(GetFullPath(fileName));
            }
            catch (IOException)
            {
                // a leftover file isn't worth bothering the user about
            }
        }

        // the picker hands back a temporary file that can vanish, so the app
        // keeps its own copy under a new name
        private async Task<PhotoResult> KeepCopyAsync(FileResult? photo)
        {
            if (photo is null)
                return new PhotoResult(PhotoStatus.Cancelled, null, string.Empty);

            Directory.CreateDirectory(PhotoFolder);

            var extension = Path.GetExtension(photo.FileName);
            var fileName = Guid.NewGuid().ToString("N") + (string.IsNullOrEmpty(extension) ? ".jpg" : extension);

            await using (var source = await photo.OpenReadAsync())
            await using (var target = File.Create(GetFullPath(fileName)))
            {
                await source.CopyToAsync(target);
            }

            return new PhotoResult(PhotoStatus.Saved, fileName, "Photo saved.");
        }
    }
}
