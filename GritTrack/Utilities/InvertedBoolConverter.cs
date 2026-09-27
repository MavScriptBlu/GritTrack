using System.Globalization;

namespace GritTrack.Utilities
{
    /// <summary>
    /// Flips true to false and false to true. Used so the "Set starting
    /// GPA" link only shows when the entry boxes are CLOSED, using the
    /// same IsEditingStartingGpa flag the boxes use to show themselves —
    /// no need for two separate on/off switches doing the same job.
    /// </summary>
    public class InvertedBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool b && !b;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool b && !b;
        }
    }
}
