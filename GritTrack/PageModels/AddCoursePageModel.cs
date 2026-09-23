using GritTrack.Models;
using GritTrack.Services;

namespace GritTrack.PageModels
{
    // doubles as both Add and Edit — Load(course) switches it into edit mode.
    // one form, one set of validation rules, instead of maintaining two
    // near-identical pages
    public class AddCoursePageModel : BaseViewModel
    {
        private readonly GpaCalculatorService _gpaCalculator;
        private int? _editingCourseId;

        public AddCoursePageModel(GpaCalculatorService gpaCalculator)
        {
            _gpaCalculator = gpaCalculator;

            LetterGrades = new List<string> { "A", "A-", "B+", "B", "B-", "C+", "C", "C-", "D+", "D", "D-", "F" };
            LetterGrade = LetterGrades[0];

            // no CanExecute gate here on purpose — Save always runs so a tap
            // always does *something*. Validate() decides pass/fail and sets
            // ValidationMessage so a blank required field is never a silent no-op.
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(() => BackRequested?.Invoke(this, EventArgs.Empty));
        }

        public List<string> LetterGrades { get; }

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

        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        // fires with the saved Course either way — CourseListPageModel
        // figures out add-vs-update by whether that ID already exists
        public event EventHandler<Course>? CourseSavedRequested;
        public event EventHandler? BackRequested;

        // called when navigating here to edit an existing course (e.g. bumping
        // a grade after extra credit) instead of adding a brand new one —
        // pre-fills the form and flips the title/button text to match
        public void Load(Course course)
        {
            _editingCourseId = course.ID;
            Name = course.Name;
            CourseCode = course.CourseCode;
            Instructor = course.Instructor;
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

        private void Save()
        {
            if (!Validate(out var credits))
                return;

            var course = new Course
            {
                // editing keeps the original ID so CourseListPageModel updates
                // the right row instead of appending a duplicate; adding gets
                // a quick-and-dirty unique-enough ID for sample data — swap for
                // real persistence-generated IDs once this hooks up to storage
                ID = _editingCourseId ?? Random.Shared.Next(1000, 999999),
                Name = Name.Trim(),
                CourseCode = CourseCode.Trim(),
                Instructor = Instructor.Trim(),
                Credits = credits,
                CurrentGrade = LetterGrade,
                GradePoints = _gpaCalculator.GetGradePoints(LetterGrade)
            };

            CourseSavedRequested?.Invoke(this, course);
        }
    }
}
