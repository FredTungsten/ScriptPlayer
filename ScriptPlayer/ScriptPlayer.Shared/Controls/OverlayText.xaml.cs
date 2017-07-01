using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ScriptPlayer.Shared
{
    /// <summary>
    /// Interaction logic for OverlayText.xaml
    /// </summary>
    public partial class OverlayText : UserControl
    {
        private Storyboard _animation;

        public OverlayText()
        {
            InitializeComponent();
        }

        public void SetText(string text, TimeSpan duration)
        {
            if (CheckAccess())
            {
                TextBlock.Text = text;

                _animation?.Stop();
                _animation = new Storyboard();

                DoubleAnimationUsingKeyFrames animation = new DoubleAnimationUsingKeyFrames();
                Storyboard.SetTarget(animation, grid);
                Storyboard.SetTargetProperty(animation, new PropertyPath(OpacityProperty));

                animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, TimeSpan.Zero));
                animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, duration));
                animation.KeyFrames.Add(new LinearDoubleKeyFrame(0, duration + TimeSpan.FromMilliseconds(500)));

                _animation.Children.Add(animation);
                _animation.Begin();
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() => SetText(text, duration)));
            }
        }
    }
}
