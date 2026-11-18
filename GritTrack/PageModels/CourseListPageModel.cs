using System.Collections.ObjectModel;
using System.Globalization;
using GritTrack.Models;
using GritTrack.Services;
using GritTrack.Services.Api;

namespace GritTrack.PageModels
{
    public class CourseListPageModel : BaseViewModel, IQueryAttributable
    {
        private readonly IAuthService _authService;

        private readonly GpaCalculatorService _gpaCalculator;

        private readonly ICourseRepository _courseRepository;
        private readonly IGpaProfileRepository _gpaProfileRepository;
        private readonly IThemeService _themeService;
        private readonly INavigationService _navigation;

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
            SimulatedCourseResults = new ObservableCollection<SimulatedCourseResult>();

            LoadCoursesCommand = new RelayCommand(() => _ = LoadCoursesAsync(forceReload: false));
            RefreshCommand = new RelayCommand(() => _ = LoadCoursesAsync(forceReload: true));
            CourseSelectedCommand = new RelayCommand<Course>(OnCourseSelected);
            AddCourseCommand = new RelayCommand(() => _ = _navigation.GoToAsync(Routes.AddCourse));

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
            ClearSearchCommand = new RelayCommand(() => SearchQuery = string.Empty);

            ToggleSimulatorCommand = new RelayCommand(() => _ = ToggleSimulatorAsync());
            ApplyPresetScenarioCommand = new RelayCommand<string>(ApplyPresetScenario);

            AuthCommand = new RelayCommand(() => _ = ToggleSignInAsync());
            _authService.SignInChanged += (_, _) => OnPropertyChanged(nameof(AuthButtonText));

            _ = LoadCoursesAsync(forceReload: false);
        }

        public string AuthButtonText => _authService.IsSignedIn ? "Sign out" : "Sign in";

        private async Task ToggleSignInAsync()
        {
            if (_authService.IsSignedIn)
                await _authService.SignOutAsync();
            else
                await _authService.RequestLoginAsync();
        }

        private ObservableCollection<Course> _courses = new();
        public ObservableCollection<Course> Courses
        {
            get => _courses;
            set => SetProperty(ref _courses, value);
        }

        public ObservableCollection<Course> FilteredCourses { get; }
        public ObservableCollection<SimulatedAssignmentItem> SimulatedAssignments { get; }

        // one line per course the simulator touches: "CS-160: B to A- (91%)"
        public ObservableCollection<SimulatedCourseResult> SimulatedCourseResults { get; }

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

        public bool HasSearchQuery => !string.IsNullOrWhiteSpace(SearchQuery);

        private bool _isSimulatorOpen;
        public bool IsSimulatorOpen
        {
            get => _isSimulatorOpen;
            set => SetProperty(ref _isSimulatorOpen, value);
        }

        // false = nothing upcoming to play with, so the panel shows an
        // explanation instead of buttons that look like they do nothing
        private bool _hasSimulatedAssignments;
        public bool HasSimulatedAssignments
        {
            get => _hasSimulatedAssignments;
            set => SetProperty(ref _hasSimulatedAssignments, value);
        }

        // the percent the last preset button set everything to. null once you
        // move a slider yourself, because then it's your own mix of scores
        private double? _scenarioPercent;
        private double? ScenarioPercent
        {
            get => _scenarioPercent;
            set
            {
                if (SetProperty(ref _scenarioPercent, value))
                    OnPropertyChanged(nameof(ScenarioText));
            }
        }

        public string ScenarioText => ScenarioPercent.HasValue
            ? $"Scenario: {ScenarioPercent.Value:0}% on every upcoming assignment"
            : "Scenario: your own scores";

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

        public string GpaDeltaText
        {
            get
            {
                double difference = ProjectedGpa - CurrentGpa;
                if (difference > 0.005) return $"+{difference:F2}";
                if (difference < -0.005) return $"{difference:F2}";
                return "\u00B10.00";
            }
        }

