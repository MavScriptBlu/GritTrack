using GritTrack.Models;
using GritTrack.PageModels;

namespace GritTrack.Pages
{
    // "SavedCourse" comes back from AddCoursePage (add or edit) and
    // "DeletedCourse" comes back from CourseDetailPage's Delete button —
    // either one hands the ViewModel a Course to act on without a full reload.
    //
    // this used to be two [QueryProperty] attributes, but Shell will replay
    // the last value it handed a route even on a plain back-navigation that
    // never passed one — that's what was popping the "Removed" toast every
    // time you left and came back to this page. Handling it here instead
    // and deleting the key right after we use it stops Shell from having
    // anything left to replay.
    public partial class CourseListPage : ContentPage, IQueryAttributable
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

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("SavedCourse", out var saved) && saved is Course savedCourse)
            {
                _ = _viewModel.SaveCourseAsync(savedCourse);
                query.Remove("SavedCourse");
            }

            if (query.TryGetValue("DeletedCourse", out var deleted) && deleted is Course deletedCourse)
            {
                _ = _viewModel.RemoveCourseAsync(deletedCourse);
                query.Remove("DeletedCourse");
            }
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
