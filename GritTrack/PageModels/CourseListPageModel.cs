using System.Collections.ObjectModel;
using GritTrack.Models;
using GritTrack.Services;
using GritTrack.Services.Api;

namespace GritTrack.PageModels
{
    public class CourseListPageModel : BaseViewModel, IQueryAttributable
    {
        // login state, and the helper that sends a logged-out write to the
        // login page and then carries on
        private readonly IAuthService _authService;

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

        // moves between pages and shows alerts — the PageModel asks, the
        // service does the Shell work, so nothing in here touches UI
        private readonly INavigationService _navigation;

        // straight from MSTC's Beta Chi Theta (PTK) chapter bylaws:
        // 3.5 gets you invited in, 3.0 keeps you a member in good standing
        private const double PtkInitiationGpa = 3.5;
        private const double PtkGoodStandingGpa = 3.0;

        public CourseListPageModel(GpaCalculatorService gpaCalculator, ICourseRepository courseRepository, IGpaProfileRepository gpaProfileRepository, IThemeService themeService, INavigationService navigation, IAuthService authService)
        {
            _authService = authService;
            _gpaCalculator = gpaCalculator;
            _courseRepository = courseRepository;
            _gpaProfileRepository = gpaProfileRepository;
            _themeService = themeService;
            _navigation = navigation;

            Courses = new ObservableCollection<Course>();
            FilteredCourses = new ObservableCollection<Course>();
            SimulatedAssignments = new ObservableCollection<SimulatedAssignmentItem>();

            // RelayCommand only takes a plain Action, so the async work runs
            // as fire-and-forget from the command's point of view — the
            // "_ =" discard is intentional, not a missed await
            LoadCoursesCommand = new RelayCommand(() => _ = LoadCoursesAsync(forceReload: false));

            // pull to refresh and Try Again always ask the API again
            RefreshCommand = new RelayCommand(() => _ = LoadCoursesAsync(forceReload: true));
            CourseSelectedCommand = new RelayCommand<Course>(OnCourseSelected);
            AddCourseCommand = new RelayCommand(() => _ = _navigation.GoToAsync(Routes.AddCourse));

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

            // clears the search box in one tap
            ClearSearchCommand = new RelayCommand(() => SearchQuery = string.Empty);

            // what-if GPA simulator: opens the panel, or sets every upcoming
            // assignment to the same percent with one of the preset buttons
            ToggleSimulatorCommand = new RelayCommand(() =>
            {
                IsSimulatorOpen = !IsSimulatorOpen;
                if (IsSimulatorOpen)
                    BuildSimulationItems();
            });
            ApplyPresetScenarioCommand = new RelayCommand<string>(percentText =>
            {
                if (double.TryParse(percentText, out var percent))
                {
                    foreach (var simulatedAssignment in SimulatedAssignments)
                        simulatedAssignment.ExpectedPoints = Math.Round(simulatedAssignment.PointsPossible * (percent / 100.0));
                    RecalculateSimulatedGpa();
                }
            });

            // top bar button: "Sign in" opens the login page, "Sign out" forgets the token
            AuthCommand = new RelayCommand(() => _ = ToggleSignInAsync());
            _authService.SignInChanged += (_, _) => OnPropertyChanged(nameof(AuthButtonText));

            // the page only sets its BindingContext, so the first load starts here
            _ = LoadCoursesAsync(forceReload: false);
        }

        // text for the top bar sign in/out button
        public string AuthButtonText => _authService.IsSignedIn ? "Sign out" : "Sign in";

        private async Task ToggleSignInAsync()
        {
            if (_authService.IsSignedIn)
                await _authService.SignOutAsync();
            else
                await _authService.RequestLoginAsync();
        }

        // the list of courses shown on screen
        private ObservableCollection<Course> _courses = new();
        public ObservableCollection<Course> Courses
        {
            get => _courses;
            set => SetProperty(ref _courses, value);
        }

        // the courses that match the search box. the list on screen shows
        // this one, and Courses stays the full list
        public ObservableCollection<Course> FilteredCourses { get; }

        // the upcoming assignments the what-if simulator lets you score
        public ObservableCollection<SimulatedAssignmentItem> SimulatedAssignments { get; }

