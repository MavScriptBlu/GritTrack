using GritTrack.Models;
using GritTrack.PageModels;

namespace GritTrack.Pages
{
    // when navigation sends something labeled "Course",
    // put it in this page's Course property below.
    [QueryProperty(nameof(Course), "Course")]
    public partial class CourseDetailPage : ContentPage
    {
        private readonly CourseDetailPageModel _viewModel;

        public CourseDetailPage(CourseDetailPageModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            _viewModel.BackRequested += OnBackRequested;
            _viewModel.EditRequested += OnEditRequested;
            _viewModel.DeleteRequested += OnDeleteRequested;
        }

        public Course Course
        {
            set => _viewModel.Load(value);
        }

        private async void OnBackRequested(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private async void OnEditRequested(object? sender, Course course)
        {
            var navigationParameter = new Dictionary<string, object> { { "EditCourse", course } };
            await Shell.Current.GoToAsync(nameof(AddCoursePage), navigationParameter);
        }

        private async void OnDeleteRequested(object? sender, Course course)
        {
            // the styled confirm card already got a "yes" before this fires —
            // no second, unstyled native popup needed here
            var navigationParameter = new Dictionary<string, object> { { "DeletedCourse", course } };
            await Shell.Current.GoToAsync("../..", navigationParameter);
        }
    }
}
