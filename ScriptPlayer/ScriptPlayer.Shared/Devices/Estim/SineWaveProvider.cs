using System;
using NAudio.Wave;

/// <summary>
/// https://markheath.net/post/naudio-sine-portamento
/// </summary>
class SineWaveProvider : ISampleProvider
{
    private float[] waveTable;
    private double phase;
    private double currentPhaseStep;
    private double targetPhaseStep;
    private double frequency;
    private double phaseStepDelta;
    private bool seekFreq;

    public SineWaveProvider(int sampleRate = 44100)
    {
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
        waveTable = new float[sampleRate];
        for (int index = 0; index < sampleRate; ++index)
            waveTable[index] = (float)Math.Sin(2 * Math.PI * (double)index / sampleRate);
        // For sawtooth instead of sine: waveTable[index] = (float)index / sampleRate;
        Frequency = 1000f;
        Volume = 0.25f;
        PortamentoTime = 0.2; // thought this was in seconds, but glide seems to take a bit longer
    }

    public double PortamentoTime { get; set; }

    public double Frequency
    {
        get
        {
            return frequency;
        }
        set
        {
            frequency = value;
            seekFreq = true;
        }
    }

    public float Volume { get; set; }

    public WaveFormat WaveFormat { get; private set; }

    public int Read(float[] buffer, int offset, int count)
    {
        if (seekFreq) // process frequency change only once per call to Read
        {
            targetPhaseStep = waveTable.Length * (frequency / WaveFormat.SampleRate);

            phaseStepDelta = (targetPhaseStep - currentPhaseStep) / (WaveFormat.SampleRate * PortamentoTime);
            seekFreq = false;
        }
        var vol = Volume; // process volume change only once per call to Read
        for (int n = 0; n < count; ++n)
        {
            int waveTableIndex = (int)phase % waveTable.Length;
            buffer[n + offset] = this.waveTable[waveTableIndex] * vol;
            phase += currentPhaseStep;
            if (this.phase > (double)this.waveTable.Length)
                this.phase -= (double)this.waveTable.Length;
            if (currentPhaseStep != targetPhaseStep)
            {
                currentPhaseStep += phaseStepDelta;
                if (phaseStepDelta > 0.0 && currentPhaseStep > targetPhaseStep)
                    currentPhaseStep = targetPhaseStep;
                else if (phaseStepDelta < 0.0 && currentPhaseStep < targetPhaseStep)
                    currentPhaseStep = targetPhaseStep;
            }
        }
        return count;
    }
}