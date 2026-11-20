using GritTrack.Services;
using GritTrack.Services.Api;

namespace GritTrack.PageModels
{
    // the sign-in screen. reached when a write needs a login (the app sends you
    // here, and your save or delete carries on after you sign in) or from
    // the "Sign in" button in the top bar
    public class LoginPageModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigation;

        public LoginPageModel(IAuthService authService, INavigationService navigation)
        {
            _authService = authService;
            _navigation = navigation;

            // Sign In and Try Again run the same code
            SignInCommand = new RelayCommand(() => _ = SignInAsync());
            CancelCommand = new RelayCommand(() => _ = CancelAsync());
        }

        // typing in a box wipes that box's error, same as the course form
        private string _userName = string.Empty;
        public string UserName
        {
            get => _userName;
            set
            {
                if (SetProperty(ref _userName, value))
                    UserNameError = string.Empty;
            }
        }

        private string _userNameError = string.Empty;
        public string UserNameError
        {
            get => _userNameError;
            set => SetProperty(ref _userNameError, value);
        }

        // the password lives only in this property while the page is open.
        // it is never saved and never logged
        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                    PasswordError = string.Empty;
            }
        }

        private string _passwordError = string.Empty;
        public string PasswordError
        {
            get => _passwordError;
            set => SetProperty(ref _passwordError, value);
        }

        // true while the login request is on its way: shows the spinner
        // and turns the buttons off
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { SetProperty(ref _isBusy, value); OnPropertyChanged(nameof(IsNotBusy)); }
        }

        public bool IsNotBusy => !IsBusy;

        // what went wrong, in plain words (wrong password, server not running...)
        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public RelayCommand SignInCommand { get; }
        public RelayCommand CancelCommand { get; }

        private async Task SignInAsync()
        {
            if (IsBusy)
                return;

            // flag every empty box at once so you see exactly what's missing
            UserNameError = string.IsNullOrWhiteSpace(UserName) ? "User name is required." : string.Empty;
            PasswordError = string.IsNullOrEmpty(Password) ? "Password is required." : string.Empty;

            if (UserNameError.Length > 0 || PasswordError.Length > 0)
            {
                ErrorMessage = string.Empty;
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                await _authService.SignInAsync(UserName.Trim(), Password);
            }
            catch (ApiRequestException exception)
            {
                // covers a wrong password (ApiValidationException) and a server
                // that can't be reached: both show their message here
                ErrorMessage = exception.Message;
                IsBusy = false;
                return;
            }

            Password = string.Empty;
            IsBusy = false;

            // close this page first, then let the waiting save or delete carry on,
            // so the two don't try to navigate at the same moment
            await _navigation.GoToAsync(Routes.Back);
            _authService.CompleteLogin(true);
        }

        private async Task CancelAsync()
        {
            Password = string.Empty;
            await _navigation.GoToAsync(Routes.Back);
            _authService.CompleteLogin(false);
        }
    }
}
