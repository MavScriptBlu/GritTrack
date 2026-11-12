using GritTrack.PageModels;

namespace GritTrack.Pages
{
    // all the signing in and navigating lives in LoginPageModel.
    // this page only wires the PageModel in
    public partial class LoginPage : ContentPage
    {
        public LoginPage(LoginPageModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
