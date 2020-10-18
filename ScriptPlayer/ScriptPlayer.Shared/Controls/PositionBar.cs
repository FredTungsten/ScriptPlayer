using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public class PositionBar : Control
    {
        public static readonly DependencyProperty DrawLinesProperty = DependencyProperty.Register(
            "DrawLines", typeof(bool), typeof(PositionBar), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool DrawLines
        {
            get => (bool)GetValue(DrawLinesProperty);
            set => SetValue(DrawLinesProperty, value);
        }

        public static readonly DependencyProperty DrawZeroProperty = DependencyProperty.Register(
            "DrawZero", typeof(bool), typeof(PositionBar), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool DrawZero
        {
            get => (bool)GetValue(DrawZeroProperty);
            set => SetValue(DrawZeroProperty, value);
        }

        public static readonly DependencyProperty DrawCirclesProperty = DependencyProperty.Register(
            "DrawCircles", typeof(bool), typeof(PositionBar), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool DrawCircles
        {
            get => (bool)GetValue(DrawCirclesProperty);
            set => SetValue(DrawCirclesProperty, value);
        }

        public static readonly DependencyProperty MinCommandDelayProperty = DependencyProperty.Register(
            "MinCommandDelay", typeof(TimeSpan), typeof(PositionBar), new PropertyMetadata(TimeSpan.FromMilliseconds(150), OnVisualPropertyChanged));

        public TimeSpan MinCommandDelay
        {
            get => (TimeSpan)GetValue(MinCommandDelayProperty);
            set => SetValue(MinCommandDelayProperty, value);
        }

        public static readonly DependencyProperty LineColorProperty = DependencyProperty.Register(
            "LineColor", typeof(Color), typeof(PositionBar), new PropertyMetadata(Colors.Lime, OnVisualPropertyChanged));

        public Color LineColor
        {
            get => (Color)GetValue(LineColorProperty);
            set => SetValue(LineColorProperty, value);
        }

        public static readonly DependencyProperty LineWidthProperty = DependencyProperty.Register(
            "LineWidth", typeof(double), typeof(PositionBar), new PropertyMetadata(4.0d, OnVisualPropertyChanged));

        public double LineWidth
        {
            get => (double)GetValue(LineWidthProperty);
            set => SetValue(LineWidthProperty, value);
        }

        public static readonly DependencyProperty PositionsProperty = DependencyProperty.Register(
            "Positions", typeof(PositionCollection), typeof(PositionBar), new PropertyMetadata(default(PositionCollection), OnVisualPropertyChanged));

        public PositionCollection Positions
        {
            get => (PositionCollection)GetValue(PositionsProperty);
            set => SetValue(PositionsProperty, value);
        }

        public static readonly DependencyProperty TotalDisplayedDurationProperty = DependencyProperty.Register(
            "TotalDisplayedDuration", typeof(TimeSpan), typeof(PositionBar), new PropertyMetadata(TimeSpan.FromSeconds(5), OnVisualPropertyChanged));

        public TimeSpan TotalDisplayedDuration
        {
            get => (TimeSpan)GetValue(TotalDisplayedDurationProperty);
            set => SetValue(TotalDisplayedDurationProperty, value);
        }

        public static readonly DependencyProperty MidpointProperty = DependencyProperty.Register(
            "Midpoint", typeof(double), typeof(PositionBar), new PropertyMetadata(0.5d, OnVisualPropertyChanged));

        public double Midpoint
        {
            get => (double)GetValue(MidpointProperty);
            set => SetValue(MidpointProperty, value);
        }

        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
            "Progress", typeof(TimeSpan), typeof(PositionBar), new PropertyMetadata(default(TimeSpan), OnVisualPropertyChanged));

        private bool _down;
        private Point _mousePos;
        private TimedPosition _position;

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PositionBar)d).InvalidateVisual();
        }

        public TimeSpan Progress
        {
            get => (TimeSpan)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
            "IsReadOnly", typeof(bool), typeof(PositionBar), new PropertyMetadata(default(bool)));

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public static readonly DependencyProperty LockTimeStampProperty = DependencyProperty.Register(
            "LockTimeStamp", typeof(bool), typeof(PositionBar), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.AffectsRender));

        public bool LockTimeStamp
        {
            get => (bool)GetValue(LockTimeStampProperty);
            set => SetValue(LockTimeStampProperty, value);
        }

        private readonly Popup _positionPopup;

        public PositionBar()
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
            LockTimeStamp = !LockTimeStamp;
            base.OnMouseRightButtonDown(e);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (IsReadOnly)
                return;

            _mousePos = e.GetPosition(this);

            // Control = Remove
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                TimedPosition closest = GetClosestPosition(_mousePos);
                if (closest != null)
                    Positions.Remove(closest);

                InvalidateVisual();
            }
            // Shift = Add
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                TimedPosition newPos = GetPositionFromPoint(_mousePos);
                Positions.Add(newPos);

                _position = newPos;
                _down = true;

                CaptureMouse();

                InvalidateVisual();
            }
            // Neither = Move
            else
            {
                TimedPosition closest = GetClosestPosition(_mousePos);
                if (closest == null)
                    return;

                _position = closest;
                _down = true;

                CaptureMouse();
            }

            if (_position != null && _down)
            {
                ((TextBlock)_positionPopup.Child).Text = $"{_position.Position} @ {_position.TimeStamp:g}";
                _positionPopup.IsOpen = true;
            }
            else
            {
                _positionPopup.IsOpen = false;
            }
        }

        private TimedPosition GetClosestPosition(Point mousePos)
        {
            TimeSpan timeFrom = Progress - TotalDisplayedDuration.Multiply(Midpoint);
            TimeSpan timeTo = Progress + TotalDisplayedDuration.Multiply(1 - Midpoint);

            List<TimedPosition> absoluteBeatPositions = Positions.GetPositions(timeFrom, timeTo).ToList();

            double minDist = double.MaxValue;
            TimedPosition closest = null;

            foreach (TimedPosition position in absoluteBeatPositions)
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

            if (IsReadOnly)
                return;

            _positionPopup.IsOpen = false;
            _down = false;
            _mousePos = e.GetPosition(this);
            UpdateSelectedPosition(true);
            InvalidateVisual();

            ReleaseMouseCapture();
        }

        private void UpdateSelectedPosition(bool final)
        {
            if (final)
            {
                var newPosition = GetPositionFromPoint(_mousePos);

                if (LockTimeStamp)
                    newPosition.TimeStamp = _position.TimeStamp;

                Positions.Remove(_position);
                Positions.Add(newPosition);
            }
            else
            {
                TimedPosition pos = GetPositionFromPoint(_mousePos);

                if (!LockTimeStamp)
                    _position.TimeStamp = pos.TimeStamp;

                _position.Position = pos.Position;
                ((TextBlock)_positionPopup.Child).Text = $"{_position.Position} @ {_position.TimeStamp:g}";
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!_down) return;

            if (IsReadOnly)
                return;

            _mousePos = e.GetPosition(this);
            UpdateSelectedPosition(false);
            InvalidateVisual();
        }

        private Point GetPointFromPosition(TimedPosition position)
        {
            double x = PositionToX(position.TimeStamp);
            double y = PositionToY(position.Position);

            return new Point(x, y);
        }

        private double PositionToX(TimeSpan timeStamp)
        {
            return (timeStamp - (Progress - TotalDisplayedDuration.Multiply(Midpoint))).Divide(TotalDisplayedDuration) * ActualWidth;
        }

        private double PositionToY(byte position)
        {
            return (ActualHeight - 2 * CircleRadius) * ((99.0 - position) / 99.0) + CircleRadius;
        }

        private TimedPosition GetPositionFromPoint(Point point)
        {
            TimeSpan timestamp = XToPosition(point.X);
            byte position = YToPosition(point.Y);

            int rounded = (int)(Math.Round(position / 5.0) * 5.0);

            position = (byte)Math.Min(99, Math.Max(0, rounded));

            return new TimedPosition
            {
                TimeStamp = timestamp,
                Position = position
            };
        }

        private TimeSpan XToPosition(double x)
        {
            return TotalDisplayedDuration.Multiply(x / ActualWidth) + (Progress - TotalDisplayedDuration.Multiply(Midpoint));
        }

        private byte YToPosition(double y)
        {
            double usableHeight = ActualHeight - 2 * CircleRadius;
            return (byte)(Math.Min(usableHeight, Math.Max(0, usableHeight - (y - CircleRadius))) / usableHeight * 99.0);
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

            if (DrawLines)
            {
                drawingContext.PushOpacity(0.3);
                for (int i = 0; i <= 4; i++)
                {
                    drawingContext.DrawLine(gridPen, new Point(0, i * ActualHeight / 4.0),
                        new Point(ActualWidth, i * ActualHeight / 4.0));
                }

                drawingContext.Pop();
            }

            if (DrawZero)
            {
                double midPointX = ActualWidth * Midpoint;

                drawingContext.DrawLine(gridPen, new Point(midPointX, 0), new Point(midPointX, ActualHeight));
            }

            if (Positions != null)
            {
                TimeSpan timeFrom = Progress - TotalDisplayedDuration.Multiply(Midpoint);
                TimeSpan timeTo = Progress + TotalDisplayedDuration.Multiply(1 - Midpoint);

                List<TimedPosition> absoluteBeatPositions = Positions.GetPositions(timeFrom, timeTo).ToList();
                List<Point> beatPoints = absoluteBeatPositions.Select(GetPointFromPosition).ToList();

                if (beatPoints.Count > 0)
                {
                    for (int i = 1; i < beatPoints.Count; i++)
                    {
                        TimeSpan duration = absoluteBeatPositions[i].TimeStamp - absoluteBeatPositions[i - 1].TimeStamp;

                        double speed = SpeedPredictor.PredictSpeed2(absoluteBeatPositions[i - 1].Position, absoluteBeatPositions[i].Position, duration) / 99.0;

                        Color color = HeatMapGenerator.GetColorAtPosition(HeatMapGenerator.HeatMap, speed);
                        drawingContext.DrawLine(new Pen(new SolidColorBrush(color), 1), beatPoints[i - 1], beatPoints[i]);
                    }

                    if (DrawCircles)
                    {
                        for (int i = 0; i < beatPoints.Count; i++)
                        {
                            Color color;

                            if (i == 0)
                                color = Colors.Lime;
                            else
                            {
                                TimeSpan duration = absoluteBeatPositions[i].TimeStamp -
                                                    absoluteBeatPositions[i - 1].TimeStamp;
                                double durationFactor = MinCommandDelay.Divide(duration);
                                color = HeatMapGenerator.GetColorAtPosition(HeatMapGenerator.HeatMap3, durationFactor);
                            }

                            Color fillColor = HeatMapGenerator.MixColors(color, Colors.Black, 0.5);

                            drawingContext.DrawEllipse(new SolidColorBrush(fillColor),
                                new Pen(new SolidColorBrush(color), 1), beatPoints[i], CircleRadius, CircleRadius);
                        }
                    }
                }
            }

            drawingContext.Pop();
        }
    }
}
