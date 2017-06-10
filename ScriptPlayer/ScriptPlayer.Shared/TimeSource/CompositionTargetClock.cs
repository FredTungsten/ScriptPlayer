using System;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public class CompositionTargetClock : ISampleClock
    {
        public CompositionTargetClock()
        {
            CompositionTarget.Rendering += CompositionTargetOnRendering;
        }

        private void CompositionTargetOnRendering(object sender, EventArgs e)
        {
            Tick?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler Tick;
    }
}