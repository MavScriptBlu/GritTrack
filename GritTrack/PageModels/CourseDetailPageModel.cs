using GritTrack.Models;
using GritTrack.Services;
using GritTrack.Services.Api;

namespace GritTrack.PageModels
{
    // Shell hands the tapped course in through IQueryAttributable, and the
    // commands below move around through INavigationService, so the page
    // behind this file has no code of its own
    public class CourseDetailPageModel : BaseViewModel, IQueryAttributable
    {
        private readonly INavigationService _navigation;
        private readonly ICourseRepository _courseRepository;
        private readonly IAuthService _authService;

        private Course _course = new();
        public Course Course
        {
            get => _course;
            set => SetProperty(ref _course, value);
        }

        // true while the "are you sure you want to drop this?" card is
        // showing on screen. this used to be a native popup (DisplayAlert)
        // but those can't be styled to match the app, so now it's just a
        // card built right into this page that shows/hides with this flag
        private bool _isConfirmingDelete;
        public bool IsConfirmingDelete
        {
            get => _isConfirmingDelete;
            set => SetProperty(ref _isConfirmingDelete, value);
        }

        // true while the delete request is on its way to the API
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        // set when dropping the course fails, so the page can show the message
        // with a Try Again button instead of doing nothing
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

        public RelayCommand GoBackCommand { get; }
        public RelayCommand EditCommand { get; }

        // tapping "Drop This Course" no longer deletes right away, it
        // just opens the confirm card. the actual delete only happens if
        // they tap "Drop It" on that card (ConfirmDeleteCommand)
        public RelayCommand DeleteCommand { get; }
        public RelayCommand ConfirmDeleteCommand { get; }
        public RelayCommand CancelDeleteCommand { get; }

        public CourseDetailPageModel(INavigationService navigation, ICourseRepository courseRepository, IAuthService authService)
        {
            _navigation = navigation;
            _courseRepository = courseRepository;
            _authService = authService;

            GoBackCommand = new RelayCommand(() => _ = _navigation.GoToAsync(Routes.Back));
            EditCommand = new RelayCommand(() => _ = _navigation.GoToAsync(
                Routes.AddCourse,
                new Dictionary<string, object> { { QueryKeys.EditCourse, Course } }));
            DeleteCommand = new RelayCommand(() => IsConfirmingDelete = true);

            // also what the Try Again button runs after a failed delete
            ConfirmDeleteCommand = new RelayCommand(() =>
            {
                IsConfirmingDelete = false;
                _ = DeleteAsync();
            });
            CancelDeleteCommand = new RelayCommand(() => IsConfirmingDelete = false);
        }

        // Shell calls this when this page is navigated to with a "Course"
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue(QueryKeys.Course, out var value) && value is Course course)
                Course = course;
        }

        // deletes through the API first. only when that worked do we go back,
        // handing the list the course so it can drop the row
        private async Task DeleteAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                // a logged-out delete opens the login page, then carries on
                await _authService.RunWriteAsync(async () =>
                {
                    await _courseRepository.DeleteCourseAsync(Course.ID);
                    return true;
                });

                await _navigation.GoToAsync(
                    Routes.BackTwice,
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
