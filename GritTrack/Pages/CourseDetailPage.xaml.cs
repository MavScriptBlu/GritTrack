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
        }

        public Course Course
        {
            set => _viewModel.Load(value);
        }

        private async void OnBackRequested(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}