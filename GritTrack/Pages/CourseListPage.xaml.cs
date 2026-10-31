using GritTrack.PageModels;

namespace GritTrack.Pages
{
    // all the loading, navigating, saving and deleting lives in CourseListPageModel.
    // this page only wires the PageModel in
    public partial class CourseListPage : ContentPage
    {
        public CourseListPage(CourseListPageModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
