using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
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

        private bool _down;
        private Point _mousePos;
        private TimedPosition _position;

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PositionBar)d).InvalidateVisual();
        }

        public TimeSpan Progress
        {
            get { return (TimeSpan)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _mousePos = e.GetPosition(this);

            TimeSpan timeFrom = Progress - TotalDisplayedDuration.Multiply(Midpoint);
            TimeSpan timeTo = Progress + TotalDisplayedDuration.Multiply(1 - Midpoint);

            List<TimedPosition> absoluteBeatPositions = Positions.GetPositions(timeFrom, timeTo).ToList();

            double minDist = double.MaxValue;
            TimedPosition closest = null;

            foreach (TimedPosition position in absoluteBeatPositions)
            {
                double distance = GetPointFromPosition(position).DistanceTo(_mousePos);
                if (distance < minDist)
                {
                    closest = position;
                    minDist = distance;
                }
            }

            if (closest == null) return;

            if (minDist > 20) return;

            _position = closest;
            _down = true;
            
            CaptureMouse();
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (!_down) return;

            _down = false;
            _mousePos = e.GetPosition(this);
            UpdateSelectedPosition();
            InvalidateVisual();

            ReleaseMouseCapture();
        }

        private void UpdateSelectedPosition()
        {
            TimedPosition pos = GetPositionFromPoint(_mousePos);
            _position.TimeStamp = pos.TimeStamp;
            _position.Position = pos.Position;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!_down) return;

            _mousePos = e.GetPosition(this);
            UpdateSelectedPosition();
            InvalidateVisual();
        }

        private Point GetPointFromPosition(TimedPosition position)
        {
            

            double x = PositionToX(position.TimeStamp);
            double y = PositionToY(position.Position);

            return new Point(x,y);
        }

        private double PositionToX(TimeSpan timeStamp)
        {
            return (timeStamp - (Progress - TotalDisplayedDuration.Multiply(Midpoint))).Divide(TotalDisplayedDuration) * ActualWidth;
        }

        private double PositionToY(byte position)
        {
            return ActualHeight * ((99.0 - position) / 99.0);
        }

        private TimedPosition GetPositionFromPoint(Point point)
        {
            TimeSpan timestamp = XToPosition(point.X);
            byte position = YToPosition(point.Y);
            position = Math.Min((byte)99, Math.Max((byte)0, position));

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
            return (byte)(Math.Min(ActualHeight, Math.Max(0, ActualHeight - y)) / ActualHeight * 99.0);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (_down)
            {
                UpdateSelectedPosition();
            }

            Pen redPen = new Pen(Brushes.Red, 1);
            Rect fullRect = new Rect(new Point(), new Size(ActualWidth, ActualHeight));

            drawingContext.PushClip(new RectangleGeometry(fullRect));

            drawingContext.DrawRectangle(Background, null, fullRect);

            double midPointX = ActualWidth * Midpoint;

            drawingContext.DrawLine(redPen, new Point(midPointX, 0), new Point(midPointX, ActualHeight));

            if (Positions != null)
            {
                TimeSpan timeFrom = Progress - TotalDisplayedDuration.Multiply(Midpoint);
                TimeSpan timeTo = Progress + TotalDisplayedDuration.Multiply(1 - Midpoint);

                List<TimedPosition> absoluteBeatPositions = Positions.GetPositions(timeFrom, timeTo).ToList();
                //double ToLocal(TimedPosition b) => (b.TimeStamp - timeFrom).Divide(timeTo - timeFrom) * ActualWidth;

                List<Point> beatPoints = absoluteBeatPositions.Select(GetPointFromPosition).ToList();

                if (beatPoints.Count > 0)
                {
                    PathFigure figure = new PathFigure {StartPoint = beatPoints[0]};

                    for (int i = 1; i < beatPoints.Count; i++)
                    {
                        figure.Segments.Add(new LineSegment(beatPoints[i], true));
                    }

                    drawingContext.DrawGeometry(null, redPen, new PathGeometry(new[] {figure}));

                    for (int i = 0; i < beatPoints.Count; i++)
                    {
                        drawingContext.DrawEllipse(Brushes.Black, redPen, beatPoints[i], 4, 4);
                    }
                }
            }

            drawingContext.Pop();
        }
    }
}
