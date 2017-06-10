using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public class BeatBar : Control
    {
        public static readonly DependencyProperty TimelineProperty = DependencyProperty.Register(
            "Timeline", typeof(BeatTimeline), typeof(BeatBar), new PropertyMetadata(default(BeatTimeline), OnTimelinePropertyChanged));

        private static void OnTimelinePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BeatBar)d).UpdateTimeline(e.OldValue as BeatTimeline, e.NewValue as BeatTimeline);
        }

        private void UpdateTimeline(BeatTimeline oldValue, BeatTimeline newValue)
        {
            if (newValue != null)
                newValue.ProgressChanged += Timeline_ProgressChanged;

            if (oldValue != null)
                oldValue.ProgressChanged -= Timeline_ProgressChanged;
        }

        private void Timeline_ProgressChanged(object sender, ProgressChangedEventArgs eventArgs)
        {
            _progress = eventArgs.Progress;
            InvalidateVisual();
        }

        public BeatTimeline Timeline
        {
            get { return (BeatTimeline)GetValue(TimelineProperty); }
            set { SetValue(TimelineProperty, value); }
        }

        public static readonly DependencyProperty TotalDisplayedDurationProperty = DependencyProperty.Register(
            "TotalDisplayedDuration", typeof(double), typeof(BeatBar), new PropertyMetadata(8.0d));

        public double TotalDisplayedDuration
        {
            get { return (double)GetValue(TotalDisplayedDurationProperty); }
            set { SetValue(TotalDisplayedDurationProperty, value); }
        }

        public static readonly DependencyProperty MidpointProperty = DependencyProperty.Register(
            "Midpoint", typeof(double), typeof(BeatBar), new PropertyMetadata(0.5d));

        private double _progress;

        public double Midpoint
        {
            get { return (double)GetValue(MidpointProperty); }
            set { SetValue(MidpointProperty, value); }
        }

        private bool _simpleRendering = true;

        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect fullRect = new Rect(new Point(), new Size(ActualWidth, ActualHeight));

            drawingContext.PushClip(new RectangleGeometry(fullRect));

            drawingContext.DrawRectangle(Background, null, fullRect);

            Pen linePen = new Pen(Brushes.White, 3);

            //drawingContext.DrawLine(linePen, new Point(0, ActualHeight / 2), new Point(ActualWidth, ActualHeight / 2));

            drawingContext.DrawLine(new Pen(Brushes.Red, 11), new Point(Midpoint * ActualWidth, 0), new Point(Midpoint * ActualWidth, ActualHeight));

            if (Timeline != null)
            {
                double timeFrom = _progress - Midpoint * TotalDisplayedDuration;
                double timeTo = _progress + (1 - Midpoint) * TotalDisplayedDuration;

                double position = Math.Floor(timeFrom);

                List<double> beatPositions = new List<double>();

                while (position < timeTo)
                {
                    BeatGroup group = Timeline.FindActiveGroup(position);
                    if (group == null)
                    {
                        group = Timeline.FindNextGroup(position);
                        if (group != null)
                            position = group.Start;
                    }

                    if (group == null)
                        break;

                    position = group.FindStartingPoint(position);

                    while (position < group.End && position < timeTo)
                    {
                        foreach (double beat in group.Pattern.BeatPositions)
                        {
                            double relativePosition = (((beat * group.ActualPatternDuration) + position - timeFrom) / (timeTo - timeFrom));
                            beatPositions.Add(relativePosition);
                        }

                        position += group.ActualPatternDuration;
                    }
                }

                const double safeSpace = 30;
                double y = ActualHeight / 2.0;

                if (_simpleRendering)
                {
                    DrawLine(drawingContext, Colors.Red, new Point(-safeSpace, y),
                        new Point(ActualWidth + safeSpace, y));

                    foreach (double pos in beatPositions)
                    {
                        DrawLine(drawingContext, Colors.Lime, new Point(pos * ActualWidth, -5), new Point(pos * ActualWidth, ActualHeight + 5));
                    }
                }
                else
                {

                    PathGeometry geometry = new PathGeometry();



                    if (beatPositions.Count > 0)
                    {

                        double start = Math.Min(0, beatPositions.First());
                        double end = Math.Max(1, beatPositions.Last());


                        PathFigure figure = new PathFigure { StartPoint = new Point(start * ActualWidth - safeSpace, y) };
                        geometry.Figures.Add(figure);

                        foreach (double beatPosition in beatPositions)
                        {
                            AppendSwiggely(figure, new Point(beatPosition * ActualWidth, y));
                        }

                        figure.Segments.Add(new LineSegment(new Point(end * ActualWidth + safeSpace, y), true));
                    }
                    else
                    {
                        geometry.Figures.Add(new PathFigure(new Point(-safeSpace, y), new[] { new LineSegment(new Point(ActualWidth + safeSpace, y), true) }, false));
                    }

                    drawingContext.DrawGeometry(null, new Pen(new SolidColorBrush(Colors.Red) { Opacity = 0.5 }, 4) { LineJoin = PenLineJoin.Round }, geometry);
                    drawingContext.DrawGeometry(null, new Pen(new SolidColorBrush(Colors.White) { Opacity = 1.0 }, 2) { LineJoin = PenLineJoin.Round }, geometry);
                }
            }

            drawingContext.Pop();
        }

        private void DrawLine(DrawingContext drawingContext, Color primary, Point pFrom, Point pTo)
        {
            drawingContext.DrawLine(new Pen(new SolidColorBrush(primary) { Opacity = 0.5 }, 4) { LineJoin = PenLineJoin.Round }, pFrom, pTo);
            drawingContext.DrawLine(new Pen(new SolidColorBrush(Colors.White) { Opacity = 1.0 }, 2) { LineJoin = PenLineJoin.Round }, pFrom, pTo);
        }

        private void AppendSwiggely(PathFigure figure, Point start)
        {
            const double width = 4;
            figure.Segments.Add(new LineSegment(start, true));
            figure.Segments.Add(new LineSegment(Add(start, 1 * width, 8), true));
            figure.Segments.Add(new LineSegment(Add(start, 2 * width, -10), true));
            figure.Segments.Add(new LineSegment(Add(start, 3 * width, 15), true));
            figure.Segments.Add(new LineSegment(Add(start, 4 * width, -15), true));
            figure.Segments.Add(new LineSegment(Add(start, 5 * width, 10), true));
            figure.Segments.Add(new LineSegment(Add(start, 6 * width, -8), true));
            figure.Segments.Add(new LineSegment(Add(start, 7 * width, 0), true));
        }

        private Point Add(Point point, double x, double y)
        {
            return new Point(point.X + x, point.Y + y);
        }
    }
}
