using GritTrack.Models;
using GritTrack.Services;
using GritTrack.Services.Api;

namespace GritTrack.PageModels
{
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

        public RelayCommand GoBackCommand { get; }
        public RelayCommand EditCommand { get; }

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
                Course = course;
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
