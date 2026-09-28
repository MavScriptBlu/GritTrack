using System.Collections.ObjectModel;
using GritTrack.Models;
using GritTrack.Services;

namespace GritTrack.PageModels
{
    public class CourseListPageModel : BaseViewModel
    {
        private readonly GpaCalculatorService _gpaCalculator;

        // this is how we grab/save courses — it's an interface, not a real
        // class, so the actual storage can change later (like a real
        // database) without having to rewrite this whole file
        private readonly ICourseRepository _courseRepository;

        // same deal, but for the starting-GPA number instead of courses —
        // its own separate storage spot, same swap-it-out-later idea
        private readonly IGpaProfileRepository _gpaProfileRepository;

        // changes the app's accent color whenever the GPA status changes —
        // see ThemeService for how that actually works
        private readonly IThemeService _themeService;

        // straight from MSTC's Beta Chi Theta (PTK) chapter bylaws:
        // 3.5 gets you invited in, 3.0 keeps you a member in good standing
        private const double PtkInitiationGpa = 3.5;
        private const double PtkGoodStandingGpa = 3.0;

        public CourseListPageModel(GpaCalculatorService gpaCalculator, ICourseRepository courseRepository, IGpaProfileRepository gpaProfileRepository, IThemeService themeService)
        {
            _gpaCalculator = gpaCalculator;
            _courseRepository = courseRepository;
            _gpaProfileRepository = gpaProfileRepository;
            _themeService = themeService;

            Courses = new ObservableCollection<Course>();

            // RelayCommand only takes a plain Action, so the async work runs
            // as fire-and-forget from the command's point of view — the
            // "_ =" discard is intentional, not a missed await
            LoadCoursesCommand = new RelayCommand(() => _ = LoadCoursesAsync());
            CourseSelectedCommand = new RelayCommand<Course>(OnCourseSelected);
            AddCourseCommand = new RelayCommand(() => AddCourseRequested?.Invoke(this, EventArgs.Empty));

            // swiping a course now just opens the "are you sure" card
            // instead of deleting right away — same pattern as the Drop
            // This Course button on CourseDetailPage. ConfirmDeleteCommand
            // below is what actually removes it.
            DeleteCourseCommand = new RelayCommand<Course>(course =>
            {
                PendingDeleteCourse = course;
                IsConfirmingDelete = true;
            });
            ConfirmDeleteCommand = new RelayCommand(() =>
            {
                IsConfirmingDelete = false;
                _ = RemoveCourseAsync(PendingDeleteCourse);
                PendingDeleteCourse = null;
            });
            CancelDeleteCommand = new RelayCommand(() =>
            {
                IsConfirmingDelete = false;
                PendingDeleteCourse = null;
            });

            ToggleStartingGpaCommand = new RelayCommand(() => IsEditingStartingGpa = !IsEditingStartingGpa);
            SaveStartingGpaCommand = new RelayCommand(() => _ = SaveStartingGpaAsync());
        }

        // the list of courses shown on screen
        private ObservableCollection<Course> _courses = new();
        public ObservableCollection<Course> Courses
        {
            get => _courses;
            set => SetProperty(ref _courses, value);
        }

        // the GPA number shown at the top of the page
        private double _currentGpa;
        public double CurrentGpa
        {
            get => _currentGpa;
            set => SetProperty(ref _currentGpa, value);
        }

        // Safe / Watch / AtRisk — used to color the status card and pick
        // the app's accent color
        private GpaStatus _status;
        public GpaStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        // true while the list is loading, so the spinner knows when to show
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        // true only the very first time the list loads, so a course added
        // mid-session doesn't get wiped out by the next OnAppearing() reload
        private bool _hasLoaded;

        // the saved starting GPA and credits, kept separate from the text
        // boxes below so a half-typed edit doesn't mess up the real saved
        // number that RecalculateGpa() uses
        private double _startingGpa;
        private double _startingCredits;

        // true when the "set starting GPA" boxes are open on screen
        private bool _isEditingStartingGpa;
        public bool IsEditingStartingGpa
        {
            get => _isEditingStartingGpa;
            set => SetProperty(ref _isEditingStartingGpa, value);
        }

        // what's typed into the "Starting GPA" text box — kept as text
        // (not a number) so the box can sit empty or half-typed without
        // crashing, same trick used on AddCoursePageModel's Credits box
        private string _startingGpaInput = string.Empty;
        public string StartingGpaInput
        {
            get => _startingGpaInput;
            set => SetProperty(ref _startingGpaInput, value);
        }

        // what's typed into the "Credits So Far" text box
        private string _startingCreditsInput = string.Empty;
        public string StartingCreditsInput
        {
            get => _startingCreditsInput;
            set => SetProperty(ref _startingCreditsInput, value);
        }

        // error message shown when the starting GPA/credits typed in don't
        // make sense (like a GPA over 4.0)
        private string _startingGpaValidationMessage = string.Empty;
        public string StartingGpaValidationMessage
        {
            get => _startingGpaValidationMessage;
            set
            {
                SetProperty(ref _startingGpaValidationMessage, value);
                HasStartingGpaValidationMessage = !string.IsNullOrWhiteSpace(value);
            }
        }

        // true when there's an error message to show — lets the XAML hide
        // the message label until there's actually something to say
        private bool _hasStartingGpaValidationMessage;
        public bool HasStartingGpaValidationMessage
        {
            get => _hasStartingGpaValidationMessage;
            set => SetProperty(ref _hasStartingGpaValidationMessage, value);
        }

