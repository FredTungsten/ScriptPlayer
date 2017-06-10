using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace ScriptPlayer.Shared
{
    public class LocalTimeSource : TimeSource
    {
        private Storyboard _storyboard;

        public LocalTimeSource()
        {
            InitializeStoryboard();
        }

        private void InitializeStoryboard()
        {
            TimeSpan duration = TimeSpan.FromDays(1);

            DoubleAnimation animation = new DoubleAnimation(0, duration.TotalSeconds, new Duration(duration));

            _storyboard = new Storyboard();
            _storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, this);
            Storyboard.SetTargetProperty(animation, new PropertyPath(ProgressProperty));
        }

        public void Start()
        {
            _storyboard.Begin();
        }

        public void Pause()
        {
            _storyboard.Pause();
        }

        public void Continue()
        {
            _storyboard.Resume();
        }

        public void TogglePlayback()
        {
            if (_storyboard.GetIsPaused())
                Continue();
            else
                Pause();
        }
    }
}