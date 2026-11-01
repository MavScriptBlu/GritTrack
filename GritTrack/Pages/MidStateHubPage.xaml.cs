using GritTrack.PageModels;

namespace GritTrack.Pages
{
    // the Mid-State Hub tab. all the real work is in MidStateHubPageModel,
    // this just hooks it up and refreshes the course picker each time the tab shows
    public partial class MidStateHubPage : ContentPage
    {
        private readonly MidStateHubPageModel _viewModel;

        public MidStateHubPage(MidStateHubPageModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = _viewModel.LoadCoursesAsync();
        }
    }
}