        // true while the "are you sure you want to drop this?" card is
        // showing on screen, same idea as CourseDetailPageModel's version —
        // this one covers the swipe-to-delete gesture on the list instead
        private bool _isConfirmingDelete;
        public bool IsConfirmingDelete
        {
            get => _isConfirmingDelete;
            set => SetProperty(ref _isConfirmingDelete, value);
        }

        // whichever course is waiting on a yes/no answer from the card —
        // holds onto it between "swiped to delete" and "tapped Drop It"
        private Course? _pendingDeleteCourse;
        public Course? PendingDeleteCourse
        {
            get => _pendingDeleteCourse;
            set => SetProperty(ref _pendingDeleteCourse, value);
        }

        public RelayCommand LoadCoursesCommand { get; }
        public RelayCommand<Course> CourseSelectedCommand { get; }
        public RelayCommand AddCourseCommand { get; }
        public RelayCommand<Course> DeleteCourseCommand { get; }
        public RelayCommand ConfirmDeleteCommand { get; }
        public RelayCommand CancelDeleteCommand { get; }
        public RelayCommand ToggleStartingGpaCommand { get; }
        public RelayCommand SaveStartingGpaCommand { get; }
        public event EventHandler<Course>? SelectedCourseRequested;
        public event EventHandler? AddCourseRequested;

        // custom event: fired after a course actually leaves the collection,
        // so the Page can react (toast, undo snackbar, whatever) without the
        // ViewModel knowing a thing about UI
        public event EventHandler<Course>? CourseDeleted;

        private async Task LoadCoursesAsync()
        {
            if (_hasLoaded)
                return;

            IsBusy = true;

            var courses = await _courseRepository.GetCoursesAsync();

            Courses.Clear();
            foreach (var course in courses)
                Courses.Add(course);

            // grab whatever starting GPA was saved before (if any) — for a
            // 2nd or 3rd-year student, this is the GPA they already had
            // before they started tracking classes in this app
            var profile = await _gpaProfileRepository.GetProfileAsync();
            _startingGpa = profile.StartingGpa;
            _startingCredits = profile.StartingCredits;
            StartingGpaInput = _startingCredits > 0 ? _startingGpa.ToString("0.##") : string.Empty;
            StartingCreditsInput = _startingCredits > 0 ? _startingCredits.ToString("0.##") : string.Empty;

            RecalculateGpa();

            _hasLoaded = true;
            IsBusy = false;
        }

        // handles both adding a new course and saving edits to one — if
        // the ID already exists, that row gets replaced, otherwise it's added
        public async Task SaveCourseAsync(Course course)
        {
            await _courseRepository.SaveCourseAsync(course);

            var index = IndexOf(course.ID);
            if (index >= 0)
                Courses[index] = course;
            else
                Courses.Add(course);

            RecalculateGpa();
        }

        // dropping a class — used by both the swipe-to-delete gesture on
        // this page and the Delete button on CourseDetailPage
        public async Task RemoveCourseAsync(Course? course)
        {
            if (course is null)
                return;

            var index = IndexOf(course.ID);
            if (index < 0)
                return;

            await _courseRepository.DeleteCourseAsync(course.ID);

            var removed = Courses[index];
            Courses.RemoveAt(index);
            RecalculateGpa();
            CourseDeleted?.Invoke(this, removed);
        }

        // checks and saves the starting GPA/credits
        private async Task SaveStartingGpaAsync()
        {
            var gpaText = StartingGpaInput?.Trim();
            var creditsText = StartingCreditsInput?.Trim();

            // both blank = "no starting GPA", perfectly valid for a
            // first-year student with nothing to carry in
            if (string.IsNullOrEmpty(gpaText) && string.IsNullOrEmpty(creditsText))
            {
                _startingGpa = 0;
                _startingCredits = 0;
                await _gpaProfileRepository.SaveProfileAsync(new GpaProfile { StartingGpa = 0, StartingCredits = 0 });
                StartingGpaValidationMessage = string.Empty;
                RecalculateGpa();
                IsEditingStartingGpa = false;
                return;
            }

            if (!double.TryParse(gpaText, out var gpa) || gpa < 0 || gpa > 4.0)
            {
                StartingGpaValidationMessage = "GPA needs to be a number between 0.0 and 4.0";
                return;
            }

            if (!double.TryParse(creditsText, out var credits) || credits <= 0)
            {
                StartingGpaValidationMessage = "Credits needs to be a number greater than 0";
                return;
            }

            _startingGpa = gpa;
            _startingCredits = credits;
            await _gpaProfileRepository.SaveProfileAsync(new GpaProfile { StartingGpa = gpa, StartingCredits = credits });

            StartingGpaValidationMessage = string.Empty;
            RecalculateGpa();
            IsEditingStartingGpa = false;
        }

        private int IndexOf(int courseId)
        {
            for (var i = 0; i < Courses.Count; i++)
            {
                if (Courses[i].ID == courseId)
                    return i;
            }

            return -1;
        }

        private void RecalculateGpa()
        {
            CurrentGpa = _startingCredits > 0
                ? _gpaCalculator.CalculateGpa(Courses, _startingGpa, _startingCredits)
                : _gpaCalculator.CalculateGpa(Courses);

            Status = _gpaCalculator.GetStatus(CurrentGpa, PtkGoodStandingGpa, PtkInitiationGpa);

            // the actual dynamic-resource trigger — every time GPA changes,
            // the app's accent color updates to match how you're doing
            _themeService.ApplyGpaStatusTheme(Status);
        }

        private void OnCourseSelected(Course? course)
        {
            if (course is null)
                return;

            SelectedCourseRequested?.Invoke(this, course);
        }
    }
}
