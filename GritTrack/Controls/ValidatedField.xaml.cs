using System.Windows.Input;

namespace GritTrack.Controls
{
    // label + text box + error line, all in one. the page hands it a LabelText,
    // a Text to bind to and an ErrorText. empty ErrorText = happy box,
    // anything in it = pink outline and the message underneath
    public partial class ValidatedField : ContentView
    {
        private static readonly Color ErrorColor = Color.FromArgb("#f6adbf");
        private static readonly Color ErrorTint = Color.FromArgb("#1f1014");
        private static readonly Color RestingColor = Color.FromArgb("#333333");
        private static readonly Color FallbackFocusColor = Color.FromArgb("#9fe0bd");

        private bool _isFocused;

        public ValidatedField()
        {
            InitializeComponent();
        }

        // the small label above the box
        public static readonly BindableProperty LabelTextProperty =
            BindableProperty.Create(
                nameof(LabelText),
                typeof(string),
                typeof(ValidatedField),
                string.Empty,
                propertyChanged: OnLabelTextChanged);

        public string LabelText
        {
            get => (string)GetValue(LabelTextProperty);
            set => SetValue(LabelTextProperty, value);
        }

        private static void OnLabelTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ValidatedField field)
            {
                field.FieldLabel.Text = (string)newValue;
                field.RefreshLook();
            }
        }

        // what the user typed. two way, so the PageModel property stays in sync
        public static readonly BindableProperty TextProperty =
            BindableProperty.Create(
                nameof(Text),
                typeof(string),
                typeof(ValidatedField),
                string.Empty,
                BindingMode.TwoWay,
                propertyChanged: OnTextChanged);

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ValidatedField field)
            {
                string newText = (string?)newValue ?? string.Empty;
                if ((field.FieldEntry.Text ?? string.Empty) != newText)
                    field.FieldEntry.Text = newText;
            }
        }

        private void OnEntryTextChanged(object? sender, TextChangedEventArgs e)
        {
            Text = e.NewTextValue ?? string.Empty;
        }

        public static readonly BindableProperty PlaceholderProperty =
            BindableProperty.Create(
                nameof(Placeholder),
                typeof(string),
                typeof(ValidatedField),
                string.Empty,
                propertyChanged: OnPlaceholderChanged);

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        private static void OnPlaceholderChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ValidatedField field)
                field.FieldEntry.Placeholder = (string)newValue;
        }

        // what's wrong with this box. empty means nothing is wrong
        public static readonly BindableProperty ErrorTextProperty =
            BindableProperty.Create(
                nameof(ErrorText),
                typeof(string),
                typeof(ValidatedField),
                string.Empty,
                propertyChanged: OnErrorTextChanged);

        public string ErrorText
        {
            get => (string)GetValue(ErrorTextProperty);
            set => SetValue(ErrorTextProperty, value);
        }

        private static void OnErrorTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ValidatedField field)
                field.RefreshLook();
        }

        public static readonly BindableProperty KeyboardProperty =
            BindableProperty.Create(
                nameof(Keyboard),
                typeof(Keyboard),
                typeof(ValidatedField),
                Keyboard.Default,
                propertyChanged: OnKeyboardChanged);

        public Keyboard Keyboard
        {
            get => (Keyboard)GetValue(KeyboardProperty);
            set => SetValue(KeyboardProperty, value);
        }

        private static void OnKeyboardChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ValidatedField field)
                field.FieldEntry.Keyboard = (Keyboard?)newValue ?? Keyboard.Default;
        }

        public static readonly BindableProperty IsPasswordProperty =
            BindableProperty.Create(
                nameof(IsPassword),
                typeof(bool),
                typeof(ValidatedField),
                false,
                propertyChanged: OnIsPasswordChanged);

        public bool IsPassword
        {
            get => (bool)GetValue(IsPasswordProperty);
            set => SetValue(IsPasswordProperty, value);
        }

        private static void OnIsPasswordChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ValidatedField field)
                field.FieldEntry.IsPassword = (bool)newValue;
        }

        public static readonly BindableProperty ReturnTypeProperty =
            BindableProperty.Create(
                nameof(ReturnType),
                typeof(ReturnType),
                typeof(ValidatedField),
                ReturnType.Default,
                propertyChanged: OnReturnTypeChanged);

        public ReturnType ReturnType
        {
            get => (ReturnType)GetValue(ReturnTypeProperty);
            set => SetValue(ReturnTypeProperty, value);
        }

        private static void OnReturnTypeChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ValidatedField field)
                field.FieldEntry.ReturnType = (ReturnType)newValue;
        }

        // runs when the user hits Enter / Done on the keyboard
        public static readonly BindableProperty ReturnCommandProperty =
            BindableProperty.Create(
                nameof(ReturnCommand),
                typeof(ICommand),
                typeof(ValidatedField),
                null,
                propertyChanged: OnReturnCommandChanged);

        public ICommand? ReturnCommand
        {
            get => (ICommand?)GetValue(ReturnCommandProperty);
            set => SetValue(ReturnCommandProperty, value);
        }

        private static void OnReturnCommandChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ValidatedField field)
                field.FieldEntry.ReturnCommand = newValue as ICommand;
        }

        private void OnEntryFocused(object? sender, FocusEventArgs e)
        {
            _isFocused = true;
            RefreshLook();
        }

        private void OnEntryUnfocused(object? sender, FocusEventArgs e)
        {
            _isFocused = false;
            RefreshLook();
        }

        // one place that decides how the box looks:
        // error = thick pink + tint, focused = thick accent, otherwise quiet gray
        private void RefreshLook()
        {
            bool hasError = !string.IsNullOrEmpty(ErrorText);

            ErrorLabel.Text = ErrorText ?? string.Empty;
            ErrorLabel.IsVisible = hasError;

            if (hasError)
            {
                FieldBorder.Stroke = new SolidColorBrush(ErrorColor);
                FieldBorder.StrokeThickness = 2;
                FieldBorder.BackgroundColor = ErrorTint;
            }
            else if (_isFocused)
            {
                FieldBorder.Stroke = new SolidColorBrush(GetFocusColor());
                FieldBorder.StrokeThickness = 2;
                FieldBorder.BackgroundColor = Colors.Transparent;
            }
            else
            {
                FieldBorder.Stroke = new SolidColorBrush(RestingColor);
                FieldBorder.StrokeThickness = 1;
                FieldBorder.BackgroundColor = Colors.Transparent;
            }

            // screen readers get the problem along with the field name
            SemanticProperties.SetDescription(
                FieldEntry,
                hasError ? $"{LabelText}. {ErrorText}" : LabelText);
        }

        // AccentColor is rewritten by ThemeService as the GPA status changes,
        // so read it fresh each time instead of caching it
        private static Color GetFocusColor()
        {
            if (Application.Current?.Resources.TryGetValue("AccentColor", out object? accent) == true && accent is Color accentColor)
                return accentColor;

            return FallbackFocusColor;
        }
    }
}
