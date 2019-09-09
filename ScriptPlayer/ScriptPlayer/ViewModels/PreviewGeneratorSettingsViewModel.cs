using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using ScriptPlayer.Generators;

namespace ScriptPlayer.ViewModels
{
    public class PreviewGeneratorSettingsViewModel : INotifyPropertyChanged
    {
        private bool _autoWidth;
        private int _width;

        private bool _autoHeight;
        private int _height;

        private double _frameRate;

        private TimeSpan _start;
        private TimeSpan _duration;

        private int _sectionCount;
        private TimeSpan _durationEach;
        private bool _mulitpleSections;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool AutoWidth
        {
            get => _autoWidth;
            set
            {
                if (value == _autoWidth) return;
                _autoWidth = value;
                OnPropertyChanged();
            }
        }

        public int Width
        {
            get => _width;
            set
            {
                if (value == _width) return;
                _width = value;
                OnPropertyChanged();
            }
        }

        public bool AutoHeight
        {
            get => _autoHeight;
            set
            {
                if (value == _autoHeight) return;
                _autoHeight = value;
                OnPropertyChanged();
            }
        }

        public int Height
        {
            get => _height;
            set
            {
                if (value == _height) return;
                _height = value;
                OnPropertyChanged();
            }
        }

        public double FrameRate
        {
            get => _frameRate;
            set
            {
                if (value.Equals(_frameRate)) return;
                _frameRate = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan Start
        {
            get => _start;
            set
            {
                if (value.Equals(_start)) return;
                _start = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan Duration
        {
            get => _duration;
            set
            {
                if (value.Equals(_duration)) return;
                _duration = value;
                OnPropertyChanged();
            }
        }

        public int SectionCount
        {
            get => _sectionCount;
            set
            {
                if (value == _sectionCount) return;
                _sectionCount = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan DurationEach
        {
            get => _durationEach;
            set
            {
                if (value.Equals(_durationEach)) return;
                _durationEach = value;
                OnPropertyChanged();
            }
        }

        public bool MulitpleSections
        {
            get => _mulitpleSections;
            set
            {
                if (value == _mulitpleSections) return;
                _mulitpleSections = value;
                OnPropertyChanged();
            }
        }

        public PreviewGeneratorSettingsViewModel()
        {
            Height = 170;
            Width = 95;

            AutoWidth = true;
            FrameRate = 24;

            MulitpleSections = true;

            SectionCount = 12;
            DurationEach = TimeSpan.FromSeconds(0.8);

            Start = TimeSpan.Zero;
            Duration = TimeSpan.FromSeconds(5);
        }

        public PreviewGeneratorSettings GetSettings(out string[] errorMessages)
        {
            List<string> errors = new List<string>();

            if (Width <= 0 && !AutoWidth)
            {
                errors.Add("Width must be greater than zero or automatic");
            }

            if (Height <= 0 && !AutoHeight)
            {
                errors.Add("Height must be greater than zero or automatic");
            }

            if (FrameRate <= 1)
            {
                errors.Add("Framerate must be greater than 1");
            }

            errorMessages = errors.ToArray();

            if (errors.Any())
                return null;

            var result = new PreviewGeneratorSettings
            {
                Height = AutoHeight ? -2 : Height,
                Width = AutoWidth ? -2 : Width,
                Framerate = FrameRate
            };

            if (MulitpleSections)
            {
                result.GenerateRelativeTimeFrames(SectionCount, DurationEach);
            }
            else
            {
                result.TimeFrames.Add(new TimeFrame
                {
                    StartTimeSpan = Start,
                    Duration = Duration
                });
            }

            return result;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}