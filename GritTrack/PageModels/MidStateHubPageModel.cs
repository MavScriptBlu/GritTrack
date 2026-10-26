using System.Collections.ObjectModel;
using GritTrack.Models;
using GritTrack.Services;

namespace GritTrack.PageModels
{
    public class CampusPortalItem
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string AccentHex { get; set; } = "#9fe0bd";
    }

    public class MidStateHubPageModel : BaseViewModel
    {
        private readonly ICourseRepository _courseRepository;

        public ObservableCollection<CampusPortalItem> Portals { get; }
        public RelayCommand<string> OpenUrlCommand { get; }
        public RelayCommand ImportSyllabusCommand { get; }

        private string _syllabusRawText = string.Empty;
        public string SyllabusRawText
        {
            get => _syllabusRawText;
            set => SetProperty(ref _syllabusRawText, value);
        }

        private string _importStatusMessage = string.Empty;
        public string ImportStatusMessage
        {
            get => _importStatusMessage;
            set => SetProperty(ref _importStatusMessage, value);
        }

        public MidStateHubPageModel(ICourseRepository courseRepository)
        {
            _courseRepository = courseRepository;

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

            OpenUrlCommand = new RelayCommand<string>(async url =>
            {
                if (!string.IsNullOrWhiteSpace(url))
                    await Launcher.Default.OpenAsync(new Uri(url));
            });

            ImportSyllabusCommand = new RelayCommand(() => _ = ParseAndImportSyllabusAsync());
        }

        private async Task ParseAndImportSyllabusAsync()
        {
            if (string.IsNullOrWhiteSpace(SyllabusRawText)) return;

            var courses = await _courseRepository.GetCoursesAsync();
            var target = courses.FirstOrDefault();
            if (target is null) return;

            var lines = SyllabusRawText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            int count = 0;

            foreach (var line in lines)
            {
                var parts = line.Split('|', StringSplitOptions.TrimEntries);
                var title = parts[0];
                var due = parts.Length > 1 && DateTime.TryParse(parts[1], out var d) ? d : DateTime.Today.AddDays(5);
                var pts = parts.Length > 2 && double.TryParse(new string(parts[2].Where(char.IsDigit).ToArray()), out var p) ? p : 50;

                target.Assignments.Add(new Assignment
                {
                    ID = Random.Shared.Next(10000, 999999),
                    CourseID = target.ID,
                    Title = title,
                    Type = AssignmentType.Homework,
                    DueDate = due,
                    PointsPossible = pts,
                    Notes = "Imported from mstc.blackboard.com schedule"
                });
                count++;
            }

            await _courseRepository.SaveCourseAsync(target);
            SyllabusRawText = string.Empty;
            ImportStatusMessage = $"Imported {count} assignment(s) into {target.Name}!";
        }
    }
}