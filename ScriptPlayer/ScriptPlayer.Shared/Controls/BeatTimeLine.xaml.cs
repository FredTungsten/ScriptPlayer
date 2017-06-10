using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace ScriptPlayer.Shared
{
    /// <summary>
    /// Interaction logic for BeatTimeLine.xaml
    /// </summary>
    public partial class BeatTimeLine : UserControl
    {
        public static readonly DependencyProperty ViewPortProperty = DependencyProperty.Register(
            "ViewPort", typeof(TimeSpan), typeof(BeatTimeLine), new PropertyMetadata(default(TimeSpan)));

        public TimeSpan ViewPort
        {
            get { return (TimeSpan)GetValue(ViewPortProperty); }
            set { SetValue(ViewPortProperty, value); }
        }

        public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(
            "Offset", typeof(TimeSpan), typeof(BeatTimeLine), new PropertyMetadata(default(TimeSpan)));

        public TimeSpan Offset
        {
            get { return (TimeSpan)GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }

        public static readonly DependencyProperty SegmentsProperty = DependencyProperty.Register(
            "Segments", typeof(IEnumerable<BeatSegment>), typeof(BeatTimeLine), new PropertyMetadata(default(IEnumerable<BeatSegment>), OnBeatSegmentsPropertyChanged));

        private static void OnBeatSegmentsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BeatTimeLine)d).UpdateSegments(e.OldValue as IEnumerable<BeatSegment>,
                e.NewValue as IEnumerable<BeatSegment>);
        }

        public IEnumerable<BeatSegment> Segments
        {
            get { return (IEnumerable<BeatSegment>)GetValue(SegmentsProperty); }
            set { SetValue(SegmentsProperty, value); }
        }

        private readonly Dictionary<BeatSegment, BeatContainer> _containerDictionary = new Dictionary<BeatSegment, BeatContainer>();

        private void UpdateSegments(IEnumerable<BeatSegment> oldValue, IEnumerable<BeatSegment> newValue)
        {
            if (oldValue is INotifyCollectionChanged)
            {
                ((INotifyCollectionChanged)oldValue).CollectionChanged -= OnBeatCollectionChanged;
            }

            if (newValue is INotifyCollectionChanged)
            {
                ((INotifyCollectionChanged)newValue).CollectionChanged += OnBeatCollectionChanged;
            }

            ResetItems();
        }

        private void ResetItems()
        {
            ClearSegments();
            GenerateAllSegments();
        }

        private void GenerateAllSegments()
        {
            if (Segments == null) return;

            foreach (BeatSegment segment in Segments)
            {
                AddItem(segment);
            }
        }

        private void AddItem(BeatSegment segment)
        {
            BeatContainer container = new BeatContainer();
            container.SetBeatSegment(segment);

            _containerDictionary.Add(segment, container);

            timePanel.Children.Add(container);
        }

        private void OnBeatCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Reset)
            {
                ResetItems();
            }
            else
            {
                if (args.OldItems != null)
                {
                    foreach (BeatSegment segment in args.OldItems)
                    {
                        RemoveItem(segment);
                    }
                }

                if (args.NewItems != null)
                {
                    foreach (BeatSegment segment in args.NewItems)
                    {
                        AddItem(segment);
                    }
                }
            }
        }

        private void RemoveItem(BeatSegment segment)
        {
            timePanel.Children.Remove(_containerDictionary[segment]);
            _containerDictionary.Remove(segment);
        }

        private void ClearSegments()
        {
            timePanel.Children.Clear();
            _containerDictionary.Clear();
        }

        public BeatTimeLine()
        {
            InitializeComponent();
        }
    }
}
