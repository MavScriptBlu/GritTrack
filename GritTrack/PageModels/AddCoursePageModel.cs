using GritTrack.Models;
using GritTrack.Services;
using GritTrack.Services.Api;

namespace GritTrack.PageModels
{
    public class AddCoursePageModel : BaseViewModel, IQueryAttributable
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IAuthService _authService;
        private readonly INavigationService _navigation;
        private int? _editingCourseId;

        public AddCoursePageModel(ICourseRepository courseRepository, IAuthService authService, INavigationService navigation)
        {
            _courseRepository = courseRepository;
            _authService = authService;
            _navigation = navigation;

            LetterGrades = new List<string> { "A", "A-", "B+", "B", "B-", "C+", "C", "C-", "D+", "D", "D-", "F" };
            LetterGrade = LetterGrades[0];

            SaveCommand = new RelayCommand(() => _ = SaveAsync());
            CancelCommand = new RelayCommand(() => _ = _navigation.GoToAsync(Routes.Back));
            ToggleGradePickerCommand = new RelayCommand(() => IsGradePickerOpen = !IsGradePickerOpen);
            SelectGradeCommand = new RelayCommand<string>(grade =>
            {
                if (!string.IsNullOrEmpty(grade))
                    LetterGrade = grade;
                IsGradePickerOpen = false;
            });
        }

        public List<string> LetterGrades { get; }

        private bool _isGradePickerOpen;
        public bool IsGradePickerOpen
        {
            get => _isGradePickerOpen;
            set => SetProperty(ref _isGradePickerOpen, value);
        }

        public bool IsEditing => _editingCourseId.HasValue;

        private string _title = "Add Course";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _saveButtonText = "Save Course";
        public string SaveButtonText
        {
            get => _saveButtonText;
            set => SetProperty(ref _saveButtonText, value);
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _courseCode = string.Empty;
        public string CourseCode
        {
            get => _courseCode;
            set => SetProperty(ref _courseCode, value);
        }

        private string _instructor = string.Empty;
        public string Instructor
        {
            get => _instructor;
            set => SetProperty(ref _instructor, value);
        }

        private string _credits = "3";
        public string Credits
        {
            get => _credits;
            set => SetProperty(ref _credits, value);
        }

        private string _letterGrade = string.Empty;
        public string LetterGrade
        {
            get => _letterGrade;
            set => SetProperty(ref _letterGrade, value);
        }

        private string _validationMessage = string.Empty;
        public string ValidationMessage
        {
            get => _validationMessage;
            set { SetProperty(ref _validationMessage, value); OnPropertyChanged(nameof(HasValidationMessage)); }
        }

        public bool HasValidationMessage => !string.IsNullOrEmpty(ValidationMessage);

        private bool _isSaving;
        public bool IsSaving
        {
            get => _isSaving;
            set { SetProperty(ref _isSaving, value); OnPropertyChanged(nameof(IsNotSaving)); }
        }

        public bool IsNotSaving => !IsSaving;

        private bool _canTryAgain;
        public bool CanTryAgain
        {
            get => _canTryAgain;
            set => SetProperty(ref _canTryAgain, value);
        }

        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand ToggleGradePickerCommand { get; }
        public RelayCommand<string> SelectGradeCommand { get; }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue(QueryKeys.EditCourse, out var value) && value is Course course)
                Load(course);
        }

        public void Load(Course course)
        {
            _editingCourseId = course.ID;
            Name = course.Name;
            CourseCode = course.CourseCode;
            Instructor = course.Instructor ?? string.Empty;
            Credits = course.Credits.ToString();
            LetterGrade = string.IsNullOrWhiteSpace(course.CurrentGrade) ? LetterGrades[0] : course.CurrentGrade;
            Title = "Edit Course";
            SaveButtonText = "Save Changes";
            OnPropertyChanged(nameof(IsEditing));
        }

        private bool Validate(out double credits)
        {
            credits = 0;

            if (string.IsNullOrWhiteSpace(Name))
            {
                ValidationMessage = "Course name is required.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(Credits) && (!double.TryParse(Credits, out credits) || credits < 0))
            {
                ValidationMessage = "Credits has to be a positive number.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Credits))
                credits = 0;

            ValidationMessage = string.Empty;
            return true;
        }

        private async Task SaveAsync()
        {
            if (IsSaving || !Validate(out var credits))
                return;

            var course = new Course
            {
                ID = _editingCourseId ?? 0,
                Name = Name.Trim(),
                CourseCode = (CourseCode ?? string.Empty).Trim(),
                Instructor = (Instructor ?? string.Empty).Trim(),
                Credits = credits,
                CurrentGrade = LetterGrade
            };

            IsSaving = true;
            CanTryAgain = false;
            ValidationMessage = string.Empty;

            try
            {
                var saved = await _authService.RunWriteAsync(() => _courseRepository.SaveCourseAsync(course));

                var route = IsEditing ? Routes.BackTwice : Routes.Back;
                await _navigation.GoToAsync(route, new Dictionary<string, object> { { QueryKeys.SavedCourse, saved } });
            }
            catch (ApiValidationException exception)
            {
                ValidationMessage = string.Join(Environment.NewLine, exception.Messages);
            }
            catch (ApiRequestException exception)
            {
                ValidationMessage = exception.Message;
                CanTryAgain = true;
            }
            catch (LoginCancelledException exception)
            {
                ValidationMessage = exception.Message;
            }
            finally
            {
                IsSaving = false;
            }
        }
    }
}
