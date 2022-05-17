using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NAudio.Wave;

class FunstimSampleProvider : ISampleProvider
{
    private readonly List<double> radsPerSample;
    private readonly float fadeSamples;
    private readonly bool fadeOnPause;
    private readonly int sampleRate;

    private readonly Stopwatch timer;

    private long sampleCount;
    private long startSample;

    private float startRamp;
    private float endRamp;
    private float scaleRamp;

    private int startPosition;
    private int endPosition;
    private int durationMs;
    private int numSamples;
    private double speedMultiplier;
    private float volume = 0.0f;

    public FunstimSampleProvider(List<int> frequencies, int fadeMs, int rampPercent, bool fadeOnPause, int sampleRate = 44100)
    {
        this.radsPerSample = frequencies.ConvertAll(f => (f * Math.PI * 2) / sampleRate);
        this.fadeSamples = (fadeMs * sampleRate) / 1000.0f;
        this.sampleRate = sampleRate;
        this.fadeOnPause = fadeOnPause;
        this.scaleRamp = rampPercent/100.0f;

        numSamples = 1;
        sampleCount = (int)fadeSamples * 10;

        this.timer = new Stopwatch();

        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);
    }

    public void Action(int startPosition, int endPosition, int durationMs, double speedMultiplier, TimeSpan timeStamp, TimeSpan mediaDuration)
    {
        int diff = Math.Abs((int)timer.ElapsedMilliseconds - this.durationMs);

        if (diff > 100 || Math.Abs(startPosition - this.endPosition)>10)
        {
            Debug.WriteLine("diff: " + diff);
            Debug.WriteLine("start: " + startPosition + ", previous end: " + this.endPosition);

            volume = 0.0f;
        }

        this.startPosition = startPosition;
        this.endPosition = endPosition;
        this.durationMs = durationMs;
        this.speedMultiplier = speedMultiplier;

        this.startRamp = (float) (timeStamp.TotalMilliseconds / mediaDuration.TotalMilliseconds) * scaleRamp + (1-scaleRamp);
        this.endRamp = (float) ((timeStamp.TotalMilliseconds + durationMs) / mediaDuration.TotalMilliseconds) * scaleRamp + (1-scaleRamp);

        numSamples = (durationMs * sampleRate) / 1000;

        timer.Restart();

        startSample = sampleCount;
    }

    public WaveFormat WaveFormat { get; private set; }

    public int Read(float[] buffer, int offset, int count)
    {
        for (int i = 0; i < count / 2; i++)
        {
            long sample = sampleCount - startSample;
            double position = (sample * endPosition + (numSamples - sample) * startPosition) / numSamples;
            float rampVolume = (sample * endRamp + (numSamples - sample) * startRamp) / numSamples;

            int filterLength = (int)fadeSamples * 2;

            if (sample > numSamples)
            {
                position = endPosition;
                rampVolume = endRamp;

                if (sample > numSamples + count)
                {
                    // fade out
                    volume = (volume * (filterLength - 1)) / filterLength;
                }
            }
            else
            {
                float targetVolume = 1.0f;

                if (fadeOnPause)
                {
                    if (sample > fadeSamples)
                    {
                        targetVolume = 0.0f;
                    }
                    else
                    {
                        targetVolume = (fadeSamples - sample) / fadeSamples;
                    }
                }

                // fade in (unless fade time is 0)

                if (filterLength > 0)
                {
                    volume = (volume * (filterLength - 1) + targetVolume) / filterLength;
                }
                else
                {
                    volume = targetVolume;
                }
            }

            if (volume > 1.0f)
            {
                volume = 1.0f;
            }

            if (volume < 0.0f)
            {
                volume = 0.0f;
            }

            float left = (float)radsPerSample.Aggregate(0.0, (a, r) => a + Math.Sin(sampleCount * r));
            float right = -(float)radsPerSample.Aggregate(0.0, (a, r) => a + Math.Sin(sampleCount * r + ((position * speedMultiplier) / 99.0) * Math.PI));

            buffer[offset + i * 2] = (rampVolume * volume * left * 0.9f) / radsPerSample.Count;
            buffer[offset + i * 2 + 1] = (rampVolume * volume * right * 0.9f) / radsPerSample.Count;

            sampleCount++;
        }

        return count;
    }
}
