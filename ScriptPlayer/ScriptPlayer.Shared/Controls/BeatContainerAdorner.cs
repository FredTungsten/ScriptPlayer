using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ScriptPlayer.Shared.Dialogs;

namespace ScriptPlayer.Shared
{
    public class BeatContainerAdorner : Adorner
    {
        public delegate void DragEventHandler(object sender, Dock position, double delta);

        public event DragEventHandler DragStarted;
        public event DragEventHandler DragDelta;
        public event DragEventHandler DragEnded;

        private readonly Thumb _thumbLeft;
        private readonly Thumb _thumbRight;
        private readonly Thumb _thumbTop;
        private static TimeSpan _copiedDuration = TimeSpan.FromSeconds(1);

        protected override Visual GetVisualChild(int index)
        {
            switch (index)
            {
                case 0: return _thumbLeft;
                case 1: return _thumbRight;
                case 2: return _thumbTop;
                default: return null;
            }
        }

        protected override int VisualChildrenCount => 3;

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(new Point(0,0), new Size(ActualWidth, ActualHeight)));
        }

        protected override Size MeasureOverride(Size constraint)
        {
            _thumbLeft.Measure(constraint);
            _thumbRight.Measure(constraint);
            _thumbTop.Measure(constraint);

            return base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _thumbLeft.Arrange(new Rect(new Point(0, 0), finalSize));
            _thumbRight.Arrange(new Rect(new Point(0, 0), finalSize));
            _thumbTop.Arrange(new Rect(new Point(0, 0), finalSize));

            return base.ArrangeOverride(finalSize);
        }

        public BeatContainerAdorner(BeatContainer adornedElement) : base(adornedElement)
        {
            Container = adornedElement;

            MenuItem mnuTimeLock = new MenuItem
            {
                Header = "Lock",
                IsCheckable = true,
                IsChecked = adornedElement.BeatLine.TimeLock
            };

            mnuTimeLock.Click += MnuTimeLockOnClick;

            MenuItem mnuEditPattern = new MenuItem
            {
                Header = "Edit Pattern",
            };

            mnuEditPattern.Click += MnuEditPatternOnClick;

            MenuItem mnuSnap = new MenuItem
            {
                Header = "Snap",
            };

            mnuSnap.Click += MnuSnapOnClick;

            MenuItem mnuCopyDuration = new MenuItem
            {
                Header = "Copy Beat Duration",
            };

            mnuCopyDuration.Click += MnuCopyDuration;

            MenuItem mnuPasteDuration = new MenuItem
            {
                Header = "Paste Beat Duration",
            };

            mnuPasteDuration.Click += MnuPasteDuration;

            ContextMenu = new ContextMenu();

            ContextMenu.Items.Add(mnuTimeLock);
            ContextMenu.Items.Add(mnuEditPattern);
            ContextMenu.Items.Add(mnuSnap);
            ContextMenu.Items.Add(new Separator());
            ContextMenu.Items.Add(mnuCopyDuration);
            ContextMenu.Items.Add(mnuPasteDuration);

            _thumbLeft = new Thumb
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 10,
                Opacity = 0,
                Margin = new Thickness(0, 20, 0, 0)
            };

            _thumbLeft.DragStarted += ThumbOnDragStarted;
            _thumbLeft.DragDelta += ThumbOnDragDelta;
            _thumbLeft.DragCompleted += ThumbOnDragCompleted;

            _thumbLeft.Cursor = Cursors.ScrollWE;

            AddVisualChild(_thumbLeft);

            _thumbRight = new Thumb
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Right,
                Width = 10,
                Opacity = 0,
                Margin = new Thickness(0, 20, 0, 0)
            };

            _thumbRight.DragStarted += ThumbOnDragStarted;
            _thumbRight.DragDelta += ThumbOnDragDelta;
            _thumbRight.DragCompleted += ThumbOnDragCompleted;

            _thumbRight.Cursor = Cursors.ScrollWE;

            AddVisualChild(_thumbRight);

            _thumbTop = new Thumb
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = 20,
                Opacity = 0
            };

            _thumbTop.DragStarted += ThumbOnDragStarted;
            _thumbTop.DragDelta += ThumbOnDragDelta;
            _thumbTop.DragCompleted += ThumbOnDragCompleted;

            _thumbTop.Cursor = Cursors.ScrollAll;

            AddVisualChild(_thumbTop);
        }

        private void MnuPasteDuration(object sender, RoutedEventArgs e)
        {
            Container.SetBeatDuration(_copiedDuration);
        }

        private void MnuCopyDuration(object sender, RoutedEventArgs e)
        {
            _copiedDuration = Container.BeatLine.PatternDuration;
        }

        public BeatContainer Container { get; set; }

        private void MnuSnapOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            Container.SnapDuration();
        }

        private void MnuTimeLockOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            BeatContainer container = AdornedElement as BeatContainer;
            if (container == null) return;

            container.BeatLine.TimeLock ^= true;

            ((MenuItem) sender).IsChecked = container.BeatLine.TimeLock;
        }

        private void MnuEditPatternOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            BeatContainer container = AdornedElement as BeatContainer;
            if (container == null) return;

            BeatPatternEditor dialog = new BeatPatternEditor(container.BeatLine.BeatDefinition.Pattern);
            if (dialog.ShowDialog() != true) return;

            container.SetBeat(new BeatDefinition { Pattern = dialog.Result });
        }

        private void ThumbOnDragCompleted(object sender, DragCompletedEventArgs dragCompletedEventArgs)
        {
            OnDragEnded(GetPosition(sender), dragCompletedEventArgs.HorizontalChange);
        }

        private Dock GetPosition(object sender)
        {
            if (ReferenceEquals(sender, _thumbLeft))
                return Dock.Left;
            if (ReferenceEquals(sender, _thumbRight))
                return Dock.Right;
            if (ReferenceEquals(sender, _thumbTop))
                return Dock.Top;

            return Dock.Bottom;
        }

        private void ThumbOnDragDelta(object sender, DragDeltaEventArgs dragDeltaEventArgs)
        {
            OnDragDelta(GetPosition(sender), dragDeltaEventArgs.HorizontalChange);
        }

        private void ThumbOnDragStarted(object sender, DragStartedEventArgs dragStartedEventArgs)
        {
            OnDragStarted(GetPosition(sender), 0.0);
        }

        protected virtual void OnDragStarted(Dock position, double delta)
        {
            DragStarted?.Invoke(this, position, delta);
        }

        protected virtual void OnDragDelta(Dock position, double delta)
        {
            DragDelta?.Invoke(this, position, delta);
        }

        protected virtual void OnDragEnded(Dock position, double delta)
        {
            DragEnded?.Invoke(this, position, delta);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
        }
    }
}
