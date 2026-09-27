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

        // true while the "are you sure you want to drop this?" card is
        // showing on screen. this used to be a native popup (DisplayAlert)
        // but those can't be styled to match the app, so now it's just a
        // card built right into this page that shows/hides with this flag
        private bool _isConfirmingDelete;
        public bool IsConfirmingDelete
        {
            get => _isConfirmingDelete;
            set => SetProperty(ref _isConfirmingDelete, value);
        }

        public RelayCommand GoBackCommand { get; }
        public RelayCommand EditCommand { get; }

        // tapping "Drop This Course" no longer deletes right away — it
        // just opens the confirm card. the actual delete only happens if
        // they tap "Drop It" on that card (ConfirmDeleteCommand)
        public RelayCommand DeleteCommand { get; }
        public RelayCommand ConfirmDeleteCommand { get; }
        public RelayCommand CancelDeleteCommand { get; }

        public event EventHandler? BackRequested;
        public event EventHandler<Course>? EditRequested;

        // only fires once the user actually confirms on the card
        public event EventHandler<Course>? DeleteRequested;

        public CourseDetailPageModel()
        {
            GoBackCommand = new RelayCommand(() => BackRequested?.Invoke(this, EventArgs.Empty));
            EditCommand = new RelayCommand(() => EditRequested?.Invoke(this, Course));
            DeleteCommand = new RelayCommand(() => IsConfirmingDelete = true);
            ConfirmDeleteCommand = new RelayCommand(() =>
            {
                IsConfirmingDelete = false;
                DeleteRequested?.Invoke(this, Course);
            });
            CancelDeleteCommand = new RelayCommand(() => IsConfirmingDelete = false);
        }

        public void Load(Course course)
        {
            Course = course;
        }
    }
}
