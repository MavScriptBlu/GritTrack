using GritTrack.Models;
using GritTrack.Services;
using GritTrack.Services.Api;

namespace GritTrack.PageModels
{
    // doubles as both Add and Edit, Load(course) switches it into edit mode.
    // one form, one set of validation rules, instead of maintaining two
    // near-identical pages
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

            // no CanExecute gate here on purpose — Save always runs so a tap
            // always does *something*. Validate() decides pass/fail and sets
            // ValidationMessage so a blank required field is never a silent no-op.
            SaveCommand = new RelayCommand(() => _ = SaveAsync());
            CancelCommand = new RelayCommand(() => _ = _navigation.GoToAsync(Routes.Back));

            // the grade field used to be a plain Picker, but that opens the
            // OS's own gray popup list which can't be styled at all. now
            // tapping the field just opens this bool-driven card instead,
            // same idea as the delete confirm cards on the other pages
            ToggleGradePickerCommand = new RelayCommand(() => IsGradePickerOpen = !IsGradePickerOpen);
            SelectGradeCommand = new RelayCommand<string>(grade =>
            {
                if (!string.IsNullOrEmpty(grade))
                    LetterGrade = grade;
                IsGradePickerOpen = false;
            });
        }

        public List<string> LetterGrades { get; }

        // true while the styled grade-picker card is showing on screen
        private bool _isGradePickerOpen;
        public bool IsGradePickerOpen
        {
            get => _isGradePickerOpen;
            set => SetProperty(ref _isGradePickerOpen, value);
        }

        // true once Load() has been called with an existing course — flips
        // the page into "editing" mode instead of "creating new"
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

        // optional — a course code/ID is nice to have but shouldn't block saving
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

        // true while the course is on its way to the API: shows the spinner
        // and turns the Save button off so it can't be double-tapped
        private bool _isSaving;
        public bool IsSaving
        {
            get => _isSaving;
            set { SetProperty(ref _isSaving, value); OnPropertyChanged(nameof(IsNotSaving)); }
        }

        public bool IsNotSaving => !IsSaving;

        // true when the last save failed because the server couldn't be reached
        // (not because a field was wrong), so a Try Again button makes sense
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

        // Shell calls this when this page is navigated to with an
        // "EditCourse" — that's what flips the page into edit mode
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue(QueryKeys.EditCourse, out var value) && value is Course course)
                Load(course);
        }

        // called when navigating here to edit an existing course (e.g. bumping
        // a grade after extra credit) instead of adding a brand new one —
        // pre-fills the form and flips the title/button text to match
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

            // Name is the only field that's actually required — Course Code,
            // Instructor and Credits (defaulted to 3) are all optional so this
            // doesn't turn into a form nobody can finish
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

            // only the basic "is the name filled in" check happens here. the
            // API has the real rules (code required, credits 0-6, lengths,
            // duplicate codes) and we show whatever it says
            var course = new Course
            {
                // 0 = brand new, the API picks the ID. editing keeps the original
                // ID so the API updates the right course
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
                // if the API says "log in first", this opens the login page and,
                // once they sign in, sends the same save again
                var saved = await _authService.RunWriteAsync(() => _courseRepository.SaveCourseAsync(course));

                // "SavedCourse" is the label CourseListPageModel looks for. the
                // course is already saved, the list just needs to show it.
                // adding got here from the list (one level up); editing got here from
                // detail (two levels up: list -> detail -> here), so pop back to
                // the list directly instead of landing on the now-stale detail page
                var route = IsEditing ? Routes.BackTwice : Routes.Back;
                await _navigation.GoToAsync(route, new Dictionary<string, object> { { QueryKeys.SavedCourse, saved } });
            }
            catch (ApiValidationException exception)
            {
                // 400 or 409: the form stays open so they can fix it
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
