using GritTrack.Models;
using GritTrack.PageModels;

namespace GritTrack.Pages
{

    public partial class CourseListPage : ContentPage
    {
        private readonly CourseListPageModel _viewModel;

        public CourseListPage(CourseListPageModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            _viewModel.SelectedCourseRequested += OnSelectedCourseRequested;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.LoadCoursesCommand.Execute(null);
        }

        // "Course" here has to match the QueryProperty name on
        // CourseDetailPage or the value just won't show up over there
        private async void OnSelectedCourseRequested(object? sender, Course course)
        {
            var navigationParameter = new Dictionary<string, object> { { "Course", course } };
            await Shell.Current.GoToAsync(nameof(CourseDetailPage), navigationParameter);
        }
    }
}