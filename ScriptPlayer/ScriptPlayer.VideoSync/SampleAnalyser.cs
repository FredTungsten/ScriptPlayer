using ScriptPlayer.Shared;

namespace ScriptPlayer.VideoSync
{
    public class SampleAnalyser
    {
        private readonly SampleCondition _condition;
        private readonly AnalysisParameters _parameters;

        bool _beatActive;
        bool _previousSamplePositive;

        int _positives;
        int _negatives;
        int _framesSinceLastBeat;

        public SampleAnalyser(SampleCondition condition, AnalysisParameters parameters)
        {
            _condition = condition;
            _parameters = parameters;
        }

        public bool AddSample(byte[] rgbPixels)
        {
            bool samplePositive = _condition.CheckSample(rgbPixels);

            if (samplePositive != _previousSamplePositive)
            {
                _positives = 0;
                _negatives = 0;
            }

            _previousSamplePositive = samplePositive;

            if (samplePositive)
                _positives++;
            else
                _negatives++;

            if (_positives > _parameters.MaxPositiveSamples)
            {
                _positives = 0;
                _negatives = 0;
                _beatActive = false;
            }

            _framesSinceLastBeat++;

            if (_framesSinceLastBeat < _parameters.MinBetweenBeats)
            {
                _positives = 0;
                _negatives = 0;
            }

            if (!_beatActive && _positives >= _parameters.MinPositiveSamples)
            {
                _beatActive = true;
                _framesSinceLastBeat = 0;
                return true;
            }

            if (_beatActive && _negatives >= _parameters.MinNegativeSamples)
            {
                _beatActive = false;
            }

            return false;
        }
    }
}