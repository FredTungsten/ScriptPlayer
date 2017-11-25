using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptPlayer.Shared.Scripts
{
    public class ScriptHandler
    {
        public event EventHandler<PositionCollection> PositionsChanged;
        private int _lastIndex;
        private TimeSpan _lastTimestamp;

        private List<TimeSpan> _beats;
        private List<ScriptAction> _originalActions;
        private List<ScriptAction> _filledActions;

        private TimeSource _timesource;
        private ConversionMode _conversionMode = ConversionMode.UpOrDown;
        private static TimeSpan _delay = TimeSpan.Zero;
        
        private bool _fillGaps;
        private bool _fillFirstGap;
        private bool _fillLastGap;
        private TimeSpan _duration;
        public event EventHandler<ScriptActionEventArgs> ScriptActionRaised;
        public event EventHandler<IntermediateScriptActionEventArgs> IntermediateScriptActionRaised;

        public TimeSpan Delay { get; set; }

        public ConversionMode ConversionMode  
        {
            get => _conversionMode;
            set
            {
                _conversionMode = value;
                ProcessScript();
            }
        }

        public bool FillGaps
        {
            get => _fillGaps;
            set
            {
                _fillGaps = value;
                ProcessScript();
            }
        }

        public bool FillFirstGap
        {
            get => _fillFirstGap;
            set
            {
                _fillFirstGap = value;
                ProcessScript();
            }
        }

        public bool FillLastGap
        {
            get => _fillLastGap;
            set
            {
                _fillLastGap = value; 
                ProcessScript();
            }
        }

        public TimeSpan Duration
        {
            get { return _duration; }
            set
            {
                _duration = value; 
                ProcessScript();
            }
        }

        public void Clear()
        {
            _originalActions?.Clear();
            _beats?.Clear();
            ProcessScript();
        }

        private void SaveBeatFile()
        {
            if (_originalActions.FirstOrDefault() is BeatScriptAction)
            {
                _beats = _originalActions.Select(a => a.TimeStamp).ToList();
            }
            else
            {
                _beats = null;
            }
        }

        private void ConvertBeatFile()
        {
            if (_beats == null)
                return;

            _originalActions = BeatsToFunScriptConverter.Convert(_beats, _conversionMode)
                .OrderBy(a => a.TimeStamp)
                .Cast<ScriptAction>()
                .ToList();

            ProcessScript();
        }

        private void UpdatePositions()
        {
            PositionCollection collection = new PositionCollection(_filledActions.OfType<FunScriptAction>().Select(f => new TimedPosition
            {
                Position = f.Position,
                TimeStamp = f.TimeStamp
            }));

            OnPositionsChanged(collection);
        }

        public ScriptHandler()
        {
            _originalActions = new List<ScriptAction>();
            _filledActions = new List<ScriptAction>();
            Delay = new TimeSpan(0);
        }

        protected virtual void OnScriptActionRaised(ScriptActionEventArgs e)
        {
            ScriptActionRaised?.Invoke(this, e);
        }

        protected virtual void OnIntermediateScriptActionRaised(IntermediateScriptActionEventArgs e)
        {
            IntermediateScriptActionRaised?.Invoke(this, e);
        }

        public IEnumerable<ScriptAction> GetScript()
        {
            if (_filledActions == null)
                return new List<ScriptAction>();

            return _filledActions.AsReadOnly();
        }

        public IEnumerable<ScriptAction> GetUnfilledScript()
        {
            if (_originalActions == null)
                return new List<ScriptAction>();

            return _originalActions.AsReadOnly();
        } 

        public void SetScript(IEnumerable<ScriptAction> script)
        {
            List<ScriptAction> actions = new List<ScriptAction>(script);
            actions.Sort((a, b) => a.TimeStamp.CompareTo(b.TimeStamp));

            _originalActions = actions;
            
            SaveBeatFile();
            ConvertBeatFile();
            ProcessScript();
        }

        private void ProcessScript()
        {
            FillScriptGaps();
            UpdatePositions();
            ResetCache();
        }

        private void FillScriptGaps()
        {
            foreach (ScriptAction action in _originalActions)
                action.OriginalAction = true;

            _filledActions = new List<ScriptAction>(_originalActions);

            if (!FillGaps) return;

            List<ScriptAction> additionalActions = new List<ScriptAction>();

            TimeSpan previous = TimeSpan.MinValue;
            foreach (ScriptAction action in _originalActions)
            {
                if (action == _originalActions.First())
                {
                    if (FillFirstGap)
                    {
                        if (action.TimeStamp > MinGapDuration)
                        {
                            additionalActions.AddRange(GenerateGapFiller(TimeSpan.Zero, action.TimeStamp.Subtract(GapBuffer)));
                        }
                    }
                }
                else if (action == _originalActions.Last())
                {
                    if (FillLastGap)
                    {
                        if (Duration - action.TimeStamp > MinGapDuration)
                        {
                            additionalActions.AddRange(GenerateGapFiller(action.TimeStamp.Add(GapBuffer), Duration));
                        }
                    }
                }
                else
                {
                    TimeSpan duration = action.TimeStamp - previous;
                    if (duration > MinGapDuration)
                    {
                        additionalActions.AddRange(GenerateGapFiller(previous.Add(GapBuffer), action.TimeStamp.Subtract(GapBuffer)));
                    }
                }

                previous = action.TimeStamp;
            }

            _filledActions.AddRange(additionalActions);
            _filledActions.Sort((a,b) => a.TimeStamp.CompareTo(b.TimeStamp));
        }

        private static IEnumerable<ScriptAction> GenerateGapFiller(TimeSpan start, TimeSpan end)
        {
            List<FunScriptAction> additionalActions = new List<FunScriptAction>();
            TimeSpan gapduration = end - start;

            int fillers = (int)Math.Round(gapduration.Divide(TimeSpan.FromMilliseconds(500)));

            bool up = true;

            for (int i = 0; i <= fillers; i++)
            {
                up ^= true;
                additionalActions.Add(new FunScriptAction
                {
                    Position = (byte)(up ? 99 : 0),
                    TimeStamp = start + gapduration.Multiply(i).Divide(fillers),
                    OriginalAction = false
                });
            }

            return additionalActions;
        }

        public TimeSpan MinGapDuration { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan GapBuffer { get; set; } = TimeSpan.FromSeconds(2);

        public void SetTimesource(TimeSource timesource)
        {
            if (_timesource != null)
                _timesource.ProgressChanged -= TimesourceOnProgressChanged;

            _timesource = timesource;

            if (_timesource != null)
                _timesource.ProgressChanged += TimesourceOnProgressChanged;
        }

        private void TimesourceOnProgressChanged(object sender, TimeSpan timeSpan)
        {
            CheckLastActionAfter(timeSpan - _delay);
        }

        private void ResetCache()
        {
            _lastIndex = -1;
            _lastTimestamp = new TimeSpan(0);
        }

        private void CheckLastActionAfter(TimeSpan newTimeSpan)
        {
            if (newTimeSpan < _lastTimestamp)
                ResetCache();

            int passedIndex = -1;

            if (_filledActions == null) return;

            for (int i = _lastIndex + 1; i < _filledActions.Count; i++)
            {
                if (GetFilledTimestamp(i) > newTimeSpan)
                    break;

                if (GetFilledTimestamp(i) <= newTimeSpan)
                    passedIndex = i;
            }

            if (passedIndex >= 0)
            {
                _lastIndex = passedIndex;
                _lastTimestamp = GetFilledTimestamp(passedIndex);

                ScriptActionEventArgs args = new ScriptActionEventArgs(_filledActions[passedIndex]);

                if (passedIndex + 1 < _filledActions.Count)
                    args.RawNextAction = _filledActions[passedIndex + 1];

                if (passedIndex > 0)
                    args.RawPreviousAction = _filledActions[passedIndex - 1];

                OnScriptActionRaised(args);
            }
            else if (_lastIndex >= 0 && _lastIndex < _filledActions.Count - 1)
            {
                TimeSpan previous = GetFilledTimestamp(_lastIndex);
                TimeSpan next = GetFilledTimestamp(_lastIndex + 1);

                double progress = (newTimeSpan - previous).Divide(next - previous);

                if (progress >= 0.0 && progress <= 1.0)
                {
                    IntermediateScriptActionEventArgs args = new IntermediateScriptActionEventArgs(_filledActions[_lastIndex], _filledActions[_lastIndex + 1], progress);
                    OnIntermediateScriptActionRaised(args);
                }
            }
        }

        private TimeSpan GetFilledTimestamp(int index)
        {
            return _filledActions[index].TimeStamp + Delay;
        }

        public static void SetDelay(TimeSpan delay)
        {
            _delay = delay;
        }

        public ScriptAction FirstOriginalEventAfter(TimeSpan currentPosition)
        {
            return _originalActions?.FirstOrDefault(a => a.TimeStamp > currentPosition);
        }

        public ScriptAction FirstEventAfter(TimeSpan currentPosition)
        {
            return _filledActions?.FirstOrDefault(a => a.TimeStamp > currentPosition);
        }

        public TimeSpan GetOriginalScriptDuration()
        {
            if (_originalActions == null)
                return TimeSpan.Zero;

            return _originalActions.Count == 0 ? TimeSpan.Zero : _originalActions.Max(a => a.TimeStamp);
        }

        protected virtual void OnPositionsChanged(PositionCollection e)
        {
            PositionsChanged?.Invoke(this, e);
        }
    }
}
