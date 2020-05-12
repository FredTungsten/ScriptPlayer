using System;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace ScriptPlayer.Shared.Devices
{
    public class EStimAudioDevice : Device
    {
        private readonly SineWaveProvider _generator;
        private readonly DirectSoundOut _soundOut;
        private MonoToStereoSampleProvider _stereo;
        private EstimParameters _parameters;

        public EStimAudioDevice(DirectSoundDeviceInfo device, EstimParameters parameters)
        {
            Name = device.Description;

            _parameters = parameters;

            _generator = new SineWaveProvider();
            _generator.Frequency = 600;

            _stereo = new MonoToStereoSampleProvider(_generator);
            _stereo.LeftVolume = 0f;
            _stereo.RightVolume = 0f;
            
            _soundOut = new DirectSoundOut(device.Guid);
            _soundOut.Init(_stereo);
            _soundOut.Play();

            MinDelayBetweenCommands = TimeSpan.Zero;
        }

        public override void SetMinCommandDelay(TimeSpan settingsCommandDelay)
        {
        }

        public override async Task Set(DeviceCommandInformation information)
        {
            
        }

        public override async Task Set(IntermediateCommandInformation information)
        {
            double position = (information.DeviceInformation.PositionFromOriginal / 99.0) * (1.0 - information.Progress) +
                              (information.DeviceInformation.PositionToOriginal / 99.0) * information.Progress;

            double freqMin = 400;
            double freqMax = 4000;

            switch (_parameters.ConversionMode)
            {
                case EstimConversionMode.Volume:
                    _stereo.LeftVolume = (float)position;
                    _stereo.RightVolume = (float)position;
                    break;
                case EstimConversionMode.Balance:
                    _stereo.LeftVolume = (float)position;
                    _stereo.RightVolume = (float)(1.0 - position);
                    break;
                case EstimConversionMode.Frequency:
                    _stereo.LeftVolume = 1f;
                    _stereo.RightVolume = 1f;
                    _generator.Frequency = freqMin + (freqMax - freqMin) * position;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        protected override void StopInternal()
        {
            _stereo.LeftVolume = 0f;
            _stereo.RightVolume = 0f;
        }

        public override void Dispose()
        {
            _soundOut.Dispose();
            base.Dispose();
        }
    }

    public enum EstimConversionMode
    {
        Volume,
        Balance,
        Frequency
    }

    public class EstimParameters
    {
        public EstimConversionMode ConversionMode { get; set; }
    }
}
