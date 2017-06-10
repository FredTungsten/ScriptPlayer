using System;
using System.Windows;

namespace ScriptPlayer.Shared
{
    public interface ISampleClock
    {
        event EventHandler Tick;
    }

    public abstract class TimeSource : DependencyObject
    {
        public event EventHandler<TimeSpan> ProgressChanged; 

        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
            "Progress", typeof (TimeSpan), typeof (TimeSource), new PropertyMetadata(default(TimeSpan), OnProgressChangedCallback));

        private static void OnProgressChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TimeSource)d).OnProgressChanged((TimeSpan) e.NewValue);
        }

        public TimeSpan Progress
        {
            get { return (TimeSpan) GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        protected virtual void OnProgressChanged(TimeSpan e)
        {
            ProgressChanged?.Invoke(this, e);
        }
    }
}
