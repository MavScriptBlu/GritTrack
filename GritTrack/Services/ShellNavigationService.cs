namespace GritTrack.Services
{
    // the only class in the app that talks to Shell.Current. everything
    // else goes through INavigationService, so PageModels stay free of UI
    public class ShellNavigationService : INavigationService
    {
        public Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
        {
            return parameters is null
                ? Shell.Current.GoToAsync(route)
                : Shell.Current.GoToAsync(route, parameters);
        }

        public Task ShowAlertAsync(string title, string message)
        {
            // no CommunityToolkit.Maui in this project, so a plain alert
            // stands in for a toast
            return Shell.Current.DisplayAlertAsync(title, message, "OK");
        }
    }
}
