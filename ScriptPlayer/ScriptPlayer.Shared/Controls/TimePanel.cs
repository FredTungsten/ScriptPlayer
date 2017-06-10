using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public class TimePanel : Panel
    {
        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TimePanel)d).InvalidateMeasure();
            ((TimePanel)d).InvalidateArrange();
            ((TimePanel)d).InvalidateVisual();
        }

        public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(
            "Rows", typeof(int), typeof(TimePanel), new PropertyMetadata(1, OnVisualPropertyChanged));

        public int Rows
        {
            get { return (int) GetValue(RowsProperty); }
            set { SetValue(RowsProperty, value); }
        }

        public static readonly DependencyProperty ViewPortProperty = DependencyProperty.Register(
            "ViewPort", typeof(TimeSpan), typeof(TimePanel), new PropertyMetadata(default(TimeSpan), OnVisualPropertyChanged));

        public TimeSpan ViewPort
        {
            get { return (TimeSpan)GetValue(ViewPortProperty); }
            set { SetValue(ViewPortProperty, value); }
        }

        public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(
            "Offset", typeof(TimeSpan), typeof(TimePanel), new PropertyMetadata(default(TimeSpan), OnVisualPropertyChanged));

        public TimeSpan Offset
        {
            get { return (TimeSpan)GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }

        public static readonly DependencyProperty PositionProperty = DependencyProperty.RegisterAttached(
            "Position", typeof(TimeSpan), typeof(TimePanel), new PropertyMetadata(default(TimeSpan), OnPositionPropertyChanged));

        private static void OnPositionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimePanel panel = VisualTreeHelper.GetParent(d) as TimePanel;
            panel?.ChildPositionChanged(d, (TimeSpan) e.OldValue, (TimeSpan) e.NewValue);
        }

        private static void OnDurationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimePanel panel = VisualTreeHelper.GetParent(d) as TimePanel;
            panel?.ChildDurationChanged(d, (TimeSpan)e.OldValue, (TimeSpan)e.NewValue);
        }

        private void ChildDurationChanged(DependencyObject child, TimeSpan oldValue, TimeSpan newValue)
        {
            TimeSpan position = GetPosition(child);

            if (IsChildVisible(position, oldValue) || IsChildVisible(position, newValue))
            {
                InvalidateMeasure();
            }
        }

        private void ChildPositionChanged(DependencyObject child, TimeSpan oldValue, TimeSpan newValue)
        {
            TimeSpan duration = GetDuration(child);

            if (IsChildVisible(oldValue, duration) || IsChildVisible(newValue, duration))
            {
                InvalidateArrange();
            }
        }

        private void ChildRowChanged(DependencyObject child, TimeSpan oldValue, TimeSpan newValue)
        {
            TimeSpan position = GetPosition(child);
            TimeSpan duration = GetDuration(child);

            if (IsChildVisible(position, duration))
                InvalidateArrange();
        }

        private bool IsChildVisible(TimeSpan position, TimeSpan duration)
        {
            if (position > Offset + ViewPort) return false;
            if (position + duration < Offset) return false;
            return true;
        }

        public static readonly DependencyProperty RowProperty = DependencyProperty.RegisterAttached(
            "Row", typeof(int), typeof(TimePanel), new PropertyMetadata(default(int), OnRowPropertyChanged));

        private static void OnRowPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimePanel panel = VisualTreeHelper.GetParent(d) as TimePanel;
            panel?.ChildRowChanged(d, (TimeSpan)e.OldValue, (TimeSpan)e.NewValue);
        }

        public static void SetRow(DependencyObject element, int value)
        {
            element.SetValue(RowProperty, value);
        }

        public static int GetRow(DependencyObject element)
        {
            return (int) element.GetValue(RowProperty);
        }

        private Dictionary<UIElement, BeatContainerAdorner> _adorners = new Dictionary<UIElement, BeatContainerAdorner>();

        public static void SetPosition(DependencyObject element, TimeSpan value)
        {
            element.SetValue(PositionProperty, value);
        }

        public static TimeSpan GetPosition(DependencyObject element)
        {
            return (TimeSpan)element.GetValue(PositionProperty);
        }

        public static readonly DependencyProperty DurationProperty = DependencyProperty.RegisterAttached(
            "Duration", typeof(TimeSpan), typeof(TimePanel), new PropertyMetadata(default(TimeSpan), OnDurationPropertyChanged));

        public static void SetDuration(DependencyObject element, TimeSpan value)
        {
            element.SetValue(DurationProperty, value);
        }

        public static TimeSpan GetDuration(DependencyObject element)
        {
            return (TimeSpan)element.GetValue(DurationProperty);
        }

        public TimePanel()
        {
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            InvalidateMeasure();
            InvalidateArrange();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size requestedSize = availableSize;

            if (IsInfinite(requestedSize.Width))
                requestedSize.Width = 1000.0;

            if (IsInfinite(requestedSize.Height))
                requestedSize.Height = 1000.0;

            EnsureAdorners();

            foreach (UIElement child in InternalChildren)
            {
                if (!IsChildVisible(child))
                    continue;

                TimeSpan duration = GetDuration(child);
                double childWidth = duration.Divide(ViewPort) * availableSize.Width;
                child.Measure(new Size(childWidth, availableSize.Height / Rows));
            }

            return availableSize;
        }

        private bool IsChildVisible(UIElement child)
        {
            return IsChildVisible(GetPosition(child), GetDuration(child));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            TimeSpan xMin = Offset;

            EnsureAdorners();

            double rowHeight = finalSize.Height / Rows;

            foreach (UIElement child in InternalChildren)
            {
                TimeSpan start = GetPosition(child);
                TimeSpan duration = GetDuration(child);

                if (!IsChildVisible(start, duration))
                {
                    child.Visibility = Visibility.Hidden;
                }
                else
                {

                    int row = GetRow(child);

                    if (duration <= TimeSpan.Zero)
                        continue;

                    TimeSpan childStartOffset = start - xMin;
                    double childStartOffsetWidths = childStartOffset.Divide(ViewPort);
                    double childXstart = childStartOffsetWidths * ActualWidth;
                    double childWidth = duration.Divide(ViewPort) * ActualWidth;

                    Rect childRect = new Rect(new Point(childXstart, row * rowHeight), new Size(childWidth, rowHeight));

                    child.Arrange(childRect);

                    child.Visibility = Visibility.Visible;
                }
            }

            return finalSize;
        }

        private void EnsureAdorners()
        {
            List<BeatContainer> containers = new List<BeatContainer>();

            foreach (UIElement element in InternalChildren)
            {
                if (element is BeatContainer)
                    containers.Add((BeatContainer)element);
            }

            var removed = _adorners.Where(kvp => !containers.Contains(kvp.Key)).ToList();
            var added = containers.Where(c => !_adorners.ContainsKey(c)).ToList();

            if (added.Count > 0 || removed.Count > 0)
            {
                AdornerLayer layer = AdornerLayer.GetAdornerLayer(this);

                foreach (KeyValuePair<UIElement, BeatContainerAdorner> container in removed)
                {
                    BeatContainerAdorner adorner = container.Value;
                    adorner.DragEnded -= AdornerOnDragEnded;
                    adorner.DragStarted -= AdornerOnDragStarted;
                    adorner.DragDelta -= AdornerOnDragDelta;

                    _adorners.Remove(container.Key);
                    layer.Remove(adorner);
                }

                foreach (BeatContainer container in added)
                {
                    BeatContainerAdorner adorner = new BeatContainerAdorner(container);
                    adorner.DragEnded += AdornerOnDragEnded;
                    adorner.DragStarted += AdornerOnDragStarted;
                    adorner.DragDelta += AdornerOnDragDelta;
                    _adorners.Add(container, adorner);
                    layer.Add(adorner);
                }
            }
        }

        private void AdornerOnDragEnded(object sender, Dock dock, double delta)
        {
            
        }

        private void AdornerOnDragDelta(object sender, Dock dock, double delta)
        {
            HandleDrag(sender, dock, delta);
        }

        private void AdornerOnDragStarted(object sender, Dock dock, double delta)
        {
            HandleDrag(sender, dock, delta);
        }

        private void HandleDrag(object sender, Dock thumbposition, double delta)
        {
            BeatContainerAdorner adorner = (BeatContainerAdorner)sender;
            UIElement container = adorner.AdornedElement;

            TimeSpan changedSpan = ViewPort.Multiply(delta / ActualWidth);

            switch (thumbposition)
            {
                case Dock.Left:
                    {
                        TimeSpan duration = GetDuration(container) - changedSpan;
                        TimeSpan position = GetPosition(container) + changedSpan;

                        SetDuration(container, duration);
                        SetPosition(container, position);
                        break;
                    }
                case Dock.Right:
                    {
                        TimeSpan duration = GetDuration(container) + changedSpan;
                        SetDuration(container, duration);
                        break;
                    }
                case Dock.Top:
                    {
                        TimeSpan position = GetPosition(container) + changedSpan;
                        SetPosition(container, position);
                        break;
                    }
            }
        }

        private bool IsInfinite(double length)
        {
            return Double.IsNaN(length) || Double.IsInfinity(length);
        }


    }
}
