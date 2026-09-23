using GritTrack.Models;
using GritTrack.PageModels;

namespace GritTrack.Pages
{
    // "EditCourse" arriving here means edit mode — the ViewModel pre-fills
    // its fields and Save() updates that course instead of inserting a new one
    [QueryProperty(nameof(EditCourse), "EditCourse")]
    public partial class AddCoursePage : ContentPage
    {
        private readonly AddCoursePageModel _viewModel;

        public AddCoursePage(AddCoursePageModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            _viewModel.CourseSavedRequested += OnCourseSavedRequested;
            _viewModel.BackRequested += OnBackRequested;
        }

        public Course EditCourse
        {
            set => _viewModel.Load(value);
        }

        // "SavedCourse" here has to match the QueryProperty name on
        // CourseListPage or the value just won't show up over there.
        // Adding got here from List (one level up); editing got here from
        // Detail (two levels up: List -> Detail -> here), so pop back to
        // List directly instead of landing on the now-stale Detail page.
        private async void OnCourseSavedRequested(object? sender, Course course)
        {
            var navigationParameter = new Dictionary<string, object> { { "SavedCourse", course } };
            var route = _viewModel.IsEditing ? "../.." : "..";
            await Shell.Current.GoToAsync(route, navigationParameter);
        }

        private async void OnBackRequested(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
