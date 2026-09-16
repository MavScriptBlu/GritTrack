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
        public event EventHandler? BackRequested;

        public CourseDetailPageModel()
        {
            GoBackCommand = new RelayCommand(() => BackRequested?.Invoke(this, EventArgs.Empty));
        }

        public void Load(Course course)
        {
            Course = course;
        }
    }
}
