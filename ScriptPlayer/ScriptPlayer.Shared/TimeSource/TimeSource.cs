using System;
using System.Windows;

namespace ScriptPlayer.Shared
{
    public abstract class TimeSource : DependencyObject
    {
        public event EventHandler<TimeSpan> ProgressChanged;
        public event EventHandler<TimeSpan> DurationChanged;
        public event EventHandler<bool> IsPlayingChanged; 

        private static readonly DependencyPropertyKey ProgressPropertyKey = DependencyProperty.RegisterReadOnly(
            "Progress", typeof (TimeSpan), typeof (TimeSource), new PropertyMetadata(default(TimeSpan), OnProgressChangedCallback));

        public DependencyProperty ProgressProperty = ProgressPropertyKey.DependencyProperty;

        private static void OnProgressChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TimeSource)d).OnProgressChanged((TimeSpan) e.NewValue);
        }

        public TimeSpan Progress
        {
            get => (TimeSpan) GetValue(ProgressProperty);
            protected set => SetValue(ProgressPropertyKey, value);
        }

        private static readonly DependencyPropertyKey DurationPropertyKey = DependencyProperty.RegisterReadOnly(
            "Duration", typeof(TimeSpan), typeof(TimeSource), new PropertyMetadata(default(TimeSpan), OnDurationChangedCollback));

        private static void OnDurationChangedCollback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TimeSource)d).OnDurationChanged((TimeSpan)e.NewValue);
        }

        public DependencyProperty DurationProperty = DurationPropertyKey.DependencyProperty;

        public TimeSpan Duration
        {
            get => (TimeSpan) GetValue(DurationProperty);
            protected set => SetValue(DurationPropertyKey, value);
        }

        protected virtual void OnProgressChanged(TimeSpan e)
        {
            ProgressChanged?.Invoke(this, e);
        }

        private static readonly DependencyPropertyKey IsPlayingPropertyKey = DependencyProperty.RegisterReadOnly(
            "IsPlaying", typeof(bool), typeof(TimeSource), new PropertyMetadata(default(bool), OnIsPlayingPropertyChanged));

        private static void OnIsPlayingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TimeSource) d).OnIsPlayingChanged((bool) e.NewValue);
        }

        public DependencyProperty IsPlayingProperty = IsPlayingPropertyKey.DependencyProperty;

        public bool IsPlaying
        {
            get => (bool) GetValue(IsPlayingProperty);
            protected set => SetValue(IsPlayingPropertyKey, value);
        }

        public abstract void Play();
        public abstract void Pause();
        public virtual void TogglePlayback()
        { 
            if (IsPlaying)
                Pause();
            else
                Play();
        }
        public abstract void SetPosition(TimeSpan position);

        protected virtual void OnDurationChanged(TimeSpan e)
        {
            DurationChanged?.Invoke(this, e);
        }

        protected virtual void OnIsPlayingChanged(bool e)
        {
            IsPlayingChanged?.Invoke(this, e);
        }
    }
}