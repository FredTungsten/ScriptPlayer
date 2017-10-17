using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ScriptPlayer.Shared;

namespace ScriptPlayer.VideoSync.Controls
{
    public class DirectInputControl : Control
    {
        public event EventHandler Beat;
        public event EventHandler<byte> Position;

        protected virtual void OnBeat()
        {
            Beat?.Invoke(this, EventArgs.Empty);
        }

        static DirectInputControl()
        {
            FocusableProperty.OverrideMetadata(typeof(DirectInputControl), new FrameworkPropertyMetadata(true));
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            OnBeat();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            bool handled = true;

            switch (e.Key)
            {
                case Key.NumPad1:
                {
                    OnPosition(0);
                    break;
                }
                case Key.NumPad2:
                {
                    OnPosition(50);
                    break;
                }
                case Key.NumPad3:
                {
                    OnPosition(99);
                    break;
                }
                default:
                    handled = false;
                    break;
            }

            if (handled)
                e.Handled = true;
        }

        protected virtual void OnPosition(byte e)
        {
            Position?.Invoke(this, e);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Brushes.Black, null, new Rect(new Point(0,0), new Size(ActualWidth, ActualHeight)));
        }
    }
}
