using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public struct TimedPosition
    {
        public TimeSpan TimeStamp;
        public byte Position;
    }

    public class PositionCollection : ICollection<TimedPosition>
    {
        private readonly List<TimedPosition> _positions;

        private static CultureInfo _culture = new CultureInfo("en-us");
        public PositionCollection()
        {
            _positions = new List<TimedPosition>();
        }

        public PositionCollection(IEnumerable<TimedPosition> beats)
        {
            _positions = new List<TimedPosition>(beats);
            _positions.Sort((a, b) => a.TimeStamp.CompareTo(b.TimeStamp));
        }

        public TimedPosition this[int index]
        {
            get => _positions[index];
        }

        public bool Remove(TimedPosition item)
        {
            return _positions.Remove(item);
        }

        public int Count => _positions.Count;
        public bool IsReadOnly => false;

        public IEnumerable<TimedPosition> GetPositions(TimeSpan timestampFrom, TimeSpan timestampTo)
        {
            int minIndex = 0;
            int maxIndex = _positions.Count - 1;

            int leftBounds = FindLastEarlierThan(minIndex, maxIndex, timestampFrom);

            if (leftBounds != -1)
            {
                int rightBounds = FindFirstLaterThan(leftBounds, maxIndex, timestampTo);

                if (rightBounds != -1)
                    return _positions.Skip(leftBounds).Take(rightBounds - leftBounds + 1);
                else
                    return _positions.Skip(leftBounds);
            }
            else
            {
                int rightBounds = FindFirstLaterThan(0, maxIndex, timestampTo);
                return _positions.Take(rightBounds + 1);
            }
        }

        private int FindFirstLaterThan(int minIndex, int maxIndex, TimeSpan timestampTo)
        {
            for (int i = minIndex; i <= maxIndex; i++)
            {
                if (_positions[i].TimeStamp >= timestampTo)
                    return i;
            }

            return -1;
        }

        private int FindLastEarlierThan(int minIndex, int maxIndex, TimeSpan t)
        {
            int l = minIndex;
            int r = maxIndex;

            while (l <= r)
            {
                int m = (l + r) / 2;

                if (_positions[m].TimeStamp > t)
                {
                    if (m > minIndex)
                        if (_positions[m - 1].TimeStamp <= t)
                            return m - 1;

                    r = m - 1;
                }
                else
                {
                    if (m < maxIndex)
                        if (_positions[m + 1].TimeStamp > t)
                            return m;

                    l = m + 1;
                }
            }

            //No element found
            return -1;
        }

        public void Add(TimedPosition beat)
        {
            _positions.Add(beat);
        }

        public void Clear()
        {
            _positions.Clear();
        }

        public bool Contains(TimedPosition item)
        {
            return _positions.Contains(item);
        }

        public void CopyTo(TimedPosition[] array, int arrayIndex)
        {
            _positions.CopyTo(array, arrayIndex);
        }

        public IEnumerator<TimedPosition> GetEnumerator()
        {
            return _positions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public PositionCollection Duplicate()
        {
            return new PositionCollection(_positions);
        }
    }

    public class PositionBar : Control
    {
        public static readonly DependencyProperty LineColorProperty = DependencyProperty.Register(
            "LineColor", typeof(Color), typeof(PositionBar), new PropertyMetadata(Colors.Lime, OnVisualPropertyChanged));

        public Color LineColor
        {
            get { return (Color)GetValue(LineColorProperty); }
            set { SetValue(LineColorProperty, value); }
        }

        public static readonly DependencyProperty LineWidthProperty = DependencyProperty.Register(
            "LineWidth", typeof(double), typeof(PositionBar), new PropertyMetadata(4.0d, OnVisualPropertyChanged));

        public double LineWidth
        {
            get { return (double)GetValue(LineWidthProperty); }
            set { SetValue(LineWidthProperty, value); }
        }

        public static readonly DependencyProperty PositionsProperty = DependencyProperty.Register(
            "Positions", typeof(PositionCollection), typeof(PositionBar), new PropertyMetadata(default(PositionCollection), OnVisualPropertyChanged));

        public PositionCollection Positions
        {
            get { return (PositionCollection)GetValue(PositionsProperty); }
            set { SetValue(PositionsProperty, value); }
        }

        public static readonly DependencyProperty TotalDisplayedDurationProperty = DependencyProperty.Register(
            "TotalDisplayedDuration", typeof(TimeSpan), typeof(PositionBar), new PropertyMetadata(TimeSpan.FromSeconds(5), OnVisualPropertyChanged));

        public TimeSpan TotalDisplayedDuration
        {
            get { return (TimeSpan)GetValue(TotalDisplayedDurationProperty); }
            set { SetValue(TotalDisplayedDurationProperty, value); }
        }

        public static readonly DependencyProperty MidpointProperty = DependencyProperty.Register(
            "Midpoint", typeof(double), typeof(PositionBar), new PropertyMetadata(0.5d, OnVisualPropertyChanged));

        public double Midpoint
        {
            get { return (double)GetValue(MidpointProperty); }
            set { SetValue(MidpointProperty, value); }
        }

        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
            "Progress", typeof(TimeSpan), typeof(PositionBar), new PropertyMetadata(default(TimeSpan), OnVisualPropertyChanged));

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PositionBar)d).InvalidateVisual();
        }

        public TimeSpan Progress
        {
            get { return (TimeSpan)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect fullRect = new Rect(new Point(), new Size(ActualWidth, ActualHeight));

            drawingContext.PushClip(new RectangleGeometry(fullRect));

            drawingContext.DrawRectangle(Background, null, fullRect);

            if (Positions != null)
            {
                TimeSpan timeFrom = Progress - TotalDisplayedDuration.Multiply(Midpoint);
                TimeSpan timeTo = Progress + TotalDisplayedDuration.Multiply(1 - Midpoint);

                List<TimedPosition> absoluteBeatPositions = Positions.GetPositions(timeFrom, timeTo).ToList();
                double ToLocal(TimedPosition b) => (b.TimeStamp - timeFrom).Divide(timeTo - timeFrom) * ActualWidth;

                List<Point> beatPoints = absoluteBeatPositions.Select(a => new Point(ToLocal(a),
                    ActualHeight * (a.Position / 99.0))).ToList();

                if (beatPoints.Count > 0)
                {
                    PathFigure figure = new PathFigure {StartPoint = beatPoints[0]};

                    for (int i = 1; i < beatPoints.Count; i++)
                    {
                        figure.Segments.Add(new LineSegment(beatPoints[i], true));
                    }

                    drawingContext.DrawGeometry(null, new Pen(Brushes.Red, 1), new PathGeometry(new[] { figure }));

                    for (int i = 0; i < beatPoints.Count; i++)
                    {
                        drawingContext.DrawEllipse(Brushes.Black, new Pen(Brushes.Red,1),beatPoints[i],4,4);
                    }
                }
            }

            drawingContext.Pop();
        }

        private void DrawLine(DrawingContext drawingContext, Color primary, Point pFrom, Point pTo, double lineWidth = 4)
        {
            drawingContext.DrawLine(new Pen(new SolidColorBrush(primary) { Opacity = 0.5 }, lineWidth) { LineJoin = PenLineJoin.Round }, pFrom, pTo);
            drawingContext.DrawLine(new Pen(new SolidColorBrush(Colors.White) { Opacity = 1.0 }, 2) { LineJoin = PenLineJoin.Round }, pFrom, pTo);
        }
    }
}
