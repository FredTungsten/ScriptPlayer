using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using JetBrains.Annotations;

namespace ScriptPlayer.Shared
{
    public abstract class TimeSource : DependencyObject, INotifyPropertyChanged
    {
        public event EventHandler<TimeSpan> ProgressChanged;
        public event EventHandler<TimeSpan> DurationChanged;
        public event EventHandler<double> PlaybackRateChanged; 
        public event EventHandler<bool> IsPlayingChanged;

        private static readonly DependencyPropertyKey IsConnectedPropertyKey = DependencyProperty.RegisterReadOnly(
            "IsConnected", typeof(bool), typeof(TimeSource), new PropertyMetadata(default(bool)));

        public DependencyProperty IsConnectedProperty = IsConnectedPropertyKey.DependencyProperty;

        public bool IsConnected
        {
            get => (bool) GetValue(IsConnectedProperty);
            protected set => SetValue(IsConnectedPropertyKey, value);
        }

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
            Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: TimeSource.IsPlaying Changed! Old Value: {e.OldValue} NewValue: {e.NewValue}");
            ((TimeSource) d).OnIsPlayingChanged((bool) e.NewValue);
        }

        public DependencyProperty IsPlayingProperty = IsPlayingPropertyKey.DependencyProperty;

        public bool IsPlaying
        {
            get => (bool) GetValue(IsPlayingProperty);
            protected set => SetValue(IsPlayingPropertyKey, value);
        }

        public abstract double PlaybackRate { get; set; }

        public abstract bool CanPlayPause { get; }
        public abstract bool CanSeek { get; }
        public abstract bool CanOpenMedia { get; }

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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPlaybackRateChanged(double rate)
        {
            PlaybackRateChanged?.Invoke(this, rate);
        }
    }
}