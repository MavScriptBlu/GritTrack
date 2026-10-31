using System.Collections.ObjectModel;
using System.Globalization;
using GritTrack.Models;
using GritTrack.Services;
using GritTrack.Services.Api;

namespace GritTrack.PageModels
{
    public class CampusPortalItem
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string AccentHex { get; set; } = "#9fe0bd";

        public Color AccentTint => Color.FromArgb(AccentHex);
        public Brush AccentBrush => new SolidColorBrush(AccentTint);
    }

    public class MidStateHubPageModel : BaseViewModel
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IAuthService _authService;

        public ObservableCollection<CampusPortalItem> Portals { get; }
        public ObservableCollection<Course> AvailableCourses { get; }

        public ObservableCollection<Assignment> ParsedAssignments { get; }

        public RelayCommand<string> OpenUrlCommand { get; }
        public RelayCommand ToggleCoursePickerCommand { get; }
        public RelayCommand<Course> SelectCourseCommand { get; }
        public RelayCommand ParseSyllabusCommand { get; }
        public RelayCommand ImportSyllabusCommand { get; }

        private Course? _selectedCourse;
        public Course? SelectedCourse
        {
            get => _selectedCourse;
            set
            {
                if (SetProperty(ref _selectedCourse, value))
                {
                    OnPropertyChanged(nameof(SelectedCourseDisplay));
                    ImportSyllabusCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string SelectedCourseDisplay
        {
            get
            {
                if (SelectedCourse is not null)
                    return $"{SelectedCourse.CourseCode} - {SelectedCourse.Name}";

                return HasNoCourses
                    ? "No courses yet. Add one on the Courses tab."
                    : "Tap to pick a course...";
            }
        }

        public bool HasNoCourses => AvailableCourses.Count == 0;

        private bool _isCoursePickerOpen;
        public bool IsCoursePickerOpen
        {
            get => _isCoursePickerOpen;
            set => SetProperty(ref _isCoursePickerOpen, value);
        }

        private string _syllabusRawText = string.Empty;
        public string SyllabusRawText
        {
            get => _syllabusRawText;
            set
            {
                if (SetProperty(ref _syllabusRawText, value))
                    ParsedAssignments.Clear();
            }
        }

        public bool HasParsedAssignments => ParsedAssignments.Count > 0;

        private bool _isImporting;

        private string _importStatusMessage = string.Empty;
        public string ImportStatusMessage
        {
            get => _importStatusMessage;
            set
            {
                if (SetProperty(ref _importStatusMessage, value))
                    OnPropertyChanged(nameof(HasImportStatusMessage));
            }
        }

        public bool HasImportStatusMessage => !string.IsNullOrWhiteSpace(ImportStatusMessage);

        private bool _isImportError;
        public bool IsImportError
        {
            get => _isImportError;
            set => SetProperty(ref _isImportError, value);
        }

        public MidStateHubPageModel(ICourseRepository courseRepository, IAuthService authService)
        {
            _courseRepository = courseRepository;
            _authService = authService;

            AvailableCourses = new ObservableCollection<Course>();
            ParsedAssignments = new ObservableCollection<Assignment>();

            Portals = new ObservableCollection<CampusPortalItem>
            {
                new()
                {
                    Name = "Blackboard Learn / Ultra",
                    Category = "LMS & Classes",
                    Description = "Mid-State course modules, assignment dropboxes, syllabus, and grades.",
                    Url = "https://mstc.blackboard.com",
                    AccentHex = "#9fe0bd"
                },
                new()
                {
                    Name = "Microsoft Teams",
                    Category = "Collaboration",
                    Description = "MSTC class channels, study sessions, and instructor office hours.",
                    Url = "https://teams.microsoft.com",
                    AccentHex = "#a3cbee"
                },
                new()
                {
                    Name = "Jira Software",
                    Category = "Collaboration",
                    Description = "Sprint planning boards, project backlogs, and issue tracking.",
                    Url = "https://www.atlassian.com/software/jira",
                    AccentHex = "#c3b0f2"
                },
                new()
                {
                    Name = "Mid-State Engage",
                    Category = "Student Life",
                    Description = "Campus events, PTK Beta Chi Theta meetings, and student organizations.",
                    Url = "https://engage.midstate.edu",
                    AccentHex = "#f2e3a6"
                },
                new()
                {
                    Name = "Microsoft MyApps (SSO)",
                    Category = "Portals & Admin",
                    Description = "Single sign-on launcher for Office 365, student email, and OneDrive.",
                    Url = "https://myapps.microsoft.com",
                    AccentHex = "#ffcfa6"
                },
                new()
                {
                    Name = "MyMSTC Student Center",
                    Category = "Portals & Admin",
                    Description = "Course registration, transcripts, financial aid, and billing.",
                    Url = "https://mymstc.mstc.edu",
                    AccentHex = "#f6adbf"
                }
            };

            OpenUrlCommand = new RelayCommand<string>(url => _ = OpenPortalAsync(url));

            ToggleCoursePickerCommand = new RelayCommand(() =>
            {
                if (!IsCoursePickerOpen && AvailableCourses.Count == 0)
                    _ = LoadCoursesAsync();

                IsCoursePickerOpen = !IsCoursePickerOpen;
            });

            SelectCourseCommand = new RelayCommand<Course>(course =>
            {
                if (course is not null)
                    SelectedCourse = course;

                IsCoursePickerOpen = false;
            });

            ParseSyllabusCommand = new RelayCommand(ParseSyllabus);

            ImportSyllabusCommand = new RelayCommand(() => _ = ImportSyllabusAsync(), CanImport);

            ParsedAssignments.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(HasParsedAssignments));
                ImportSyllabusCommand.RaiseCanExecuteChanged();
            };
        }

        public async Task LoadCoursesAsync()
        {
            try
            {
                var courses = await _courseRepository.GetCoursesAsync();
                var previousCourseId = SelectedCourse?.ID;

                AvailableCourses.Clear();
                foreach (var loadedCourse in courses)
                    AvailableCourses.Add(loadedCourse);

                SelectedCourse = AvailableCourses.FirstOrDefault(availableCourse => availableCourse.ID == previousCourseId)
                    ?? AvailableCourses.FirstOrDefault();

                OnPropertyChanged(nameof(HasNoCourses));
                OnPropertyChanged(nameof(SelectedCourseDisplay));
            }
            catch (ApiRequestException exception)
            {
                ShowStatus(exception.Message, isError: true);
            }
            catch (Exception)
            {
                ShowStatus("Couldn't load your courses. Try switching tabs again.", isError: true);
            }
        }

        private async Task OpenPortalAsync(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            try
            {
                await Launcher.Default.OpenAsync(new Uri(url));
            }
            catch (Exception)
            {
                ShowStatus("Couldn't open that link.", isError: true);
            }
        }

        private bool CanImport() => SelectedCourse is not null && ParsedAssignments.Count > 0 && !_isImporting;

        private void ParseSyllabus()
        {
            if (string.IsNullOrWhiteSpace(SyllabusRawText))
            {
                ShowStatus("Paste at least one syllabus line to parse.", isError: true);
                return;
            }

            var lines = SyllabusRawText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var parsedAssignments = new List<Assignment>();

            foreach (var line in lines)
            {
                var parts = line.Split('|', StringSplitOptions.TrimEntries);
                var title = parts[0];
                if (string.IsNullOrWhiteSpace(title))
                    continue;

                var dueDate = parts.Length > 1 && DateTime.TryParse(parts[1], out var parsedDate)
                    ? parsedDate
                    : DateTime.Today.AddDays(5);

                var pointsText = parts.Length > 2
                    ? new string(parts[2].Where(character => char.IsDigit(character) || character == '.').ToArray())
                    : string.Empty;
                var pointsPossible = double.TryParse(pointsText, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedPoints) && parsedPoints > 0
                    ? parsedPoints
                    : 50;

                parsedAssignments.Add(new Assignment
                {
                    ID = Random.Shared.Next(10000, 999999),
                    Title = title,
                    Type = AssignmentType.Homework,
                    DueDate = dueDate,
                    PointsPossible = pointsPossible,
                    Notes = "Imported from mstc.blackboard.com schedule"
                });
            }

            ParsedAssignments.Clear();

            if (parsedAssignments.Count == 0)
            {
                ShowStatus("Couldn't find any valid assignment titles to parse.", isError: true);
                return;
            }

            foreach (var parsedAssignment in parsedAssignments)
                ParsedAssignments.Add(parsedAssignment);

            ShowStatus($"Found {ParsedAssignments.Count} assignment(s). Check them, then hit Import.", isError: false);
        }

        private async Task ImportSyllabusAsync()
        {
            var targetCourse = SelectedCourse;
            var importedAssignments = ParsedAssignments.ToList();

            if (targetCourse is null || importedAssignments.Count == 0)
                return;

            _isImporting = true;
            ImportSyllabusCommand.RaiseCanExecuteChanged();

            try
            {
                if (!_authService.IsSignedIn && !await _authService.RequestLoginAsync())
                    throw new LoginCancelledException();

                var result = await _courseRepository.SaveAssignmentsLocalAsync(targetCourse.ID, importedAssignments);

                foreach (var addedAssignment in result.AddedAssignments)
                {
                    if (!targetCourse.Assignments.Contains(addedAssignment))
                        targetCourse.Assignments.Add(addedAssignment);
                }

                SyllabusRawText = string.Empty;

                var skippedNote = result.SkippedCount > 0 ? $" ({result.SkippedCount} duplicate(s) skipped)" : string.Empty;
                ShowStatus(
                    result.AddedCount > 0
                        ? $"Imported {result.AddedCount} assignment(s) to {targetCourse.Name}{skippedNote}."
                        : $"Nothing new: all {result.SkippedCount} were already in {targetCourse.Name}.",
                    isError: false);
            }
            catch (Exception exception) when (exception is ApiRequestException or LoginCancelledException)
            {
                ShowStatus(exception.Message, isError: true);
            }
            catch (Exception)
            {
                ShowStatus("Couldn't save the assignments on this phone. Try again.", isError: true);
            }
            finally
            {
                _isImporting = false;
                ImportSyllabusCommand.RaiseCanExecuteChanged();
            }
        }

        private void ShowStatus(string message, bool isError)
        {
            IsImportError = isError;
            ImportStatusMessage = message;
        }
    }
}