        private double _currentGpa;
        public double CurrentGpa
        {
            get => _currentGpa;
            set
            {
                if (SetProperty(ref _currentGpa, value))
                    OnPropertyChanged(nameof(GpaDeltaText));
            }
        }

        private GpaStatus _status;
        public GpaStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private bool _hasLoaded;

        // true while a preset button is setting every slider, so the math
        // runs once at the end instead of once per slider
        private bool _isApplyingPreset;

        private double _startingGpa;
        private double _startingCredits;

        private bool _isEditingStartingGpa;
        public bool IsEditingStartingGpa
        {
            get => _isEditingStartingGpa;
            set => SetProperty(ref _isEditingStartingGpa, value);
        }

        // typing in a box wipes that box's error so the pink goes away as you fix it
        private string _startingGpaInput = string.Empty;
        public string StartingGpaInput
        {
            get => _startingGpaInput;
            set
            {
                if (SetProperty(ref _startingGpaInput, value))
                    StartingGpaError = string.Empty;
            }
        }

        private string _startingCreditsInput = string.Empty;
        public string StartingCreditsInput
        {
            get => _startingCreditsInput;
            set
            {
                if (SetProperty(ref _startingCreditsInput, value))
                    StartingCreditsError = string.Empty;
            }
        }

        private string _startingGpaError = string.Empty;
        public string StartingGpaError
        {
            get => _startingGpaError;
            set => SetProperty(ref _startingGpaError, value);
        }

        private string _startingCreditsError = string.Empty;
        public string StartingCreditsError
        {
            get => _startingCreditsError;
            set => SetProperty(ref _startingCreditsError, value);
        }

        private bool _isConfirmingDelete;
        public bool IsConfirmingDelete
        {
            get => _isConfirmingDelete;
            set => SetProperty(ref _isConfirmingDelete, value);
        }

        private Course? _pendingDeleteCourse;
        public Course? PendingDeleteCourse
        {
            get => _pendingDeleteCourse;
            set => SetProperty(ref _pendingDeleteCourse, value);
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

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue(QueryKeys.SavedCourse, out object? saved) && saved is Course savedCourse)
            {
                ShowSavedCourse(savedCourse);
                query.Remove(QueryKeys.SavedCourse);
            }

