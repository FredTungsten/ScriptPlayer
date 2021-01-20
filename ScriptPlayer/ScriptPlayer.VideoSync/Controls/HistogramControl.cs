using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScriptPlayer.VideoSync.Controls
{
    public class HistogramControl : Control
    {
        public static readonly DependencyProperty EntriesProperty = DependencyProperty.Register(
            "Entries", typeof(List<HistogramEntry>), typeof(HistogramControl), new FrameworkPropertyMetadata(default(List<HistogramEntry>), FrameworkPropertyMetadataOptions.AffectsRender));

        private int _previousHoverIndex = -1;

        public List<HistogramEntry> Entries
        {
            get { return (List<HistogramEntry>) GetValue(EntriesProperty); }
            set { SetValue(EntriesProperty, value); }
        }

        private Point HoverPos { get; set; }

        private Thickness _padding = new Thickness(10, 10, 10, 10);

        protected override void OnRender(DrawingContext dc)
        {
            if (ActualWidth == 0 || ActualHeight == 0)
                return;

            var rectAll = new Rect(0, 0, ActualWidth, ActualHeight);

            dc.DrawRectangle(Background, null, rectAll);
            
            var rectInner = new Rect(_padding.Left, _padding.Top, rectAll.Width - _padding.Left - _padding.Right, rectAll.Height - _padding.Top - _padding.Bottom);

            Brush diagramBackground = Brushes.White;
            Pen diagramBorder = new Pen(Brushes.Black, 1);

            Brush barBackground = Brushes.Lime;
            Pen barBorder = new Pen(Brushes.Black, 1);

            dc.DrawRectangle(diagramBackground, diagramBorder, rectInner);

            if (Entries == null || Entries.Count == 0)
                return;

            double entryWidth = rectInner.Width / Entries.Count;

            if (double.IsNaN(entryWidth) || double.IsInfinity(entryWidth))
                return;

            double entryValueScale = rectInner.Height / Entries.Max(e => e.Value);

            if (double.IsNaN(entryValueScale) || double.IsInfinity(entryValueScale))
                return;
            
            for (int index = 0; index < Entries.Count; index++)
            {
                HistogramEntry entry = Entries[index];
                double x = rectInner.X + entryWidth * index;
                double height = entry.Value * entryValueScale;
                double y = rectInner.Height - height;
                
                Rect bar = new Rect(x,y, entryWidth, height);
                dc.DrawRectangle(barBackground, barBorder, bar);
            }

            if (IsMouseOver && _previousHoverIndex < Entries.Count)
            {
                var entry = Entries[_previousHoverIndex];

                string tip = $"{entry.Label}\r\n{entry.Description}\r\n{entry.Value}";

                var typeFace = new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch);

                FormattedText text = new FormattedText(tip, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    typeFace, this.FontSize, Brushes.Black, new NumberSubstitution(), 96);

                Rect tipRect = new Rect(HoverPos, new Size(text.Width + 10, text.Height + 10));

                dc.DrawRectangle(Brushes.LightYellow, new Pen(Brushes.Black, 1), tipRect );
                dc.DrawText(text, HoverPos + new Vector(5,5));
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            HoverPos = e.GetPosition(this);

            if (Entries == null || Entries.Count == 0)
                return;

            double entryWidth = (this.ActualWidth - _padding.Left - _padding.Right) / Entries.Count;
            int hoverIndex = (int) ((HoverPos.X - _padding.Left) / entryWidth);

            hoverIndex = Math.Min(Entries.Count - 1, Math.Max(0, hoverIndex));

            if (hoverIndex != _previousHoverIndex)
            {
                _previousHoverIndex = hoverIndex;
                InvalidateVisual();
            }
        }
    }

    public class HistogramEntry
    {
        public int Value { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
    }
}
