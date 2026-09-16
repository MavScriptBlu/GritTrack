using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GritTrack.PageModels
{
    /// <summary>
    /// Hand-rolled MVVM base class — no source-gen shortcuts.
    /// Every PageModel inherits this so the View can bind to properties
    /// and get notified when they change.
    /// </summary>
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Sets the backing field and raises PropertyChanged ONLY if the
        /// value actually changed. This is the pattern from the "What is
        /// SetProperty" lesson — ref lets us write directly into the
        /// caller's backing field instead of returning a copy.
        /// </summary>
        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
