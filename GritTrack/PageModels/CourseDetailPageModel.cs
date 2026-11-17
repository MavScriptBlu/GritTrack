using System.Collections.ObjectModel;
using GritTrack.Models;
using GritTrack.Services;
using GritTrack.Services.Api;
using GritTrack.Services.Photos;
using GritTrack.Services.Reminders;

namespace GritTrack.PageModels
{
    public class CourseDetailPageModel : BaseViewModel, IQueryAttributable
    {
        private readonly INavigationService _navigation;
        private readonly ICourseRepository _courseRepository;
        private readonly IAuthService _authService;
        private readonly IPhotoService _photoService;
        private readonly IReminderService _reminderService;

        private Course _course = new();
        public Course Course
        {
            get => _course;
            set => SetProperty(ref _course, value);
        }

        private bool _isConfirmingDelete;
        public bool IsConfirmingDelete
        {
            get => _isConfirmingDelete;
            set => SetProperty(ref _isConfirmingDelete, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // ---- assignments on this course ----

        public ObservableCollection<AssignmentRowItem> Assignments { get; } = new();

        public bool HasAssignments => Assignments.Count > 0;
        public bool HasNoAssignments => Assignments.Count == 0;

        // the assignment whose card is open (photo, reminder, done)
        private AssignmentRowItem? _selectedAssignment;
        public AssignmentRowItem? SelectedAssignment
        {
            get => _selectedAssignment;
            set => SetProperty(ref _selectedAssignment, value);
        }

        private bool _isAssignmentSheetOpen;
        public bool IsAssignmentSheetOpen
        {
            get => _isAssignmentSheetOpen;
            set => SetProperty(ref _isAssignmentSheetOpen, value);
        }

        // true while the camera, gallery or a save is in progress
        private bool _isSheetBusy;
        public bool IsSheetBusy
        {
            get => _isSheetBusy;
            set => SetProperty(ref _isSheetBusy, value);
        }

        // what just happened on the card, good or bad ("Photo saved.", "Camera access is off...")
        private string _sheetMessage = string.Empty;
        public string SheetMessage
        {
            get => _sheetMessage;
            set
            {
                SetProperty(ref _sheetMessage, value);
                OnPropertyChanged(nameof(HasSheetMessage));
            }
        }

        public bool HasSheetMessage => !string.IsNullOrEmpty(SheetMessage);

        public RelayCommand<AssignmentRowItem> OpenAssignmentCommand { get; }
        public RelayCommand CloseAssignmentCommand { get; }
        public RelayCommand TakePhotoCommand { get; }
        public RelayCommand PickPhotoCommand { get; }
        public RelayCommand RemovePhotoCommand { get; }
        public RelayCommand ToggleReminderCommand { get; }
        public RelayCommand ToggleDoneCommand { get; }

        public RelayCommand GoBackCommand { get; }
        public RelayCommand EditCommand { get; }

        public RelayCommand DeleteCommand { get; }
        public RelayCommand ConfirmDeleteCommand { get; }
        public RelayCommand CancelDeleteCommand { get; }

        public CourseDetailPageModel(INavigationService navigation, ICourseRepository courseRepository, IAuthService authService, IPhotoService photoService, IReminderService reminderService)
        {
            _navigation = navigation;
            _courseRepository = courseRepository;
            _authService = authService;
            _photoService = photoService;
            _reminderService = reminderService;

            OpenAssignmentCommand = new RelayCommand<AssignmentRowItem>(row =>
            {
                if (row is null)
                    return;

                SelectedAssignment = row;
                SheetMessage = string.Empty;
                IsAssignmentSheetOpen = true;
            });
            CloseAssignmentCommand = new RelayCommand(() => IsAssignmentSheetOpen = false);

            // camera and gallery share everything except where the photo comes from
            TakePhotoCommand = new RelayCommand(() => _ = AttachPhotoAsync(_photoService.TakePhotoAsync));
            PickPhotoCommand = new RelayCommand(() => _ = AttachPhotoAsync(_photoService.PickFromGalleryAsync));
            RemovePhotoCommand = new RelayCommand(() => _ = RemovePhotoAsync());
            ToggleReminderCommand = new RelayCommand(() => _ = ToggleReminderAsync());
            ToggleDoneCommand = new RelayCommand(() => _ = ToggleDoneAsync());

            GoBackCommand = new RelayCommand(() => _ = _navigation.GoToAsync(Routes.Back));
            EditCommand = new RelayCommand(() => _ = _navigation.GoToAsync(
                Routes.AddCourse,
                new Dictionary<string, object> { { QueryKeys.EditCourse, Course } }));
            DeleteCommand = new RelayCommand(() => IsConfirmingDelete = true);

            ConfirmDeleteCommand = new RelayCommand(() =>
            {
                IsConfirmingDelete = false;
                _ = DeleteAsync();
            });
            CancelDeleteCommand = new RelayCommand(() => IsConfirmingDelete = false);
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue(QueryKeys.Course, out var value) && value is Course course)
            {
                Course = course;
                ShowAssignments();
            }
        }

        private void ShowAssignments()
        {
            Assignments.Clear();
            foreach (var assignment in Course.Assignments.OrderBy(assignment => assignment.DueDate))
                Assignments.Add(new AssignmentRowItem(assignment, _photoService.GetFullPath));

            OnPropertyChanged(nameof(HasAssignments));
            OnPropertyChanged(nameof(HasNoAssignments));
        }

        // getPhoto is either "open the camera" or "open the gallery"
        private async Task AttachPhotoAsync(Func<Task<PhotoResult>> getPhoto)
        {
            var row = SelectedAssignment;
            if (row is null || IsSheetBusy)
                return;

            IsSheetBusy = true;
            SheetMessage = string.Empty;

            try
            {
                var result = await getPhoto();

                // backing out isn't an error, so say nothing
                if (result.Status == PhotoStatus.Cancelled)
                    return;

                if (result.Status != PhotoStatus.Saved)
                {
                    SheetMessage = result.Message;
                    return;
                }

                var oldPhoto = row.Assignment.PhotoFileName;
                row.Assignment.PhotoFileName = result.FileName;
                await _courseRepository.UpdateAssignmentLocalAsync(Course.ID, row.Assignment);

                // the new one is saved, so the one it replaced can go
                _photoService.Delete(oldPhoto);

                row.Refresh();
                SheetMessage = result.Message;
            }
            finally
            {
                IsSheetBusy = false;
            }
        }

        private async Task RemovePhotoAsync()
        {
            var row = SelectedAssignment;
            if (row is null || IsSheetBusy || !row.HasPhoto)
                return;

            var oldPhoto = row.Assignment.PhotoFileName;
            row.Assignment.PhotoFileName = null;
            await _courseRepository.UpdateAssignmentLocalAsync(Course.ID, row.Assignment);
            _photoService.Delete(oldPhoto);

            row.Refresh();
            SheetMessage = "Photo removed.";
        }

        private async Task ToggleReminderAsync()
        {
            var row = SelectedAssignment;
            if (row is null || IsSheetBusy)
                return;

            IsSheetBusy = true;

            try
            {
                if (row.Assignment.ReminderOn)
                {
                    _reminderService.Cancel(row.Assignment);
                    row.Assignment.ReminderOn = false;
                    SheetMessage = "Reminder turned off.";
                }
                else
                {
                    var result = await _reminderService.ScheduleAsync(row.Assignment, Course.CourseCode);
                    row.Assignment.ReminderOn = result.Status == ReminderStatus.Scheduled;

                    // covers the bad cases too: notifications off, already due, not on this device
                    SheetMessage = result.Message;
                }

                await _courseRepository.UpdateAssignmentLocalAsync(Course.ID, row.Assignment);
                row.Refresh();
            }
            finally
            {
                IsSheetBusy = false;
            }
        }

        private async Task ToggleDoneAsync()
        {
            var row = SelectedAssignment;
            if (row is null || IsSheetBusy)
                return;

            row.Assignment.IsCompleted = !row.Assignment.IsCompleted;

            // no point reminding about something that's finished
            if (row.Assignment.IsCompleted && row.Assignment.ReminderOn)
            {
                _reminderService.Cancel(row.Assignment);
                row.Assignment.ReminderOn = false;
            }

            await _courseRepository.UpdateAssignmentLocalAsync(Course.ID, row.Assignment);
            row.Refresh();
            SheetMessage = row.Assignment.IsCompleted ? "Marked done. Nice." : "Marked not done.";
        }

        private async Task DeleteAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                await _authService.RunWriteAsync(async () =>
                {
                    await _courseRepository.DeleteCourseAsync(Course.ID);
                    return true;
                });

                // the course is gone, so its reminders and photos go with it
                foreach (var row in Assignments)
                {
                    _reminderService.Cancel(row.Assignment);
                    _photoService.Delete(row.Assignment.PhotoFileName);
                }

                await _navigation.GoToAsync(
                    Routes.Back,
                    new Dictionary<string, object> { { QueryKeys.DeletedCourse, Course } });
            }
            catch (Exception exception) when (exception is ApiRequestException or LoginCancelledException)
            {
                ErrorMessage = exception.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
