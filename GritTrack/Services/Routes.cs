namespace GritTrack.Services
{
    // every Shell route and query-string key lives here so the names are
    // only typed once. AppShell registers the pushed pages from this list
    public static class Routes
    {
        // the list is a ShellContent in AppShell.xaml (the home screen)
        public const string CourseList = "CourseListPage";

        // pushed on top of the list, registered in AppShell.xaml.cs
        public const string CourseDetail = "CourseDetailPage";
        public const string AddCourse = "AddCoursePage";
        public const string Login = "LoginPage";

        // relative routes: go back one page, or two
        public const string Back = "..";
        public const string BackTwice = "../..";
    }

    // the labels used when handing an object from one page to the next
    public static class QueryKeys
    {
        public const string Course = "Course";
        public const string EditCourse = "EditCourse";
        public const string SavedCourse = "SavedCourse";
        public const string DeletedCourse = "DeletedCourse";
    }
}
