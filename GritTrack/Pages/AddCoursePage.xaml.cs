using GritTrack.PageModels;

namespace GritTrack.Pages
{
    // Add and Edit share this page. AddCoursePageModel receives "EditCourse"
    // from Shell on its own, saves, and navigates back itself —
    // nothing left to do here but wire it in
    public partial class AddCoursePage : ContentPage
    {
        public AddCoursePage(AddCoursePageModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
