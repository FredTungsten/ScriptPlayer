using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ScriptPlayer.Shared
{
    public class VanishingContainer : ContentControl
    {
        private readonly ScaleTransform _scale;
        private DoubleAnimationUsingKeyFrames _animation;
        public event EventHandler Gone;

        public VanishingContainer()
        {
            _scale = new ScaleTransform(1,1);
            LayoutTransform = _scale;
        }

        public void Vanish(TimeSpan duration)
        {
            if (_animation != null)
            {
                _animation.Completed -= StoryboardOnCompleted;
            }

            var totalDuration = duration + TimeSpan.FromMilliseconds(250);

            _animation = new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(totalDuration),
                RepeatBehavior = new RepeatBehavior(1),
                FillBehavior = FillBehavior.Stop
            };

            _animation.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            _animation.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(duration)));
            _animation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(totalDuration)));

            _animation.Completed += StoryboardOnCompleted;

           _scale.BeginAnimation(ScaleTransform.ScaleYProperty, _animation);
            BeginAnimation(OpacityProperty, _animation);
        }

        private void StoryboardOnCompleted(object sender, EventArgs eventArgs)
        {
            OnGone();
        }

        protected virtual void OnGone()
        {
            Gone?.Invoke(this, EventArgs.Empty);
        }
    }
}
