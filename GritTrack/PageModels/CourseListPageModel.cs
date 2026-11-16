using System.Collections.ObjectModel;
using GritTrack.Models;
using GritTrack.Services;
using GritTrack.Services.Api;
using GritTrack.Services.Offline;
using GritTrack.Services.Photos;
using GritTrack.Services.Reminders;

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
        private readonly ISyncService _sync;
        private readonly IPhotoService _photoService;
        private readonly IReminderService _reminderService;

        private const double PtkInitiationGpa = 3.5;
        private const double PtkGoodStandingGpa = 3.0;

        public CourseListPageModel(GpaCalculatorService gpaCalculator, ICourseRepository courseRepository, IGpaProfileRepository gpaProfileRepository, IThemeService themeService, INavigationService navigation, IAuthService authService, ISyncService sync, IPhotoService photoService, IReminderService reminderService)
        {
            _authService = authService;
            _sync = sync;
            _photoService = photoService;
            _reminderService = reminderService;

            SyncCommand = new RelayCommand(() => _ = SyncAsync());
            DiscardProblemsCommand = new RelayCommand(() => _ = DiscardProblemsAsync());

            // the waiting list can change from other pages (a save on the add page),
            // so listen for it instead of guessing when to refresh the card
            _sync.Changed += (_, _) => MainThread.BeginInvokeOnMainThread(RaiseSyncProperties);
            _gpaCalculator = gpaCalculator;
            _courseRepository = courseRepository;
            _gpaProfileRepository = gpaProfileRepository;
            _themeService = themeService;
            _navigation = navigation;

            Courses = new ObservableCollection<Course>();
            FilteredCourses = new ObservableCollection<Course>();
            SimulatedAssignments = new ObservableCollection<SimulatedAssignmentItem>();

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
                var difference = ProjectedGpa - CurrentGpa;
                if (difference > 0.005) return $"+{difference:F2}";
                if (difference < -0.005) return $"{difference:F2}";
                return "\u00B10.00";
            }
        }

        private double _currentGpa;
        public double CurrentGpa
        {
            get => _currentGpa;
            set => SetProperty(ref _currentGpa, value);
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

        // ---- offline mode: the "waiting to sync" card ----

        public bool ShowSyncCard => _sync.IsOffline || _sync.PendingCount > 0 || HasSyncMessage;

        public string SyncTitle => _sync.PendingCount switch
        {
            0 => _sync.IsOffline ? "You're offline" : "Sync",
            1 => "1 change waiting to sync",
            _ => $"{_sync.PendingCount} changes waiting to sync"
        };

        public string SyncDetail => _sync.IsOffline
            ? "Can't reach the server, so this is the copy saved on this device. Anything you change is kept here and sent later."
            : "Saved on this device. Tap Sync Now to send it to the server.";

        public bool CanSync => _sync.PendingCount > 0;

        public bool HasSyncProblems => _sync.Problems.Count > 0;

        private bool _isSyncing;
        public bool IsSyncing
        {
            get => _isSyncing;
            set => SetProperty(ref _isSyncing, value);
        }

        // what the last sync did, in plain words
        private string _syncMessage = string.Empty;
        public string SyncMessage
        {
            get => _syncMessage;
            set
            {
                SetProperty(ref _syncMessage, value);
                OnPropertyChanged(nameof(HasSyncMessage));
                OnPropertyChanged(nameof(ShowSyncCard));
            }
        }

        public bool HasSyncMessage => !string.IsNullOrEmpty(SyncMessage);

        public RelayCommand SyncCommand { get; }
        public RelayCommand DiscardProblemsCommand { get; }

        private void RaiseSyncProperties()
        {
            OnPropertyChanged(nameof(ShowSyncCard));
            OnPropertyChanged(nameof(SyncTitle));
            OnPropertyChanged(nameof(SyncDetail));
            OnPropertyChanged(nameof(CanSync));
            OnPropertyChanged(nameof(HasSyncProblems));
        }

        private async Task SyncAsync()
        {
            if (IsSyncing)
                return;

            IsSyncing = true;
            SyncMessage = string.Empty;

            try
            {
                // sending changes is a write, so it needs a login like any other save.
                // signed out? the login page opens and the sync carries on after
                var result = await _authService.RunWriteAsync(() => _sync.SyncAsync());
                SyncMessage = DescribeSync(result);

                // pull the list again so real IDs and server values replace the temporary ones
                await LoadCoursesAsync(forceReload: true);
            }
            catch (LoginCancelledException)
            {
                SyncMessage = "Sign in to send your changes. They're still safe on this device.";
            }
            finally
            {
                IsSyncing = false;
                RaiseSyncProperties();
            }
        }

        private string DescribeSync(SyncResult result)
        {
            if (result.StillOffline)
                return $"Still can't reach the server. {Count(result.StillWaitingCount + result.ProblemCount, "change")} safe on this device. Try again in a bit.";

            if (result.ProblemCount > 0)
                return $"{Count(result.SentCount, "change")} sent. The server turned down {result.ProblemCount}:{Environment.NewLine}{string.Join(Environment.NewLine, _sync.Problems)}";

            return $"All caught up. {Count(result.SentCount, "change")} sent.";
        }

        private static string Count(int number, string word) => number == 1 ? $"1 {word}" : $"{number} {word}s";

        private async Task DiscardProblemsAsync()
        {
            var removed = await _sync.DiscardProblemsAsync();
            SyncMessage = $"Threw away {Count(removed, "change")} the server wouldn't take.";
            await LoadCoursesAsync(forceReload: true);
        }

        private double _startingGpa;
        private double _startingCredits;

        private bool _isEditingStartingGpa;
        public bool IsEditingStartingGpa
        {
            get => _isEditingStartingGpa;
            set => SetProperty(ref _isEditingStartingGpa, value);
        }

        private string _startingGpaInput = string.Empty;
        public string StartingGpaInput
        {
            get => _startingGpaInput;
            set => SetProperty(ref _startingGpaInput, value);
        }

        private string _startingCreditsInput = string.Empty;
        public string StartingCreditsInput
        {
            get => _startingCreditsInput;
            set => SetProperty(ref _startingCreditsInput, value);
        }

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

        private bool _hasStartingGpaValidationMessage;
        public bool HasStartingGpaValidationMessage
        {
            get => _hasStartingGpaValidationMessage;
            set => SetProperty(ref _hasStartingGpaValidationMessage, value);
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

                var profile = await _gpaProfileRepository.GetProfileAsync();
                _startingGpa = profile.StartingGpa;
                _startingCredits = profile.StartingCredits;
                StartingGpaInput = _startingCredits > 0 ? _startingGpa.ToString("0.##") : string.Empty;
                StartingCreditsInput = _startingCredits > 0 ? _startingCredits.ToString("0.##") : string.Empty;

                RecalculateGpa();
                RaiseSyncProperties();

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
            var index = IndexOf(course.ID);
            if (index >= 0)
                Courses[index] = course;
            else
                Courses.Add(course);

            ErrorMessage = string.Empty;
            RecalculateGpa();

            if (course.IsPendingSync)
                SyncMessage = $"{course.CourseCode} is saved on this device. It goes to the server when you sync.";
        }

        private async Task ShowDeletedCourseAsync(Course course)
        {
            var index = IndexOf(course.ID);
            if (index >= 0)
                Courses.RemoveAt(index);

            RecalculateGpa();

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

            // the course is gone, so its reminders and photos go with it
            foreach (var assignment in course.Assignments)
            {
                _reminderService.Cancel(assignment);
                _photoService.Delete(assignment.PhotoFileName);
            }

            IsBusy = false;
            await ShowDeletedCourseAsync(course);
        }

        private async Task SaveStartingGpaAsync()
        {
            var gpaText = StartingGpaInput?.Trim();
            var creditsText = StartingCreditsInput?.Trim();

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