            if (query.TryGetValue(QueryKeys.DeletedCourse, out object? deleted) && deleted is Course deletedCourse)
            {
                _ = ShowDeletedCourseAsync(deletedCourse);
                query.Remove(QueryKeys.DeletedCourse);
            }
        }

        private async Task LoadCoursesAsync(bool forceReload)
        {
            if (_hasLoaded && !forceReload)
            {
                IsBusy = false;
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                List<Course> courses = await _courseRepository.GetCoursesAsync();
                GpaProfile profile = await _gpaProfileRepository.GetProfileAsync();

                // ObservableCollection changes must happen on the main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Courses.Clear();
                    foreach (Course course in courses)
                        Courses.Add(course);

                    _startingGpa = profile.StartingGpa;
                    _startingCredits = profile.StartingCredits;
                    StartingGpaInput = _startingCredits > 0 ? _startingGpa.ToString("0.##") : string.Empty;
                    StartingCreditsInput = _startingCredits > 0 ? _startingCredits.ToString("0.##") : string.Empty;

                    RecalculateGpa();
                });

                _hasLoaded = true;
            }
            catch (Exception)
            {
                ErrorMessage = "Couldn't load your courses. Check that the API is running and try again.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ShowSavedCourse(Course course)
        {
            // ObservableCollection changes must happen on the main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                int index = IndexOf(course.ID);
                if (index >= 0)
                    Courses[index] = course;
                else
                    Courses.Add(course);

                ErrorMessage = string.Empty;
                RecalculateGpa();
            });
        }

        private async Task ShowDeletedCourseAsync(Course course)
        {
            // ObservableCollection changes must happen on the main thread for CollectionView to update
            MainThread.BeginInvokeOnMainThread(() =>
            {
                int index = IndexOf(course.ID);
                if (index >= 0)
                    Courses.RemoveAt(index);

                RecalculateGpa();
            });

            await _navigation.ShowAlertAsync("Removed", $"{course.CourseCode} was removed.");
        }

        public async Task RemoveCourseAsync(Course? course)
        {
            if (course is null)
                return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
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

        private async Task SaveStartingGpaAsync()
        {
            string? gpaText = StartingGpaInput?.Trim();
            string? creditsText = StartingCreditsInput?.Trim();

            StartingGpaError = string.Empty;
            StartingCreditsError = string.Empty;

            // both boxes empty = clear the starting GPA
            if (string.IsNullOrEmpty(gpaText) && string.IsNullOrEmpty(creditsText))
            {
                _startingGpa = 0;
                _startingCredits = 0;
                await _gpaProfileRepository.SaveProfileAsync(new GpaProfile { StartingGpa = 0, StartingCredits = 0 });
                RecalculateGpa();
                IsEditingStartingGpa = false;
                return;
            }

            // check both boxes so every bad one lights up at the same time
            bool isGpaValid = double.TryParse(gpaText, out double gpa) && gpa >= 0 && gpa <= 4.0;
            bool areCreditsValid = double.TryParse(creditsText, out double credits) && credits > 0;

            if (!isGpaValid)
                StartingGpaError = "GPA needs to be a number from 0.0 to 4.0.";

            if (!areCreditsValid)
                StartingCreditsError = "Credits needs to be a number greater than 0.";

            if (!isGpaValid || !areCreditsValid)
                return;

            _startingGpa = gpa;
            _startingCredits = credits;
            await _gpaProfileRepository.SaveProfileAsync(new GpaProfile { StartingGpa = gpa, StartingCredits = credits });

            RecalculateGpa();
            IsEditingStartingGpa = false;
        }

        private int IndexOf(int courseId)
        {
            for (int courseIndex = 0; courseIndex < Courses.Count; courseIndex++)
            {
                if (Courses[courseIndex].ID == courseId)
                    return courseIndex;
            }

            return -1;
        }

        private void ApplyFilter()
        {
            FilteredCourses.Clear();

            string query = SearchQuery?.Trim().ToLowerInvariant() ?? string.Empty;

            foreach (Course course in Courses)
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

        // open = grab the freshest assignments first, then build the sliders.
        // the Mid-State Hub saves imported assignments to a local file, and this
        // page loaded its courses long before that. without the refresh, the
        // simulator opened with nothing in it even right after an import
        private async Task ToggleSimulatorAsync()
        {
            if (IsSimulatorOpen)
            {
                IsSimulatorOpen = false;
                return;
            }

            foreach (Course course in Courses)
            {
                try
                {
                    course.Assignments = await _courseRepository.GetAssignmentsLocalAsync(course.ID);
                }
                catch (Exception)
                {
                    // can't read this course's file, so keep whatever it already had
                }
            }

            BuildSimulationItems();
            IsSimulatorOpen = true;
        }

        // one slider per assignment that isn't graded yet. worth 0 points = nothing
        // to simulate, so those stay out
        private void BuildSimulationItems()
        {
            SimulatedAssignments.Clear();

            foreach (Course course in Courses)
            {
                IEnumerable<Assignment> ungradedAssignments = course.Assignments
                    .Where(assignment => (!assignment.IsCompleted || !assignment.PointsEarned.HasValue) && assignment.PointsPossible > 0);

                foreach (Assignment assignment in ungradedAssignments)
                {
                    SimulatedAssignments.Add(new SimulatedAssignmentItem
                    {
                        AssignmentId = assignment.ID,
                        CourseId = course.ID,
                        CourseCode = course.CourseCode,
                        Title = assignment.Title,
                        PointsPossible = assignment.PointsPossible,
                        ExpectedPoints = Math.Round(assignment.PointsPossible * 0.9),
                        OnScoreChanged = OnSimulatedScoreChanged
                    });
                }
            }

            HasSimulatedAssignments = SimulatedAssignments.Count > 0;
            ScenarioPercent = 90;
            RecalculateSimulatedGpa();
        }

        // preset buttons: "what if I get X% on everything coming up?"
        private void ApplyPresetScenario(string? percentText)
        {
            if (!double.TryParse(percentText, NumberStyles.Float, CultureInfo.InvariantCulture, out double percent))
                return;

            // every slider fires its own change. hold the math until they're all set
            _isApplyingPreset = true;
            try
            {
                foreach (SimulatedAssignmentItem simulatedAssignment in SimulatedAssignments)
                    simulatedAssignment.ExpectedPoints = Math.Round(simulatedAssignment.PointsPossible * (percent / 100.0));
            }
            finally
            {
                _isApplyingPreset = false;
            }

            ScenarioPercent = percent;
            RecalculateSimulatedGpa();
        }

        // you dragged a slider yourself, so it's no longer "X% on everything"
        private void OnSimulatedScoreChanged()
        {
            if (_isApplyingPreset)
                return;

            ScenarioPercent = null;
            RecalculateSimulatedGpa();
        }

        private void RecalculateSimulatedGpa()
        {
            List<Course> simulatedCourses = new List<Course>();
            SimulatedCourseResults.Clear();

            foreach (Course course in Courses)
            {
                List<SimulatedAssignmentItem> courseItems = SimulatedAssignments.Where(item => item.CourseId == course.ID).ToList();

                // nothing simulated here, so this course keeps its real grade.
                // (rebuilding it from assignment points would make the GPA move
                // before you touched a single slider)
                if (courseItems.Count == 0)
                {
                    simulatedCourses.Add(course);
                    continue;
                }

                List<Assignment> gradedAssignments = course.Assignments
                    .Where(assignment => assignment.IsCompleted && assignment.PointsEarned.HasValue && assignment.PointsPossible > 0)
                    .ToList();

                double pointsEarned = gradedAssignments.Sum(assignment => assignment.PointsEarned!.Value);
                double pointsPossible = gradedAssignments.Sum(assignment => assignment.PointsPossible);

                foreach (SimulatedAssignmentItem simulatedAssignment in courseItems)
                {
                    pointsEarned += simulatedAssignment.ExpectedPoints;
                    pointsPossible += simulatedAssignment.PointsPossible;
                }

                double percent = (pointsEarned / pointsPossible) * 100.0;
                string letterGrade = _gpaCalculator.GetLetterGradeFromPercent(percent);

                simulatedCourses.Add(new Course
                {
                    ID = course.ID,
                    Name = course.Name,
                    Credits = course.Credits,
                    CurrentGrade = letterGrade,
                    GradePoints = _gpaCalculator.GetGradePoints(letterGrade)
                });

                SimulatedCourseResults.Add(new SimulatedCourseResult
                {
                    CourseCode = string.IsNullOrWhiteSpace(course.CourseCode) ? course.Name : course.CourseCode,
                    CurrentGrade = course.CurrentGrade,
                    ProjectedGrade = letterGrade,
                    ProjectedPercent = percent
                });
            }

            ProjectedGpa = _startingCredits > 0
                ? _gpaCalculator.CalculateGpa(simulatedCourses, _startingGpa, _startingCredits)
                : _gpaCalculator.CalculateGpa(simulatedCourses);

            ProjectedStatus = _gpaCalculator.GetStatus(ProjectedGpa, PtkGoodStandingGpa, PtkInitiationGpa);
        }

        private void RecalculateGpa()
        {
            ApplyFilter();

            CurrentGpa = _startingCredits > 0
                ? _gpaCalculator.CalculateGpa(Courses, _startingGpa, _startingCredits)
                : _gpaCalculator.CalculateGpa(Courses);

            Status = _gpaCalculator.GetStatus(CurrentGpa, PtkGoodStandingGpa, PtkInitiationGpa);

            _themeService.ApplyGpaStatusTheme(Status);

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
