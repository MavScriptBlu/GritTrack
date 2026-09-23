using GritTrack.Models;
using GritTrack.PageModels;

namespace GritTrack.Pages
{
    // "SavedCourse" comes back from AddCoursePage (add or edit) and
    // "DeletedCourse" comes back from CourseDetailPage's Delete button —
    // either one hands the ViewModel a Course to act on without a full reload
    [QueryProperty(nameof(SavedCourse), "SavedCourse")]
    [QueryProperty(nameof(DeletedCourse), "DeletedCourse")]
    public partial class CourseListPage : ContentPage
    {
        private readonly CourseListPageModel _viewModel;

        public CourseListPage(CourseListPageModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            _viewModel.SelectedCourseRequested += OnSelectedCourseRequested;
            _viewModel.AddCourseRequested += OnAddCourseRequested;
            _viewModel.CourseDeleted += OnCourseDeleted;
        }

        public Course SavedCourse
        {
            // property setters can't be async, so this is intentionally
            // fire-and-forget — same pattern as the commands in the ViewModel
            set => _ = _viewModel.SaveCourseAsync(value);
        }

        public Course DeletedCourse
        {
            set => _ = _viewModel.RemoveCourseAsync(value);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.LoadCoursesCommand.Execute(null);
        }

        private async void OnSelectedCourseRequested(object? sender, Course course)
        {
            var navigationParameter = new Dictionary<string, object> { { "Course", course } };
            await Shell.Current.GoToAsync(nameof(CourseDetailPage), navigationParameter);
        }

        private async void OnAddCourseRequested(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(AddCoursePage));
        }

        // reacting to the ViewModel's custom CourseDeleted event — pure UI
        // feedback, so it belongs here, not in the ViewModel. No
        // CommunityToolkit.Maui installed in this project, so a quick
        // auto-dismissing alert stands in for a real toast/snackbar.
        private async void OnCourseDeleted(object? sender, Course course)
        {
            await DisplayAlertAsync("Removed", $"{course.CourseCode} was removed.", "OK");
        }
    }
}
