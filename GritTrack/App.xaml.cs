using Microsoft.Extensions.DependencyInjection;

namespace GritTrack
{
    public partial class App : Application
    {
        private readonly AppShell _shell;

        // AppShell comes from dependency injection now because it needs its
        // own PageModel (the one that supplies the flyout title)
        public App(AppShell shell)
        {
            InitializeComponent();
            _shell = shell;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(_shell);
        }
    }
}
