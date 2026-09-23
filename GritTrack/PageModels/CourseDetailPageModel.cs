using GritTrack.Models;

namespace GritTrack.PageModels
{
    public class CourseDetailPageModel : BaseViewModel
    {
        private Course _course = new();
        public Course Course
        {
            get => _course;
            set => SetProperty(ref _course, value);
        }

        public RelayCommand GoBackCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand DeleteCommand { get; }

        public event EventHandler? BackRequested;
        public event EventHandler<Course>? EditRequested;
        public event EventHandler<Course>? DeleteRequested;

        public CourseDetailPageModel()
        {
            GoBackCommand = new RelayCommand(() => BackRequested?.Invoke(this, EventArgs.Empty));
            EditCommand = new RelayCommand(() => EditRequested?.Invoke(this, Course));
            DeleteCommand = new RelayCommand(() => DeleteRequested?.Invoke(this, Course));
        }

        public void Load(Course course)
        {
            Course = course;
        }
    }
}
