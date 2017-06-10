using System;
using System.Diagnostics;
using System.Windows.Threading;

namespace ScriptPlayer.Shared
{
    public class Metronome
    {
        public delegate void TickEventHandler(object sender, byte position, byte speed);

        public double MaxSpeed { get; set; }
        public double BeatsPerMinute { get; set; }
        public byte PositionFrom { get; set; }
        public byte PositionTo { get; set; }

        public event TickEventHandler Tick;

        private DispatcherTimer _timer;

        private bool up = false;

        public Metronome()
        {
            _timer = new DispatcherTimer(DispatcherPriority.Normal);
            _timer.Tick += TimerOnElapsed;
        }

        private void TimerOnElapsed(object sender, EventArgs eventArgs)
        {
            double delta = Math.Abs(PositionTo - PositionFrom);

            byte actualSpeed2 = SpeedPredictor.Predict2((byte) delta, _timer.Interval);
            byte actualSpeed = SpeedPredictor.Predict((byte)delta, _timer.Interval);

            Debug.WriteLine("PredictedSpeed: " + actualSpeed + " / " + actualSpeed2);

            OnTick(up ? PositionTo : PositionFrom, actualSpeed);
            up ^= true;
        }

        protected virtual void OnTick(byte position, byte speed)
        {
            Tick?.Invoke(this, position, speed);
        }

        public void Refresh()
        {
            double seconds = 60.0 / BeatsPerMinute / 2.0;
            _timer.Interval = TimeSpan.FromSeconds(seconds);
            _timer.Stop();
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }
    }
}
