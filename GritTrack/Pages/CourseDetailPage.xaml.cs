using GritTrack.PageModels;

namespace GritTrack.Pages
{
    // CourseDetailPageModel receives the "Course" from Shell and handles
    // back, edit and delete itself — nothing left to do here but wire it in
    public partial class CourseDetailPage : ContentPage
    {
        public CourseDetailPage(CourseDetailPageModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
