using System;
using System.Windows;
using System.Windows.Media;
using ScriptPlayer.Shared.Beats;

namespace ScriptPlayer.Shared.Controls
{
    public class BarBar : TimelineBaseControl
    {
        public static readonly DependencyProperty BarsProperty = DependencyProperty.Register(
            "Bars", typeof(BarCollection), typeof(BarBar), new PropertyMetadata(default(BarCollection)));

        public BarCollection Bars
        {
            get { return (BarCollection) GetValue(BarsProperty); }
            set { SetValue(BarsProperty, value); }
        }

        protected override void Render(TimeBasedRenderContext context)
        {
            context.DrawingContext.DrawRectangle(Background, null, context.FullRect);

            if (Bars != null)
            {
                foreach (Bar bar in Bars)
                {
                    TimeSpan beatDuration = bar.Tact.BeatDuration;
                    TimeSpan start = bar.Tact.Start + beatDuration.Multiply(bar.Start);
                    TimeSpan end = bar.Tact.Start + beatDuration.Multiply(bar.End);

                    if (start > context.TimeTo || end < context.TimeFrom)
                        continue;

                    double startX = XFromTime(start);
                    double endX = XFromTime(end);

                    Rect barRect = new Rect(startX, context.FullRect.Y,endX - startX,context.FullRect.Height);

                    context.DrawingContext.DrawRectangle(Brushes.DarkRed, new Pen(Brushes.Red, 1), barRect);
                    
                    foreach (int tactIndex in bar.Tact.GetBeatIndices(start, end))
                    {
                        int barIndex = (tactIndex - bar.Start) % bar.Rythm.Length;
                        TimeSpan beatPosition = bar.Tact.Start + bar.Tact.BeatDuration.Multiply(tactIndex);
                        double x = XFromTime(beatPosition);

                        Pen pen;
                        if (bar.Rythm[barIndex])
                        {
                            pen = new Pen(Brushes.White, 3);
                        }
                        else
                        {
                            pen = new Pen(Brushes.White, 1);
                        }

                        context.DrawingContext.DrawLine(pen, new Point(x,context.FullRect.Top), new Point(x, context.FullRect.Bottom));
                    }
                }
            }
        }
    }
}
