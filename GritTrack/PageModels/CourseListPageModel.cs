using System.Collections.ObjectModel;
using GritTrack.Models;
using GritTrack.Services;

namespace GritTrack.PageModels
{
    public class CourseListPageModel : BaseViewModel
    {
        private readonly GpaCalculatorService _gpaCalculator;
        private const double PtkGpaThreshold = 3.5;

        public CourseListPageModel(GpaCalculatorService gpaCalculator)
        {
            _gpaCalculator = gpaCalculator;

            Courses = new ObservableCollection<Course>();
            LoadCoursesCommand = new RelayCommand(LoadCourses);
            CourseSelectedCommand = new RelayCommand<Course>(OnCourseSelected);
        }

        private ObservableCollection<Course> _courses = new();
        public ObservableCollection<Course> Courses
        {
            get => _courses;
            set => SetProperty(ref _courses, value);
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

        public RelayCommand LoadCoursesCommand { get; }
        public RelayCommand<Course> CourseSelectedCommand { get; }
        public event EventHandler<Course>? SelectedCourseRequested;

        private void LoadCourses()
        {
            IsBusy = true;

            Courses.Clear();
            foreach (var course in GetSampleCourses())
                Courses.Add(course);

            CurrentGpa = _gpaCalculator.CalculateGpa(Courses);
            Status = _gpaCalculator.GetStatus(CurrentGpa, PtkGpaThreshold);

            IsBusy = false;
        }

        private void OnCourseSelected(Course? course)
        {
            if (course is null)
                return;

            SelectedCourseRequested?.Invoke(this, course);
        }

        private static List<Course> GetSampleCourses()
        {
            return new List<Course>
            {
                new Course { ID = 1, Name = "Programming Fundamentals", CourseCode = "CS-101", Instructor = "Staff", Credits = 3, CurrentGrade = "A-", GradePoints = 3.7 },
                new Course { ID = 2, Name = "Database Concepts and Design", CourseCode = "CS-210", Instructor = "Staff", Credits = 3, CurrentGrade = "B+", GradePoints = 3.3 },
                new Course { ID = 3, Name = "Web Development I", CourseCode = "CS-150", Instructor = "Staff", Credits = 3, CurrentGrade = "A", GradePoints = 4.0 }
            };
        }
    }
}