        // whatever is typed in the search bar above the course list
        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    OnPropertyChanged(nameof(HasSearchQuery));
                    ApplyFilter();
                }
            }
        }

        // true once something is typed, so the clear button only shows then
        public bool HasSearchQuery => !string.IsNullOrWhiteSpace(SearchQuery);

        // true when the what-if GPA simulator panel is open
        private bool _isSimulatorOpen;
        public bool IsSimulatorOpen
        {
            get => _isSimulatorOpen;
            set => SetProperty(ref _isSimulatorOpen, value);
        }

        // the GPA you'd end up with if the simulated scores happen
        private double _projectedGpa;
        public double ProjectedGpa
        {
            get => _projectedGpa;
            set
            {
                if (SetProperty(ref _projectedGpa, value))
                    OnPropertyChanged(nameof(GpaDeltaText));
            }
        }

        private GpaStatus _projectedStatus;
        public GpaStatus ProjectedStatus
        {
            get => _projectedStatus;
            set => SetProperty(ref _projectedStatus, value);
        }

        // how far the projected GPA is from the real one, like +0.12 or -0.05
        public string GpaDeltaText
        {
            get
            {
                var difference = ProjectedGpa - CurrentGpa;
                if (difference > 0.005) return $"+{difference:F2}";
                if (difference < -0.005) return $"{difference:F2}";
                return "±0.00";
            }
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

        // set when loading, saving or removing a course fails, so the page
        // can show a message with a Try Again button instead of a blank screen
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

        public RelayCommand LoadCoursesCommand { get; }
        public RelayCommand RefreshCommand { get; }
        public RelayCommand AuthCommand { get; }
        public RelayCommand<Course> CourseSelectedCommand { get; }
        public RelayCommand AddCourseCommand { get; }
        public RelayCommand<Course> DeleteCourseCommand { get; }
        public RelayCommand ConfirmDeleteCommand { get; }
        public RelayCommand CancelDeleteCommand { get; }
        public RelayCommand ToggleStartingGpaCommand { get; }
        public RelayCommand SaveStartingGpaCommand { get; }
        public RelayCommand ClearSearchCommand { get; }
        public RelayCommand ToggleSimulatorCommand { get; }
        public RelayCommand<string> ApplyPresetScenarioCommand { get; }

        // Shell calls this when we land back on the list. "SavedCourse"
        // comes from AddCoursePageModel (add or edit) and "DeletedCourse"
        // comes from CourseDetailPageModel's Drop It button — either one
        // hands us a Course to act on without a full reload.
        // each value gets removed right after it's used — Shell was
        // replaying the old one on every visit back here, which is why
        // "Removed" kept popping up.
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue(QueryKeys.SavedCourse, out var saved) && saved is Course savedCourse)
            {
                ShowSavedCourse(savedCourse);
                query.Remove(QueryKeys.SavedCourse);
            }

            if (query.TryGetValue(QueryKeys.DeletedCourse, out var deleted) && deleted is Course deletedCourse)
            {
                _ = ShowDeletedCourseAsync(deletedCourse);
                query.Remove(QueryKeys.DeletedCourse);
            }
        }

        private async Task LoadCoursesAsync(bool forceReload)
        {
            if (_hasLoaded && !forceReload)
            {
                // a pull-to-refresh turns the spinner on before it runs the
                // command, so turn it back off even when there's nothing to load
                IsBusy = false;
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
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
            }
            catch (Exception)
            {
                // _hasLoaded stays false, so Try Again really does reload
                ErrorMessage = "Couldn't load your courses. Check that the API is running and try again.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // the add/edit page already saved the course through the API. all the
        // list does is show it: replace the row if the ID is here, else add it
        private void ShowSavedCourse(Course course)
        {
            var index = IndexOf(course.ID);
            if (index >= 0)
                Courses[index] = course;
            else
                Courses.Add(course);

            ErrorMessage = string.Empty;
            RecalculateGpa();
        }

        // the detail page already deleted the course through the API. the list
        // just drops the row and says so
        private async Task ShowDeletedCourseAsync(Course course)
        {
            var index = IndexOf(course.ID);
            if (index >= 0)
                Courses.RemoveAt(index);

            RecalculateGpa();

            // pure UI feedback, but it goes through the navigation service
            // so this file still never touches a Page
            await _navigation.ShowAlertAsync("Removed", $"{course.CourseCode} was removed.");
        }

        // dropping a class with the swipe gesture on this page: delete through
        // the API first, and only remove the row once the API says it worked
        public async Task RemoveCourseAsync(Course? course)
        {
            if (course is null)
                return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                // a logged-out delete opens the login page, then carries on
                await _authService.RunWriteAsync(async () =>
                {
                    await _courseRepository.DeleteCourseAsync(course.ID);
                    return true;
                });
            }
            catch (Exception exception) when (exception is ApiRequestException or LoginCancelledException)
            {
                ErrorMessage = exception.Message;
                IsBusy = false;
                return;
            }

            IsBusy = false;
            await ShowDeletedCourseAsync(course);
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
            for (var courseIndex = 0; courseIndex < Courses.Count; courseIndex++)
            {
                if (Courses[courseIndex].ID == courseId)
                    return courseIndex;
            }

            return -1;
        }

        // rebuilds FilteredCourses from Courses using the search box. matches
        // the course name, the course code or the instructor
        private void ApplyFilter()
        {
            FilteredCourses.Clear();

            var query = SearchQuery?.Trim().ToLowerInvariant() ?? string.Empty;

            foreach (var course in Courses)
            {
                if (string.IsNullOrEmpty(query) ||
                    course.Name.ToLowerInvariant().Contains(query) ||
                    course.CourseCode.ToLowerInvariant().Contains(query) ||
                    course.Instructor.ToLowerInvariant().Contains(query))
                {
                    FilteredCourses.Add(course);
                }
            }
        }

        // lists every assignment that isn't graded yet, each starting at 90%
        private void BuildSimulationItems()
        {
            SimulatedAssignments.Clear();

            foreach (var course in Courses)
            {
                var ungradedAssignments = course.Assignments
                    .Where(assignment => !assignment.IsCompleted || !assignment.PointsEarned.HasValue);

                foreach (var assignment in ungradedAssignments)
                {
                    SimulatedAssignments.Add(new SimulatedAssignmentItem
                    {
                        AssignmentId = assignment.ID,
                        CourseId = course.ID,
                        CourseCode = course.CourseCode,
                        Title = assignment.Title,
                        PointsPossible = assignment.PointsPossible,
                        ExpectedPoints = Math.Round(assignment.PointsPossible * 0.9),
                        OnScoreChanged = RecalculateSimulatedGpa
                    });
                }
            }

            RecalculateSimulatedGpa();
        }

        // works out the GPA as if every simulated score really happens. graded
        // work counts as it is, simulated work gets added on top, then each
        // course's percent turns into a letter grade like the real calculation
        private void RecalculateSimulatedGpa()
        {
            var simulatedCourses = new List<Course>();

            foreach (var course in Courses)
            {
                var gradedAssignments = course.Assignments
                    .Where(assignment => assignment.IsCompleted && assignment.PointsEarned.HasValue && assignment.PointsPossible > 0)
                    .ToList();

                var pointsEarned = gradedAssignments.Sum(assignment => assignment.PointsEarned!.Value);
                var pointsPossible = gradedAssignments.Sum(assignment => assignment.PointsPossible);

                foreach (var simulatedAssignment in SimulatedAssignments.Where(item => item.CourseId == course.ID))
                {
                    pointsEarned += simulatedAssignment.ExpectedPoints;
                    pointsPossible += simulatedAssignment.PointsPossible;
                }

                if (pointsPossible > 0)
                {
                    var percent = (pointsEarned / pointsPossible) * 100.0;
                    var letterGrade = _gpaCalculator.GetLetterGradeFromPercent(percent);

                    simulatedCourses.Add(new Course
                    {
                        ID = course.ID,
                        Name = course.Name,
                        Credits = course.Credits,
                        CurrentGrade = letterGrade,
                        GradePoints = _gpaCalculator.GetGradePoints(letterGrade)
                    });
                }
                else
                {
                    // nothing to go on, so keep the course's real grade
                    simulatedCourses.Add(course);
                }
            }

            ProjectedGpa = _startingCredits > 0
                ? _gpaCalculator.CalculateGpa(simulatedCourses, _startingGpa, _startingCredits)
                : _gpaCalculator.CalculateGpa(simulatedCourses);

            ProjectedStatus = _gpaCalculator.GetStatus(ProjectedGpa, PtkGoodStandingGpa, PtkInitiationGpa);
        }

        private void RecalculateGpa()
        {
            // Courses just changed, so the search results need a refresh too
            ApplyFilter();

            CurrentGpa = _startingCredits > 0
                ? _gpaCalculator.CalculateGpa(Courses, _startingGpa, _startingCredits)
                : _gpaCalculator.CalculateGpa(Courses);

            Status = _gpaCalculator.GetStatus(CurrentGpa, PtkGoodStandingGpa, PtkInitiationGpa);

            // the actual dynamic-resource trigger — every time GPA changes,
            // the app's accent color updates to match how you're doing
            _themeService.ApplyGpaStatusTheme(Status);

            // keep the open simulator in step with the new real numbers
            if (IsSimulatorOpen)
                RecalculateSimulatedGpa();
        }

        private void OnCourseSelected(Course? course)
        {
            if (course is null)
                return;

            _ = _navigation.GoToAsync(
                Routes.CourseDetail,
                new Dictionary<string, object> { { QueryKeys.Course, course } });
        }
    }
}
