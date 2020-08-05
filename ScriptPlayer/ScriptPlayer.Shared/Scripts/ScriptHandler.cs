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
        private int _lastIntermediateIndex;

        private List<ScriptAction> _originalScript;
        private List<FunScriptAction> _originalActions;
        private List<FunScriptAction> _filledActions;

        private TimeSource _timesource;
        private ConversionMode _conversionMode = ConversionMode.UpOrDown;
        private static TimeSpan _delay = TimeSpan.Zero;

        private bool _fillGaps;
        private bool _fillFirstGap;
        private bool _fillLastGap;
        private TimeSpan _duration;
        private TimeSpan _fillGapGap;
        private TimeSpan _fillGapIntervall;
        private TimeSpan _minGapDuration;
        private RepeatablePattern _fillGapPattern;
        private int _rangeExtender;
        private bool _loopScriptToDuration = true;
        private bool _squashScriptGaps;
        private bool _isFallback;
        public event EventHandler<ScriptActionEventArgs> ScriptActionRaised;
        public event EventHandler<IntermediateScriptActionEventArgs> IntermediateScriptActionRaised;
        public event EventHandler<IntermediateScriptActionEventArgs> InstantUpdateRaised;

        public TimeSpan MinIntermediateCommandDuration { get; set; } = TimeSpan.FromMilliseconds(50);

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

        public TimeSpan FillGapIntervall
        {
            get => _fillGapIntervall;
            set
            {
                if (value >= TimeSpan.FromMilliseconds(50))
                    _fillGapIntervall = value;
                else
                    _fillGapIntervall = TimeSpan.FromMilliseconds(50);

                ProcessScript();
            }
        }

        public TimeSpan FillGapGap
        {
            get => _fillGapGap;
            set
            {
                _fillGapGap = value;
                ProcessScript();
            }
        }

        public TimeSpan MinGapDuration
        {
            get => _minGapDuration;
            set
            {
                _minGapDuration = value;
                ProcessScript();
            }
        }

        public TimeSpan Duration
        {
            get => _duration;
            set
            {
                _duration = value;
                ProcessScript();
            }
        }

        public bool LoopScriptToDuration
        {
            get => _loopScriptToDuration;
            set
            {
                _loopScriptToDuration = value;
                ProcessScript();
            } 
        }

        public bool SquashScriptGaps
        {
            get => _squashScriptGaps;
            set
            {
                _squashScriptGaps = value; 
                ProcessScript();
            }
        }

        public RepeatablePattern FillGapPattern
        {
            get => _fillGapPattern;
            set
            {
                _fillGapPattern = value;
                ProcessScript();
            }
        }

        public int RangeExtender
        {
            get => _rangeExtender;
            set
            {
                _rangeExtender = value;
                ProcessScript();
            }
        }



        public void Clear()
        {
            _originalActions?.Clear();
            _originalScript?.Clear();
            ProcessScript();
        }

        private void UpdatePositions()
        {
            PositionCollection collection = new PositionCollection(_filledActions.Select(f => new TimedPosition
            {
                Position = f.Position,
                TimeStamp = f.TimeStamp
            }));

            OnPositionsChanged(collection);
        }

        public ScriptHandler()
        {
            _originalActions = new List<FunScriptAction>();
            _filledActions = new List<FunScriptAction>();
            _originalScript = new List<ScriptAction>();

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

        public IEnumerable<FunScriptAction> GetScript()
        {
            if (_filledActions == null)
                return new List<FunScriptAction>();

            return _filledActions.AsReadOnly();
        }

        public IEnumerable<FunScriptAction> GetUnfilledScript()
        {
            if (_originalActions == null)
                return new List<FunScriptAction>();

            return _originalActions.AsReadOnly();
        }

        public void SetScript(IEnumerable<ScriptAction> script, bool isFallback)
        {
            _isFallback = isFallback;

            List<ScriptAction> actions = new List<ScriptAction>(script);
            actions.Sort((a, b) => a.TimeStamp.CompareTo(b.TimeStamp));
            _originalScript = actions;

            ProcessScript();
        }

        /// <summary>
        /// Timespan will be ADDED to the original
        /// </summary>
        /// <param name="shiftBy"></param>
        public void ShiftScript(TimeSpan shiftBy)
        {
            foreach (ScriptAction action in _originalScript)
            {
                action.TimeStamp += shiftBy;
            }

            ProcessScript();
        }

        public void TrimScript(TimeSpan excludeBefore, TimeSpan excludeAfter)
        {
            _originalScript = _originalScript.Where(a => a.TimeStamp >= excludeBefore && a.TimeStamp <= excludeAfter).ToList();

            ProcessScript();
        }

        public void SaveAs(string filename)
        {
            var sourceActions = _filledActions;

            FunScriptFile file = new FunScriptFile
            {
                Inverted = false,
                Actions = sourceActions,
                Range = sourceActions.Max(o => o.Position) - sourceActions.Min(o => o.Position)
            };

            file.Save(filename);
        }

        private void ConvertScript()
        {
            if (_originalScript.FirstOrDefault() is BeatScriptAction)
            {
                _originalActions = BeatsToFunScriptConverter.Convert(_originalScript.Select(s => s.TimeStamp), new ConversionSettings
                {
                    Min = 5,
                    Max = 95,
                    Mode = _conversionMode
                })
                    .OrderBy(a => a.TimeStamp)
                    .ToList();
            }
            else if (_originalScript.FirstOrDefault() is FunScriptAction)
            {
                _originalActions = _originalScript.Cast<FunScriptAction>().ToList();
            }
        }

        private void ProcessScript()
        {
            ConvertScript();
            SquashScript();
            LoopToDuration();
            FillScriptGaps();
            ExtendRange();

            ResetCache();
            UpdatePositions();
        }

        private void SquashScript()
        {
            if (!SquashScriptGaps || !_isFallback)
                return;

            if (_originalActions.Count < 2)
                return;

            if (_originalActions.First().TimeStamp == _originalActions.Last().TimeStamp)
                return;

            TimeSpan previousAction = TimeSpan.Zero;
            TimeSpan shift = TimeSpan.Zero;
            TimeSpan sec = TimeSpan.FromSeconds(1);

            List<FunScriptAction> actions = new List<FunScriptAction>();

            foreach (var action in _originalActions)
            {
                if (actions.Count == 0) //First action
                {
                    if (_originalActions[0].TimeStamp > sec)
                    {
                        shift = _originalActions[0].TimeStamp - sec;
                    }
                }

                var copy = action.Duplicate();
                TimeSpan duration = action.TimeStamp - previousAction;
                previousAction = action.TimeStamp;

                if (duration >= TimeSpan.FromSeconds(10))
                {
                    shift += duration - sec;
                }

                copy.TimeStamp -= shift;
                actions.Add(copy);
            }

            _originalActions = actions;
        }

        private void LoopToDuration()
        {
            if (!LoopScriptToDuration || !_isFallback)
                return;

            if (_originalActions.Count < 2)
                return;

            if (_originalActions.First().TimeStamp == _originalActions.Last().TimeStamp)
                return;

            var copy = _originalActions.ToList();

            while (_originalActions.Last().TimeStamp < Duration)
            {
                TimeSpan firstBeat = _originalActions[1].TimeStamp - _originalActions[0].TimeStamp;
                TimeSpan lastBeat = _originalActions[_originalActions.Count-1].TimeStamp - _originalActions[_originalActions.Count - 2].TimeStamp;
                TimeSpan pause = (firstBeat + lastBeat).Divide(2);

                TimeSpan shift = _originalActions.Last().TimeStamp + pause - _originalActions.First().TimeStamp;

                foreach (var action in copy)
                {
                    var newAction = action.Duplicate();
                    newAction.TimeStamp += shift;
                    _originalActions.Add(newAction);
                    if (newAction.TimeStamp > Duration)
                        break;
                }
            }
        }

        private void ExtendRange()
        {
            if (RangeExtender == 0)
                return;

            if (_filledActions.Count < 3)
                return;

            DateTime start = DateTime.Now;

            List<FunScriptAction> extendedActions = new List<FunScriptAction>();

            int lastExtremeIndex = 0;
            byte lastValue = _filledActions[0].Position;
            byte lastExtremeValue = lastValue;

            byte lowest = lastValue;
            byte highest = lastValue;

            bool? goingUp = null;

            extendedActions.Add(_filledActions.First().Duplicate());

            for (int index = 0; index < _filledActions.Count; index++)
            {
                // Direction unknown
                if (goingUp == null)
                {
                    if (_filledActions[index].Position < lastExtremeValue)
                        goingUp = false;
                    else if (_filledActions[index].Position > lastExtremeValue)
                        goingUp = true;
                }
                else
                {
                    if ((_filledActions[index].Position < lastValue && (bool)goingUp)     //previous was highpoint
                        || (_filledActions[index].Position > lastValue && (bool)!goingUp) //previous was lowpoint
                        || (index == _filledActions.Count - 1))                            //last action
                    {
                        for (int i = lastExtremeIndex + 1; i < index; i++)
                        {
                            FunScriptAction action = _filledActions[i].Duplicate();
                            action.Position = StretchPosition(action.Position, lowest, highest, RangeExtender);
                            extendedActions.Add(action);
                        }

                        lastExtremeValue = _filledActions[index - 1].Position;
                        lastExtremeIndex = index - 1;

                        highest = lastExtremeValue;
                        lowest = lastExtremeValue;

                        goingUp ^= true;
                    }

                    lastValue = _filledActions[index].Position;
                    if (lastValue > highest)
                        highest = lastValue;
                    if (lastValue < lowest)
                        lowest = lastValue;
                }
            }

            extendedActions.Add(_filledActions.Last().Duplicate());

            //
            //Debug.WriteLine($"RangeExtender done in {(DateTime.Now - start).TotalMilliseconds} ms");

            _filledActions = extendedActions;
        }

        private byte StretchPosition(byte position, byte lowest, byte highest, int extension)
        {
            byte newHigh = ClampPosition(highest + extension);
            byte newLow = ClampPosition(lowest - extension);

            double relativePosition = (position - lowest) / (double)(highest - lowest);
            double newposition = relativePosition * (newHigh - newLow) + newLow;

            return ClampPosition(newposition);
        }

        private byte ClampPosition(double position)
        {
            return (byte)Math.Min(99, Math.Max(0, position));
        }

        private void FillScriptGaps()
        {
            foreach (FunScriptAction action in _originalActions)
                action.OriginalAction = true;

            _filledActions = new List<FunScriptAction>(_originalActions);

            if (!FillGaps) return;

            TimeSpan gapgap = FillGapGap < FillGapIntervall ? FillGapIntervall : FillGapGap;

            List<FunScriptAction> additionalActions = new List<FunScriptAction>();

            FunScriptAction previous = null;
            for (int index = 0; index < _originalActions.Count; index++)
            {
                FunScriptAction action = _originalActions[index];

                if (index == 0)
                {
                    if (FillFirstGap)
                    {
                        bool nextIsLow = action.Position < 50;
                        if (action.TimeStamp > MinGapDuration)
                        {
                            additionalActions.AddRange(GenerateGapFiller(FillGapPattern, TimeSpan.Zero,
                                action.TimeStamp.Subtract(gapgap), null, nextIsLow));
                        }
                    }
                }
                else if (index == _originalActions.Count - 1)
                {
                    if (FillLastGap)
                    {
                        bool previousIsLow = action.Position < 50;

                        if (Duration - action.TimeStamp > MinGapDuration)
                        {
                            additionalActions.AddRange(GenerateGapFiller(FillGapPattern, action.TimeStamp.Add(gapgap), Duration,
                                previousIsLow, null));
                        }
                    }
                }
                else
                {
                    // Can't be null since it's not the first item
                    // ReSharper disable once PossibleNullReferenceException

                    bool previousIsLow = previous.Position < 50;
                    bool nextIsLow = action.Position < 50;

                    TimeSpan duration = action.TimeStamp - previous.TimeStamp;
                    if (duration > MinGapDuration)
                    {
                        additionalActions.AddRange(GenerateGapFiller(FillGapPattern, previous.TimeStamp.Add(gapgap),
                            action.TimeStamp.Subtract(gapgap), previousIsLow, nextIsLow));
                    }
                }

                previous = action;
            }

            _filledActions.AddRange(additionalActions);
            _filledActions.Sort((a, b) => a.TimeStamp.CompareTo(b.TimeStamp));
        }

        private IEnumerable<FunScriptAction> GenerateGapFiller(RepeatablePattern pattern, TimeSpan start, TimeSpan end, bool? startHigh = false, bool? endHigh = false)
        {
            if (pattern == null)
                pattern = new RepeatablePattern(0, 99);

            List<FunScriptAction> additionalActions = new List<FunScriptAction>();
            TimeSpan gapduration = end - start;

            double approximateFillers = gapduration.Divide(FillGapIntervall);

            bool up;
            bool? shouldBeEven;

            if (startHigh != null && endHigh != null)
            {
                up = (bool)startHigh;
                shouldBeEven = startHigh != endHigh;
            }
            else if (startHigh != null)
            {
                up = (bool)startHigh;
                shouldBeEven = null;
            }
            else if (endHigh != null)
            {
                bool wouldBeEven = Round(approximateFillers, null) % 2 == 0;
                up = !(wouldBeEven ^ (bool)endHigh);
                shouldBeEven = null;
            }
            else
            {
                up = false;
                shouldBeEven = null;
            }

            //Since we add an additional entry automatically, invert even and odd
            shouldBeEven = !shouldBeEven;

            int fillers = Round(approximateFillers, shouldBeEven);

            //for (int i = 0; i <= fillers; i++)
            //{
            //    additionalActions.Add(new FunScriptAction
            //    {
            //        Position = (byte)(up ? 99 : 0),
            //        TimeStamp = start + gapduration.Multiply(i).Divide(fillers),
            //        OriginalAction = false
            //    });

            //    up ^= true;
            //}

            // TODO Reuse logic from above for new patterns

            int patternIndex = 0;
            int duration = 1;

            for (int i = 0; i <= fillers; i += duration)
            {
                additionalActions.Add(new FunScriptAction
                {
                    Position = pattern[patternIndex].Position,
                    TimeStamp = start + gapduration.Multiply(i).Divide(fillers),
                    OriginalAction = false
                });

                duration = Math.Min(1, pattern[patternIndex].Duration);
                patternIndex++;
            }

            return additionalActions;
        }

        private int Round(double value, bool? shouldBeEven)
        {
            int result;

            switch (shouldBeEven)
            {
                case true:
                case false:
                    {
                        if (((int)Math.Ceiling(value) % 2 == 0) == shouldBeEven)
                            result = (int)Math.Ceiling(value);
                        else
                            result = (int)Math.Floor(value);
                        break;
                    }
                default:
                    {
                        result = (int)Math.Round(value);
                        break;
                    }
            }

            if (result <= 0)
                result = 0;

            return result;
        }

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
            _lastIntermediateIndex = int.MaxValue;
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
                _lastIntermediateIndex = 0;

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

                if (newTimeSpan <= previous) return;
                if (newTimeSpan >= next) return;

                TimeSpan duration = next - previous;

                if (duration < MinIntermediateCommandDuration.Multiply(2))
                    return;

                int intermediateCommands = (int)Math.Floor(duration.Divide(MinIntermediateCommandDuration));
                TimeSpan intermediateDuration = duration.Divide(intermediateCommands + 1);

                int passedIntermediateCommand = (int)Math.Floor((newTimeSpan - previous).Divide(intermediateDuration));

                if (_lastIntermediateIndex < passedIntermediateCommand)
                {
                    _lastIntermediateIndex = passedIntermediateCommand;
                    double progress = passedIntermediateCommand / (double)(intermediateCommands + 1);

                    IntermediateScriptActionEventArgs args =
                        new IntermediateScriptActionEventArgs(_filledActions[_lastIndex],
                            _filledActions[_lastIndex + 1],
                            progress)
                        { TimeStamp = newTimeSpan };

                    //Debug.WriteLine($"{DateTime.Now:HH:mm:ss.fff}: Intermediate {passedIntermediateCommand}/{intermediateCommands+1}, {progress :P0}");

                    OnIntermediateScriptActionRaised(args);
                }
                else
                {
                    double progress = (newTimeSpan - previous).Divide(next - previous);

                    IntermediateScriptActionEventArgs args =
                        new IntermediateScriptActionEventArgs(_filledActions[_lastIndex],
                            _filledActions[_lastIndex + 1], progress);

                    OnInstantUpdateRaised(args);
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

        protected virtual void OnInstantUpdateRaised(IntermediateScriptActionEventArgs e)
        {
            InstantUpdateRaised?.Invoke(this, e);
        }

        public bool IsScriptLoaded()
        {
            return _originalActions.Count > 0;
        }
    }
}
