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

        // typing in a box with an error wipes that box's error, so the pink
        // goes away the moment you start fixing it
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                    NameError = string.Empty;
            }
        }

        private string _courseCode = string.Empty;
        public string CourseCode
        {
            get => _courseCode;
            set
            {
                if (SetProperty(ref _courseCode, value))
                    CourseCodeError = string.Empty;
            }
        }

        private string _instructor = string.Empty;
        public string Instructor
        {
            get => _instructor;
            set
            {
                if (SetProperty(ref _instructor, value))
                    InstructorError = string.Empty;
            }
        }

        private string _credits = "3";
        public string Credits
        {
            get => _credits;
            set
            {
                if (SetProperty(ref _credits, value))
                    CreditsError = string.Empty;
            }
        }

        // one error line per box. empty = box is fine
        private string _nameError = string.Empty;
        public string NameError
        {
            get => _nameError;
            set => SetProperty(ref _nameError, value);
        }

        private string _courseCodeError = string.Empty;
        public string CourseCodeError
        {
            get => _courseCodeError;
            set => SetProperty(ref _courseCodeError, value);
        }

        private string _instructorError = string.Empty;
        public string InstructorError
        {
            get => _instructorError;
            set => SetProperty(ref _instructorError, value);
        }

        private string _creditsError = string.Empty;
        public string CreditsError
        {
            get => _creditsError;
            set => SetProperty(ref _creditsError, value);
        }

        private string _letterGrade = string.Empty;
        public string LetterGrade
        {
            get => _letterGrade;
            set => SetProperty(ref _letterGrade, value);
        }

        // big headline on the error card above Save
        private string _validationTitle = string.Empty;
        public string ValidationTitle
        {
            get => _validationTitle;
            set => SetProperty(ref _validationTitle, value);
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
            set
            {
                if (SetProperty(ref _canTryAgain, value))
                    OnPropertyChanged(nameof(TryAgainText));
            }
        }

        // the banner only grows a button when there's something worth retrying
        // (server unreachable). bad input gets no button, you just fix the box
        public string TryAgainText => CanTryAgain ? "Try Again" : string.Empty;

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

        // checks every box (not just the first bad one) so all the pink shows up
        // at once. same rules the API enforces, so most bad saves never leave the phone
        private bool Validate(out double credits)
        {
            credits = 0;

            NameError = string.Empty;
            CourseCodeError = string.Empty;
            InstructorError = string.Empty;
            CreditsError = string.Empty;

            if (string.IsNullOrWhiteSpace(Name))
                NameError = "Course name is required.";
            else if (Name.Trim().Length > 100)
                NameError = "Course name can't be longer than 100 characters.";

            if (string.IsNullOrWhiteSpace(CourseCode))
                CourseCodeError = "Course code is required.";
            else if (CourseCode.Trim().Length > 10)
                CourseCodeError = "Course code can't be longer than 10 characters.";

            if (!string.IsNullOrWhiteSpace(Instructor) && Instructor.Trim().Length > 100)
                InstructorError = "Instructor name can't be longer than 100 characters.";

            if (!string.IsNullOrWhiteSpace(Credits) && (!double.TryParse(Credits, out credits) || credits < 0 || credits > 6))
            {
                credits = 0;
                CreditsError = "Credits has to be a number from 0 to 6.";
            }

            var problems = new[] { NameError, CourseCodeError, InstructorError, CreditsError }
                .Where(problem => !string.IsNullOrEmpty(problem))
                .ToList();

            if (problems.Count == 0)
            {
                ValidationTitle = string.Empty;
                ValidationMessage = string.Empty;
                return true;
            }

            ValidationTitle = problems.Count == 1 ? "One thing to fix" : $"{problems.Count} things to fix";
            ValidationMessage = string.Join(Environment.NewLine, problems);
            return false;
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
            ValidationTitle = string.Empty;
            ValidationMessage = string.Empty;

            try
            {
                var saved = await _authService.RunWriteAsync(() => _courseRepository.SaveCourseAsync(course));

                var route = IsEditing ? Routes.BackTwice : Routes.Back;
                await _navigation.GoToAsync(route, new Dictionary<string, object> { { QueryKeys.SavedCourse, saved } });
            }
            catch (ApiValidationException exception)
            {
                ValidationTitle = "The server said no";
                ValidationMessage = string.Join(Environment.NewLine, exception.Messages);
            }
            catch (ApiRequestException exception)
            {
                ValidationTitle = "Couldn't save";
                ValidationMessage = exception.Message;
                CanTryAgain = true;
            }
            catch (LoginCancelledException exception)
            {
                ValidationTitle = "Not signed in";
                ValidationMessage = exception.Message;
            }
            finally
            {
                IsSaving = false;
            }
        }
    }
}
