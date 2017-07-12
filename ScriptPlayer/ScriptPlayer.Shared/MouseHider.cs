using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ScriptPlayer.Shared
{
    public class MouseHider
    {
        private bool _isEnabled;
        private readonly FrameworkElement _element;
        private readonly DispatcherTimer _mouseTimer;

        public MouseHider(FrameworkElement element)
        {
            _element = element;
            _element.MouseMove += ElementOnMouseMove;
            _mouseTimer = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Normal, MouseTimerOnElapsed, Dispatcher.CurrentDispatcher);
        }

        private void ElementOnMouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            ResetTimer();
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value; 
                ResetTimer();
            }
        }

        private void SetMouse(bool visible)
        {
            _element.Cursor = visible ? Cursors.Arrow : Cursors.None;
        }

        private void MouseTimerOnElapsed(object sender, EventArgs eventArgs)
        {
            _mouseTimer.Stop();
            SetMouse(false);
        }

        public void ResetTimer()
        {
            _mouseTimer.Stop();
            SetMouse(true);

            if (IsEnabled)
            {
                _mouseTimer.Start();
            }
        }
    }
}
