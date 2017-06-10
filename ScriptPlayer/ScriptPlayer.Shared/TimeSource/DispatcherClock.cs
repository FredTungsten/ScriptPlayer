using System;
using System.Windows.Threading;

namespace ScriptPlayer.Shared
{
    public class DispatcherClock : ISampleClock, IDisposable
    {
        private DispatcherTimer _timer;

        public DispatcherClock(Dispatcher dispatcher, TimeSpan intervall)
        {
            _timer = new DispatcherTimer(intervall, DispatcherPriority.Normal, Timer_Tick, dispatcher);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Tick?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler Tick;

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
        }
    }
}