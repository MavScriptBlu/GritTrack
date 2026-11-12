using GritTrack.Services.Api;

namespace GritTrack.PageModels
{
    // feeds the Shell, so the flyout title isn't typed into XAML.
    // shows who is signed in once someone has logged in
    public class AppShellViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;

        public AppShellViewModel(IAuthService authService)
        {
            _authService = authService;
            _authService.SignInChanged += (_, _) => OnPropertyChanged(nameof(FlyoutTitle));

            // pick up a login saved from last time; the title updates when it lands
            _ = _authService.RestoreAsync();
        }

        public string FlyoutTitle => _authService.IsSignedIn
            ? $"Grit Track ({_authService.UserName})"
            : "Grit Track";
    }
}
