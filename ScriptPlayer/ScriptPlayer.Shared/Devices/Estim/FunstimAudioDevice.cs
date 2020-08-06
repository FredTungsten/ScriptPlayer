using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;

namespace ScriptPlayer.Shared.Estim
{
    public class FunstimAudioDevice : Device
    {
        private readonly DirectSoundOut _soundOut;
        private readonly FunstimSampleProvider _provider;

        public FunstimAudioDevice(DirectSoundDeviceInfo device, FunstimParameters parameters)
        {
            Name = "Funstim - " + device.Description;

            _soundOut = new DirectSoundOut(device.Guid);

            try
            {
                List<int> frequencies = Array.ConvertAll(parameters.Frequencies.Split(','), int.Parse).Where(x => x != 0).ToList();

                _provider = new FunstimSampleProvider(frequencies, (int)parameters.FadeMs.TotalMilliseconds, parameters.FadeOnPause);

                _soundOut.Init(_provider);
                _soundOut.Play();

                MinDelayBetweenCommands = TimeSpan.Zero;
            }
            catch (FormatException)
            {

            }
        }

        protected override bool CommandsAreSimilar(DeviceCommandInformation command1, DeviceCommandInformation command2)
        {
            return false;
        }

        public override void SetMinCommandDelay(TimeSpan settingsCommandDelay)
        {

        }

        protected override Task Set(DeviceCommandInformation information)
        {
            _provider?.Action(information.PositionFromTransformed, information.PositionToTransformed, (int)information.Duration.TotalMilliseconds, information.SpeedMultiplier);
            return Task.CompletedTask;
        }

        public override Task Set(IntermediateCommandInformation information)
        {
            return Task.CompletedTask;
        }

        protected override void StopInternal()
        {

        }

        public override void Dispose()
        {
            _soundOut.Dispose();
            base.Dispose();
        }
    }

    public class FunstimParameters
    {
        public string Frequencies { get; set; }
        public TimeSpan FadeMs { get; set; }
        public bool FadeOnPause { get; set; }
    }
}
