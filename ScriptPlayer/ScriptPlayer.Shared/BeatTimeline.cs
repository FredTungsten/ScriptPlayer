using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ScriptPlayer.Shared
{
    public class BeatTimeline : DependencyObject
    {
        public event EventHandler<BeatGroupChangedEventArgs> BeatGroupChanged;

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
            "Progress", typeof (double), typeof (BeatTimeline), new PropertyMetadata(default(double), OnProgressPropertyChanged));

        private static void OnProgressPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BeatTimeline) d).ProgressHasChanged();
        }

        private BeatGroup _currentBeatGroup;

        private void ProgressHasChanged()
        {
            BeatGroup activeGroup = FindActiveGroup(Progress);
            if (activeGroup != null)
                BeatProgress = activeGroup.GetBeatProgress(Progress);
            else
                BeatProgress = BeatPattern.BeatEnd;

            OnProgressChanged(new ProgressChangedEventArgs(Progress, BeatProgress));
            if (activeGroup != _currentBeatGroup)
            {
                _currentBeatGroup = activeGroup;
                OnBeatGroupChanged(new BeatGroupChangedEventArgs(activeGroup));
            }
        }

        public static readonly DependencyProperty TimeSourceProperty = DependencyProperty.Register(
            "TimeSource", typeof (TimeSource), typeof (BeatTimeline), new PropertyMetadata(default(TimeSource)));

        public TimeSource TimeSource
        {
            get { return (TimeSource) GetValue(TimeSourceProperty); }
            set { SetValue(TimeSourceProperty, value); }
        }

        public BeatGroup FindNextGroup(double position)
        {
            return _beatGroups.FirstOrDefault(beatGroup => beatGroup.Start >= position);
        }

        public BeatGroup FindActiveGroup(double position)
        {
            return _beatGroups.FirstOrDefault(beatGroup => beatGroup.Start <= position && beatGroup.End > position);
        }

        public double Progress
        {
            get { return (double) GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        public static readonly DependencyProperty BeatProgressProperty = DependencyProperty.Register(
            "BeatProgress", typeof (double), typeof (BeatTimeline), new PropertyMetadata(default(double)));

        public double BeatProgress
        {
            get { return (double) GetValue(BeatProgressProperty); }
            set { SetValue(BeatProgressProperty, value); }
        }

        public BeatTimeline()
        {
            _beatGroups = new List<BeatGroup>();
            GenerateBeatGroups();

            BindingOperations.SetBinding(this, ProgressProperty,
                new Binding("TimeSource.Progress") {RelativeSource = RelativeSource.Self});
        }

        private List<BeatGroup> _beatGroups;

        private void GenerateBeatGroups()
        {
            //AppendBeatGroup(BeatGroup.Pause(TimeSpan.FromSeconds(34)));
            //for(int i = 0; i < 20; i++)
            //AppendBeatGroup(new BeatGroup(BeatPattern.Normal, 150, 2));
        }

        public void AppendBeatGroup(BeatGroup beatGroup)
        {
            double start = 0;

            if (_beatGroups.Count > 0)
            {
                BeatGroup lastGroup = _beatGroups.Last();
                start = lastGroup.Start + lastGroup.Duration;
            }

            beatGroup.Start = start;

            _beatGroups.Add(beatGroup);
        }

        protected virtual void OnProgressChanged(ProgressChangedEventArgs e)
        {
            var handler = ProgressChanged;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnBeatGroupChanged(BeatGroupChangedEventArgs e)
        {
            var handler = BeatGroupChanged;
            if (handler != null) handler(this, e);
        }

        public static BeatTimeline FromTimestamps(List<double> beats)
        {
            var timeline = new BeatTimeline();

            if (beats.Count < 2) return timeline;

            timeline.AppendBeatGroup(BeatGroup.Pause(TimeSpan.FromSeconds(beats[0])));

            for (int i = 1; i < beats.Count; i++)
            {
                timeline.AppendBeatGroup(BeatGroup.SingleBeat(TimeSpan.FromSeconds(beats[i] - beats[i - 1])));
            }

            return timeline;
        }
    }

    public class BeatGroupChangedEventArgs : EventArgs
    {
        public BeatGroup BeatGroup;

        public BeatGroupChangedEventArgs(BeatGroup beatGroup)
        {
            BeatGroup = beatGroup;
        }
    }

    public class ProgressChangedEventArgs : EventArgs
    {
        public double Progress;
        public double BeatProgress;

        public ProgressChangedEventArgs(double progress, double beatProgress)
        {
            Progress = progress;
            BeatProgress = beatProgress;
        }
    }
}
