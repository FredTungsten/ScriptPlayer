using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public class RangePreview : Control
    {
        public static readonly DependencyProperty RangesProperty = DependencyProperty.Register(
            "Ranges", typeof(List<Range>), typeof(RangePreview), new FrameworkPropertyMetadata(default(List<Range>), FrameworkPropertyMetadataOptions.AffectsRender));

        public List<Range> Ranges
        {
            get { return (List<Range>)GetValue(RangesProperty); }
            set { SetValue(RangesProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Brushes.Black, null, new Rect(0, 0, ActualWidth, ActualHeight));

            if (Ranges == null)
                return;

            if (Ranges.Count < 2)
                return;

            List<double> min = new List<double>();
            List<double> max = new List<double>();
            List<double> toggle = new List<double>();

            for (int i = 0; i < Ranges.Count; i++)
            {
                min.Add(Ranges[i].Min);
                max.Add(Ranges[i].Max);
                if(i%3 == 0)
                    toggle.Add((i/3 % 2 == 0) ? Ranges[i].Min : Ranges[i].Max);
            }

            PathGeometry geometryMin = GenerateLine(min, ActualWidth, ActualHeight);
            PathGeometry geometryMax = GenerateLine(max, ActualWidth, ActualHeight);
            PathGeometry geometryToggle = GenerateLine(toggle, ActualWidth, ActualHeight);

            drawingContext.DrawGeometry(null, new Pen(Brushes.Cyan, 1) { EndLineCap = PenLineCap.Flat, StartLineCap = PenLineCap.Flat, LineJoin = PenLineJoin.Miter, MiterLimit = 0}, geometryToggle);
            drawingContext.DrawGeometry(null, new Pen(Brushes.Red, 3) { EndLineCap = PenLineCap.Flat, StartLineCap = PenLineCap.Flat, LineJoin = PenLineJoin.Miter, MiterLimit = 0 }, geometryMin);
            drawingContext.DrawGeometry(null, new Pen(Brushes.Lime, 3) { EndLineCap = PenLineCap.Flat, StartLineCap = PenLineCap.Flat, LineJoin = PenLineJoin.Miter, MiterLimit = 0 }, geometryMax);
        }

        private PathGeometry GenerateLine(List<double> values, double w, double h)
        {
            double segmentWidth = w / (values.Count - 1);
            var points = values.Select((val, index) => new Point(index * segmentWidth, h * (1.0 - val))).ToList();

            PathFigure figure = new PathFigure(points[0], points.Skip(1).Select(p => new LineSegment(p, true)), false);
            PathGeometry result = new PathGeometry(new[] { figure });
            return result;
        }
    }

    public class Range
    {
        public double Min { get; set; }
        public double Max { get; set; }
    }
}
