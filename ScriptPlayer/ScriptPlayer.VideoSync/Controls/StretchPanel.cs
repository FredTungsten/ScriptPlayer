using System;
using System.Windows;
using System.Windows.Controls;

namespace ScriptPlayer.VideoSync.Controls
{
    public class StretchPanel : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            double width = 0;
            double height = 0;

            foreach (UIElement child in Children)
            {
                child.Measure(availableSize);

                width = Math.Max(child.DesiredSize.Width, width);
                height += child.DesiredSize.Height;
            }

            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double y = 0;

            foreach (UIElement child in Children)
            {
                child.Arrange(new Rect(0, y, finalSize.Width, child.DesiredSize.Height));
                y += child.DesiredSize.Height;
            }

            return finalSize;
        }
    }
}
