using GritTrack.PageModels;
using GritTrack.Services;

namespace GritTrack
{
    public partial class AppShell : Shell
    {
        public AppShell(AppShellViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;

            // every page that gets pushed on top of the list needs a route.
            // names come from Routes so they're only typed in one place.
            // CourseListPage is a ShellContent in AppShell.xaml, so Shell
            // already knows that one
            Routing.RegisterRoute(Routes.CourseDetail, typeof(Pages.CourseDetailPage));
            Routing.RegisterRoute(Routes.AddCourse, typeof(Pages.AddCoursePage));
            Routing.RegisterRoute(Routes.Login, typeof(Pages.LoginPage));
        }
    }
}
