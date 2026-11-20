using System.Windows.Input;

namespace GritTrack.Controls
{
    // the shared big error card. Title + Message always, plus an action button
    // (like Try Again) only when the page gives it ActionText and ActionCommand
    public partial class ErrorBanner : ContentView
    {
        public ErrorBanner()
        {
            InitializeComponent();
        }

        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create(
                nameof(Title),
                typeof(string),
                typeof(ErrorBanner),
                string.Empty,
                propertyChanged: OnTitleChanged);

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        private static void OnTitleChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ErrorBanner banner)
                banner.TitleLabel.Text = (string)newValue;
        }

        public static readonly BindableProperty MessageProperty =
            BindableProperty.Create(
                nameof(Message),
                typeof(string),
                typeof(ErrorBanner),
                string.Empty,
                propertyChanged: OnMessageChanged);

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        private static void OnMessageChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ErrorBanner banner)
            {
                banner.MessageLabel.Text = (string)newValue;
                SemanticProperties.SetDescription(banner.MessageLabel, $"{banner.Title}. {newValue}");
            }
        }

        public static readonly BindableProperty ActionTextProperty =
            BindableProperty.Create(
                nameof(ActionText),
                typeof(string),
                typeof(ErrorBanner),
                string.Empty,
                propertyChanged: OnActionChanged);

        public string ActionText
        {
            get => (string)GetValue(ActionTextProperty);
            set => SetValue(ActionTextProperty, value);
        }

        public static readonly BindableProperty ActionCommandProperty =
            BindableProperty.Create(
                nameof(ActionCommand),
                typeof(ICommand),
                typeof(ErrorBanner),
                null,
                propertyChanged: OnActionChanged);

        public ICommand? ActionCommand
        {
            get => (ICommand?)GetValue(ActionCommandProperty);
            set => SetValue(ActionCommandProperty, value);
        }

        // false while a retry is already running, so you can't double tap it
        public static readonly BindableProperty IsActionEnabledProperty =
            BindableProperty.Create(
                nameof(IsActionEnabled),
                typeof(bool),
                typeof(ErrorBanner),
                true,
                propertyChanged: OnActionChanged);

        public bool IsActionEnabled
        {
            get => (bool)GetValue(IsActionEnabledProperty);
            set => SetValue(IsActionEnabledProperty, value);
        }

        // text, command and enabled all land here, the button just follows them
        private static void OnActionChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not ErrorBanner banner)
                return;

            banner.ActionButton.Text = banner.ActionText;
            banner.ActionButton.Command = banner.ActionCommand;
            banner.ActionButton.IsEnabled = banner.IsActionEnabled;
            banner.ActionButton.IsVisible = !string.IsNullOrEmpty(banner.ActionText) && banner.ActionCommand is not null;
        }
    }
}
