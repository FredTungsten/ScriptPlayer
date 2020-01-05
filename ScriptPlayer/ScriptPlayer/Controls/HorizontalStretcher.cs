using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScriptPlayer.Controls
{
    public class HorizontalStretcher : Control
    {
        public static readonly DependencyProperty StretchedParentProperty = DependencyProperty.Register(
            "StretchedParent", typeof(FrameworkElement), typeof(HorizontalStretcher), new PropertyMetadata(default(FrameworkElement)));

        public FrameworkElement StretchedParent
        {
            get => (FrameworkElement) GetValue(StretchedParentProperty);
            set => SetValue(StretchedParentProperty, value);
        }

        private FrameworkElement GetParent()
        {
            return StretchedParent ?? (FrameworkElement) Parent;
        }

        static HorizontalStretcher()
        {
            WidthProperty.OverrideMetadata(typeof(HorizontalStretcher), new FrameworkPropertyMetadata(3.0));
            BackgroundProperty.OverrideMetadata(typeof(HorizontalStretcher), new FrameworkPropertyMetadata(Brushes.LightGray));
            HorizontalAlignmentProperty.OverrideMetadata(typeof(HorizontalStretcher), new FrameworkPropertyMetadata(HorizontalAlignment.Right));
            VerticalAlignmentProperty.OverrideMetadata(typeof(HorizontalStretcher), new FrameworkPropertyMetadata(VerticalAlignment.Stretch));
            CursorProperty.OverrideMetadata(typeof(HorizontalStretcher), new FrameworkPropertyMetadata(Cursors.SizeWE));
        }

        public event EventHandler FinishedDragging;

        private bool _down;
        private Point _downPos;
        private double _downWidth;

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            _down = true;
            _downPos = e.GetPosition(Window.GetWindow(this));
            _downWidth = GetParent().ActualWidth;

            CaptureMouse();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (!_down)
                return;

            var parent = GetParent();

            Point pos = e.GetPosition(Window.GetWindow(this));

            double diff = pos.X - _downPos.X;

            double minWidth = Width;
            double maxWidth = parent.MaxWidth;
            double newWidth = _downWidth;

            switch (HorizontalAlignment)
            {
                case HorizontalAlignment.Right:
                     newWidth = _downWidth + diff;
                    break;
                case HorizontalAlignment.Left:
                    newWidth = _downWidth - diff;
                    break;
            }

            newWidth = (int)Math.Min(maxWidth, Math.Max(minWidth, newWidth));

            parent.Width = newWidth;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if(!_down)
                return;

            _down = false;

            ReleaseMouseCapture();
            OnFinishedDragging();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Background, null, new Rect(0,0, ActualWidth, ActualHeight));
        }

        protected virtual void OnFinishedDragging()
        {
            FinishedDragging?.Invoke(this, EventArgs.Empty);
        }
    }
}
