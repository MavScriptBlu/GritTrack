using GritTrack.Services;
using System.Globalization;

namespace GritTrack.Utilities
{
    /// <summary>
    /// The one required IValueConverter: takes a GpaStatus enum from the
    /// ViewModel and turns it into a Color the View can use for a status
    /// card — the View never needs to know what "Safe/Watch/AtRisk" means.
    /// </summary>
    public class GpaStatusToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not GpaStatus status)
                return Colors.Gray;

            return status switch
            {
                GpaStatus.Safe => Color.FromArgb("#9fe0bd"),   // Mint
                GpaStatus.Watch => Color.FromArgb("#f2e3a6"),  // Butter
                GpaStatus.AtRisk => Color.FromArgb("#f6adbf"), // Pink
                _ => Color.FromArgb("#232323")                 // ink-500 hairline
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException("One-way converter — status is derived, never set from the UI.");
        }
    }
}
