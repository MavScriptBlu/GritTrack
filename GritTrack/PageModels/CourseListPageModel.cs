using System.Collections.ObjectModel;
using GritTrack.Models;
using GritTrack.Services;

namespace GritTrack.PageModels
{
    public class CourseListPageModel : BaseViewModel
    {
        private readonly GpaCalculatorService _gpaCalculator;

        // talks to ICourseRepository, never to a concrete class — see that
        // interface's comments for why. Today it's in-memory sample data;
        // later it could be a REST API or SQLite without this ViewModel
        // changing at all.
        private readonly ICourseRepository _courseRepository;
        private const double PtkGpaThreshold = 3.5;

        public CourseListPageModel(GpaCalculatorService gpaCalculator, ICourseRepository courseRepository)
        {
            _gpaCalculator = gpaCalculator;
            _courseRepository = courseRepository;

            Courses = new ObservableCollection<Course>();

            // RelayCommand only takes a plain Action, so the async work runs
            // as fire-and-forget from the command's point of view — the
            // "_ =" discard is intentional, not a missed await
            LoadCoursesCommand = new RelayCommand(() => _ = LoadCoursesAsync());
            CourseSelectedCommand = new RelayCommand<Course>(OnCourseSelected);
            AddCourseCommand = new RelayCommand(() => AddCourseRequested?.Invoke(this, EventArgs.Empty));
            DeleteCourseCommand = new RelayCommand<Course>(course => _ = RemoveCourseAsync(course));
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

        // true only the very first time the list loads, so a course added
        // mid-session doesn't get wiped out by the next OnAppearing() reload
        private bool _hasLoaded;

        public RelayCommand LoadCoursesCommand { get; }
        public RelayCommand<Course> CourseSelectedCommand { get; }
        public RelayCommand AddCourseCommand { get; }
        public RelayCommand<Course> DeleteCourseCommand { get; }
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

            RecalculateGpa();

            _hasLoaded = true;
            IsBusy = false;
        }

        // handles both "add a brand new course" and "save edits to an
        // existing one" — persists through the repository first, then
        // mirrors the same change into the on-screen collection. If the ID
        // already exists, that row gets replaced in place (so CollectionView
        // updates the right card); otherwise it's appended.
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
            CurrentGpa = _gpaCalculator.CalculateGpa(Courses);
            Status = _gpaCalculator.GetStatus(CurrentGpa, PtkGpaThreshold);
        }

        private void OnCourseSelected(Course? course)
        {
            if (course is null)
                return;

            SelectedCourseRequested?.Invoke(this, course);
        }
    }
}
