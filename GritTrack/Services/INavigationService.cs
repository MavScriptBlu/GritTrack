namespace GritTrack.Services
{
    // lets a PageModel move between pages and show a message without
    // knowing anything about Shell or the Pages themselves. the PageModel
    // just asks, ShellNavigationService does the actual Shell work
    public interface INavigationService
    {
        // goes to a route (see Routes), optionally handing data along
        Task GoToAsync(string route, IDictionary<string, object>? parameters = null);

        // quick message with an OK button
        Task ShowAlertAsync(string title, string message);
    }
}
