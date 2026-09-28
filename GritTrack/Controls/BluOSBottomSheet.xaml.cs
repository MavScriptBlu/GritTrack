using System.Windows.Input;

namespace GritTrack.Controls
{
    // one shared popup card, used everywhere instead of copy-pasting it
    public partial class BluOSBottomSheet : ContentView
    {
        public BluOSBottomSheet()
        {
            InitializeComponent();
        }

        // true = card is showing
        public static readonly BindableProperty IsOpenProperty =
            BindableProperty.Create(
                nameof(IsOpen),
                typeof(bool),
                typeof(BluOSBottomSheet),
                false,
                propertyChanged: OnIsOpenChanged);

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        private static void OnIsOpenChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is BluOSBottomSheet sheet)
                sheet.RootGrid.IsVisible = (bool)newValue;
        }

        // big text at the top of the card
        public static readonly BindableProperty SheetTitleProperty =
            BindableProperty.Create(
                nameof(SheetTitle),
                typeof(string),
                typeof(BluOSBottomSheet),
                string.Empty,
                propertyChanged: OnSheetTitleChanged);

        public string SheetTitle
        {
            get => (string)GetValue(SheetTitleProperty);
            set => SetValue(SheetTitleProperty, value);
        }

        private static void OnSheetTitleChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is BluOSBottomSheet sheet)
                sheet.TitleLabel.Text = (string)newValue;
        }

        // whatever the calling page wants shown inside the card
        public static readonly BindableProperty SheetContentProperty =
            BindableProperty.Create(
                nameof(SheetContent),
                typeof(View),
                typeof(BluOSBottomSheet),
                null,
                propertyChanged: OnSheetContentChanged);

        public View SheetContent
        {
            get => (View)GetValue(SheetContentProperty);
            set => SetValue(SheetContentProperty, value);
        }

        private static void OnSheetContentChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is BluOSBottomSheet sheet)
                sheet.ContentHost.Content = (View)newValue;
        }

        // runs when the dimmed backdrop is tapped, same as tapping Cancel
        public static readonly BindableProperty CloseCommandProperty =
            BindableProperty.Create(
                nameof(CloseCommand),
                typeof(ICommand),
                typeof(BluOSBottomSheet));

        public ICommand CloseCommand
        {
            get => (ICommand)GetValue(CloseCommandProperty);
            set => SetValue(CloseCommandProperty, value);
        }

        private void OnBackdropTapped(object? sender, TappedEventArgs e)
        {
            if (CloseCommand?.CanExecute(null) == true)
                CloseCommand.Execute(null);
        }
    }
}
