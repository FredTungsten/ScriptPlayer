using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public class BeatAndPositionBar : Control
    {
        public static readonly DependencyProperty IsReadonlyProperty = DependencyProperty.Register(
            "IsReadonly", typeof(bool), typeof(BeatAndPositionBar), new PropertyMetadata(default(bool)));

        public bool IsReadonly
        {
            get { return (bool) GetValue(IsReadonlyProperty); }
            set { SetValue(IsReadonlyProperty, value); }
        }

        public event EventHandler PositionsChanged;

        public static readonly DependencyProperty LineColorProperty = DependencyProperty.Register(
            "LineColor", typeof(Color), typeof(BeatAndPositionBar), new PropertyMetadata(Colors.Lime, OnVisualPropertyChanged));

        public Color LineColor
        {
            get => (Color)GetValue(LineColorProperty);
            set => SetValue(LineColorProperty, value);
        }

        public static readonly DependencyProperty LineWidthProperty = DependencyProperty.Register(
            "LineWidth", typeof(double), typeof(BeatAndPositionBar), new PropertyMetadata(4.0d, OnVisualPropertyChanged));

        public double LineWidth
        {
            get => (double)GetValue(LineWidthProperty);
            set => SetValue(LineWidthProperty, value);
        }

        public static readonly DependencyProperty PositionsProperty = DependencyProperty.Register(
            "Positions", typeof(RelativePositionCollection), typeof(BeatAndPositionBar), new PropertyMetadata(default(RelativePositionCollection), OnVisualPropertyChanged));

        public RelativePositionCollection Positions
        {
            get => (RelativePositionCollection)GetValue(PositionsProperty);
            set => SetValue(PositionsProperty, value);
        }

        public static readonly DependencyProperty BeatPatternProperty = DependencyProperty.Register(
            "BeatPattern", typeof(IList<bool>), typeof(BeatAndPositionBar), new PropertyMetadata(default(IList<bool>), OnVisualPropertyChanged));

        public IList<bool> BeatPattern
        {
            get { return (IList<bool>) GetValue(BeatPatternProperty); }
            set { SetValue(BeatPatternProperty, value); }
        }

        private bool _down;
        private Point _mousePos;
        private RelativePosition _position;

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BeatAndPositionBar)d).InvalidateVisual();
        }

        public static readonly DependencyProperty LockTimeStampProperty = DependencyProperty.Register(
            "LockTimeStamp", typeof(bool), typeof(BeatAndPositionBar), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));

        public bool LockTimeStamp
        {
            get => (bool)GetValue(LockTimeStampProperty);
            set => SetValue(LockTimeStampProperty, value);
        }

        private readonly Popup _positionPopup;

        public BeatAndPositionBar()
        {
            _positionPopup = new Popup
            {
                Child = new TextBlock
                {
                    Background = Brushes.White,
                },
                PlacementTarget = this,
                Placement = PlacementMode.Mouse,
            };
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            if (!IsReadonly)
            {
                LockTimeStamp = !LockTimeStamp;
            }

            base.OnMouseRightButtonDown(e);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!IsReadonly)
            {
                _mousePos = e.GetPosition(this);

                // Control = Remove
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    RelativePosition closest = GetClosestPosition(_mousePos);
                    if (closest != null)
                        Positions.Remove(closest);

                    InvalidateVisual();
                    OnPositionsChanged();
                }
                // Shift = Add
                else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    RelativePosition newPos = GetPositionFromPoint(_mousePos);
                    Positions.Add(newPos);

                    _position = newPos;
                    _down = true;

                    CaptureMouse();

                    InvalidateVisual();
                }
                // Neither = Move
                else
                {
                    RelativePosition closest = GetClosestPosition(_mousePos);
                    if (closest == null)
                        return;

                    _position = closest;
                    _down = true;

                    CaptureMouse();
                }

                if (_position != null && _down)
                {
                    ((TextBlock) _positionPopup.Child).Text = $"{_position.Position} @ {_position.RelativeTime:P}";
                    _positionPopup.IsOpen = true;
                }
                else
                {
                    _positionPopup.IsOpen = false;
                }
            }
        }

        private RelativePosition GetClosestPosition(Point mousePos)
        {
            List<RelativePosition> absoluteBeatPositions = Positions.ToList();

            double minDist = double.MaxValue;
            RelativePosition closest = null;

            foreach (RelativePosition position in absoluteBeatPositions)
            {
                double distance = GetPointFromPosition(position).DistanceTo(mousePos);
                if (distance < minDist)
                {
                    closest = position;
                    minDist = distance;
                }
            }

            if (closest == null)
                return null;

            if (minDist > 20) return null;

            return closest;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (!_down) return;

            _positionPopup.IsOpen = false;
            _down = false;

            if (!IsReadonly)
            {
                _mousePos = e.GetPosition(this);
                UpdateSelectedPosition(true);
                InvalidateVisual();
            }

            ReleaseMouseCapture();

            if (!IsReadonly)
            {
                OnPositionsChanged();
            }
        }

        private void UpdateSelectedPosition(bool final)
        {
            if (final)
            {
                var newPosition = GetPositionFromPoint(_mousePos);

                if (LockTimeStamp)
                    newPosition.RelativeTime = _position.RelativeTime;

                Positions.Remove(_position);
                Positions.Add(newPosition);
            }
            else
            {
                RelativePosition pos = GetPositionFromPoint(_mousePos);

                if (!LockTimeStamp)
                    _position.RelativeTime = pos.RelativeTime;

                _position.Position = pos.Position;
                ((TextBlock)_positionPopup.Child).Text = $"{_position.Position} @ {_position.RelativeTime:P}";
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (IsReadonly)
                return;

            if (!_down) return;

            _mousePos = e.GetPosition(this);
            UpdateSelectedPosition(false);
            InvalidateVisual();
        }

        private Point GetPointFromPosition(RelativePosition position)
        {
            double x = PositionToX(position.RelativeTime);
            double y = PositionToY(position.Position);

            return new Point(x, y);
        }

        private double PositionToX(double relativeTime)
        {
            return relativeTime * ActualWidth;
        }

        private double PositionToY(byte position)
        {
            return (ActualHeight - 2 * CircleRadius) * ((99.0 - position) / 99.0) + CircleRadius;
        }

        private RelativePosition GetPositionFromPoint(Point point)
        {
            double timestamp = XToPosition(point.X);
            byte position = YToPosition(point.Y);
            position = Math.Min((byte)99, Math.Max((byte)0, position));

            if (BeatPattern != null)
            {
                double snapRange = 10;

                List<double> snapPositions = new List<double>();

                for(int i = 0; i < (BeatPattern.Count -1) * 4; i++)
                    snapPositions.Add(PositionToX(i / (double)((BeatPattern.Count - 1) * 4)));

                double minRange = double.MaxValue;
                double minPos = 0;

                foreach (double snapPos in snapPositions)
                {
                    double distance = Math.Abs(snapPos - point.X);
                    if (distance < minRange)
                    {
                        minRange = distance;
                        minPos = snapPos;
                    }
                }

                if (minRange <= snapRange)
                    timestamp = XToPosition(minPos);
            }

            return new RelativePosition
            {
                RelativeTime = timestamp,
                Position = position
            };
        }

        private double XToPosition(double x)
        {
            return Math.Max(0, Math.Min(1, x / ActualWidth));
        }

        private byte YToPosition(double y)
        {
            double usableHeight = ActualHeight - 2 * CircleRadius;

            byte val = (byte)(Math.Min(usableHeight, Math.Max(0, usableHeight - (y - CircleRadius))) / usableHeight * 99.0);

            int valRound = (int)(Math.Round(val / 5.0) * 5);

            return (byte)Math.Min(99, Math.Max(0, valRound));
        }

        private static double CircleRadius = 4;

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (_down)
            {
                UpdateSelectedPosition(false);
            }

            Rect fullRect = new Rect(new Point(), new Size(ActualWidth, ActualHeight));

            drawingContext.PushClip(new RectangleGeometry(fullRect));
            drawingContext.DrawRectangle(Background, null, fullRect);

            var gridBrush = LockTimeStamp ? Brushes.Red : Brushes.Green;
            Pen gridPen = new Pen(gridBrush, 1);

            drawingContext.PushOpacity(0.3);
            for (int i = 0; i <= 4; i++)
            {
                drawingContext.DrawLine(gridPen, new Point(0, i * ActualHeight / 4.0),
                    new Point(ActualWidth, i * ActualHeight / 4.0));
            }

            drawingContext.Pop();

            if (BeatPattern != null)
            {
                Pen linePen = new Pen(Brushes.Aquamarine, 3);
                Pen noline = new Pen(new SolidColorBrush(HeatMapGenerator.MixColors(Colors.Aquamarine, Colors.Black, 0.8)), 1);

                int subdivisions = 4;

                for(int i = 0; i < (BeatPattern.Count - 1) * subdivisions; i++)
                {
                    double x = (ActualWidth / ((BeatPattern.Count - 1) * subdivisions )) * i;

                    Pen pen = noline;

                    if (i % subdivisions == 0 && BeatPattern[i / subdivisions])
                    {
                        pen = linePen;
                    }

                    drawingContext.DrawLine(pen, new Point(x, 0), new Point(x, ActualHeight));
                }
            }

            if (Positions != null)
            {
                List<RelativePosition> absoluteBeatPositions = Positions.ToList();
                List<Point> beatPoints = absoluteBeatPositions.Select(GetPointFromPosition).ToList();

                if (beatPoints.Count > 0)
                {
                    for (int i = 1; i < beatPoints.Count; i++)
                    {
                        Color color = Colors.Lime;
                        drawingContext.DrawLine(new Pen(new SolidColorBrush(color), 1), beatPoints[i - 1], beatPoints[i]);
                    }

                    
                    for (int i = 0; i < beatPoints.Count; i++)
                    {
                        Color color = Colors.Lime;
                        Color fillColor = HeatMapGenerator.MixColors(color, Colors.Black, 0.5);

                        drawingContext.DrawEllipse(new SolidColorBrush(fillColor),
                            new Pen(new SolidColorBrush(color), 1), beatPoints[i], CircleRadius, CircleRadius);
                    }
                    
                }
            }

            drawingContext.Pop();
        }

        protected virtual void OnPositionsChanged()
        {
            PositionsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class RelativePosition
    {
        public double RelativeTime { get; set; }
        public byte Position { get; set; }
    }

    public class RelativePositionCollection : ICollection<RelativePosition>
    {
        private readonly List<RelativePosition> _positions;

        public RelativePositionCollection()
        {
            _positions = new List<RelativePosition>();
        }

        public RelativePositionCollection(IEnumerable<RelativePosition> beats)
        {
            if (beats == null)
            {
                _positions = new List<RelativePosition>();
            }
            else
            {
                _positions = new List<RelativePosition>(beats);
                _positions.Sort((a, b) => a.RelativeTime.CompareTo(b.RelativeTime));
            }
        }

        public RelativePosition this[int index]
        {
            get => _positions[index];
        }

        public bool Remove(RelativePosition item)
        {
            return _positions.Remove(item);
        }

        public int Count => _positions.Count;
        public bool IsReadOnly => false;

        private int FindFirstLaterThan(int minIndex, int maxIndex, double relativePosition)
        {
            for (int i = minIndex; i <= maxIndex; i++)
            {
                if (_positions[i].RelativeTime >= relativePosition)
                    return i;
            }

            return -1;
        }

        private int FindLastEarlierThan(int minIndex, int maxIndex, double relativePosition)
        {
            if (minIndex < 0 || minIndex > _positions.Count - 1) return -1;
            if (maxIndex < 0 || maxIndex > _positions.Count - 1) return -1;

            if (_positions[_positions.Count - 1].RelativeTime < relativePosition)
                return _positions.Count - 1;

            int l = minIndex;
            int r = maxIndex;

            while (l <= r)
            {
                int m = (l + r) / 2;

                if (_positions[m].RelativeTime > relativePosition)
                {
                    if (m > minIndex)
                        if (_positions[m - 1].RelativeTime <= relativePosition)
                            return m - 1;

                    r = m - 1;
                }
                else
                {
                    if (m < maxIndex)
                        if (_positions[m + 1].RelativeTime > relativePosition)
                            return m;

                    l = m + 1;
                }
            }

            //No element found
            return -1;
        }

        public void Add(RelativePosition beat)
        {
            if (_positions.Count == 0)
                _positions.Add(beat);
            else
            {
                int newIndex = FindLastEarlierThan(0, _positions.Count - 1, beat.RelativeTime);
                _positions.Insert(newIndex + 1, beat);
            }
        }

        public void Clear()
        {
            _positions.Clear();
        }

        public bool Contains(RelativePosition item)
        {
            return _positions.Contains(item);
        }

        public void CopyTo(RelativePosition[] array, int arrayIndex)
        {
            _positions.CopyTo(array, arrayIndex);
        }

        public IEnumerator<RelativePosition> GetEnumerator()
        {
            return _positions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public RelativePositionCollection Duplicate()
        {
            return new RelativePositionCollection(_positions);
        }

        public bool IsIdenticalTo(RelativePositionCollection other)
        {
            if (other.Count != Count)
                return false;

            for (int i = 0; i < Count; i++)
            {
                if (other[i].Position != this[i].Position)
                    return false;

                if (Math.Abs(other[i].RelativeTime - this[i].RelativeTime) > double.Epsilon)
                    return false;
            }

            return true;
        }
    }
}
