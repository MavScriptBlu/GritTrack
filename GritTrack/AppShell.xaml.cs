namespace GritTrack
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(Pages.CourseDetailPage), typeof(Pages.CourseDetailPage));
            Routing.RegisterRoute(nameof(Pages.AddCoursePage), typeof(Pages.AddCoursePage));
        }
    }
}
