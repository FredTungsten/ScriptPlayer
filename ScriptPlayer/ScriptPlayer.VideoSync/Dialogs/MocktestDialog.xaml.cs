using System;
using System.Windows;
using System.Windows.Media.Animation;
using ScriptPlayer.Shared;

namespace ScriptPlayer.VideoSync.Dialogs
{
    /// <summary>
    /// Interaction logic for MocktestDialog.xaml
    /// </summary>
    public partial class MocktestDialog : Window
    {
        public MocktestDialog()
        {
            Loaded += OnLoaded;
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            DoubleAnimationUsingKeyFrames anim = new DoubleAnimationUsingKeyFrames();
            anim.RepeatBehavior = RepeatBehavior.Forever;
            Storyboard.SetTarget(anim, launchSimulator);
            Storyboard.SetTargetProperty(anim, new PropertyPath(LaunchSimulator.PositionProperty));

            anim.Duration = new Duration(TimeSpan.FromSeconds(5));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0.0)));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(99, KeyTime.FromPercent(0.10)));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0.20)));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(99, KeyTime.FromPercent(0.30)));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0.50)));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(99, KeyTime.FromPercent(0.70)));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(1.00)));

            Storyboard s = new Storyboard();
            s.Children.Add(anim);
            s.Begin();
        }
    }
}
