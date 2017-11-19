using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using JetBrains.Annotations;
using ScriptPlayer.Shared.Scripts;

namespace ScriptPlayer.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private string _vlcEndpoint;
        private string _vlcPassword;
        private string _whirligigEndpoint;
        private string _buttplugUrl;

        private ObservableCollection<string> _additionalPaths;

        private bool _checkForNewVersionOnStartup;
        private bool _autoSkip;

        private TimeSpan _commandDelay = TimeSpan.FromMilliseconds(166);
        private TimeSpan _scriptDelay;

        private ConversionMode _conversionMode = ConversionMode.UpOrDown;
        private bool _logMarkers;

        private byte _maxPosition = 95;
        private byte _minPosition = 5;
        private byte _minSpeed = 20;
        private byte _maxSpeed = 95;
        private double _speedMultiplier = 1;
        
        private bool _showHeatMap;
        private PositionFilterMode _filterMode = PositionFilterMode.FullRange;
        private double _filterRange = 0.5;
        private bool _showScriptPositions;

        private bool _showTimeLeft;
        private bool _filterDoubleClicks;
        private bool _doubleClickToFullscreen;
        private bool _clickToPlayPause;
        private bool _rememberPlaylist;
        private bool _shufflePlaylist;
        private bool _repeatPlaylist;
        private bool _softSeek;
        private bool _seekFreezeFrame;
        private bool _notifyGaps = true;
        private bool _notifyPosition = true;
        private bool _notifyDevices = true;
        private bool _notifyFileLoaded = true;
        private bool _notifyPlayPause = true;
        private bool _notifyVolume = true;
        private bool _notifyLogging = true;
        private bool _fillGaps;
        private bool _fillFirstGap;
        private bool _fillLastGap;
        private bool _showFilledGapsInHeatMap;

        public SettingsViewModel()
        {
            WhirligigEndpoint = null;
            VlcEndpoint = null;
            ButtplugUrl = null;

            AdditionalPaths = new ObservableCollection<string>();
            CheckForNewVersionOnStartup = true;
            ClickToPlayPause = true;
            DoubleClickToFullscreen = true;
            FilterDoubleClicks = true;
            RememberPlaylist = true;
            SoftSeek = true;
            SeekFreezeFrame = true;
        }

        public SettingsViewModel Duplicate()
        {
            SettingsViewModel duplicate = new SettingsViewModel();

            foreach(PropertyInfo property in GetType().GetProperties())
                property.SetValue(duplicate, property.GetValue(this));

            duplicate.AdditionalPaths = new ObservableCollection<string>(AdditionalPaths);

            return duplicate;

            /*return new SettingsViewModel
            {
                AdditionalPaths = new ObservableCollection<string>(AdditionalPaths),
                AutoSkip = AutoSkip,
                ButtplugUrl = ButtplugUrl,
                CheckForNewVersionOnStartup = CheckForNewVersionOnStartup,
                ClickToPlayPause = ClickToPlayPause,
                CommandDelay = CommandDelay,
                ConversionMode = ConversionMode,
                DisplayEventNotifications = DisplayEventNotifications,
                DoubleClickToFullscreen = DoubleClickToFullscreen,
                FilterDoubleClicks = FilterDoubleClicks,
                FilterMode = FilterMode,
                FilterRange = FilterRange,
                LogMarkers = LogMarkers,
                MaxPosition = MaxPosition,
                MaxSpeed = MaxSpeed,
                MinPosition = MinPosition,
                MinSpeed = MinSpeed,
                RememberPlaylist = RememberPlaylist,
                RepeatPlaylist = RepeatPlaylist,
                ScriptDelay = ScriptDelay,
                ShowHeatMap = ShowHeatMap,
                ShowScriptPositions = ShowScriptPositions,
                ShowTimeLeft = ShowTimeLeft,
                SpeedMultiplier = SpeedMultiplier,
                ShufflePlaylist = ShufflePlaylist,
                SoftSeek = SoftSeek,
                SeekFreezeFrame = SeekFreezeFrame,
                VlcEndpoint = VlcEndpoint,
                VlcPassword = VlcPassword,
                WhirligigEndpoint = WhirligigEndpoint
            };*/
        }

        public bool NotifyVolume
        {
            get => _notifyVolume;
            set
            {
                if (value == _notifyVolume) return;
                _notifyVolume = value;
                OnPropertyChanged();
            }
        }

        public bool NotifyPlayPause
        {
            get => _notifyPlayPause;
            set
            {
                if (value == _notifyPlayPause) return;
                _notifyPlayPause = value;
                OnPropertyChanged();
            }
        }

        public bool NotifyFileLoaded
        {
            get => _notifyFileLoaded;
            set
            {
                if (value == _notifyFileLoaded) return;
                _notifyFileLoaded = value;
                OnPropertyChanged();
            }
        }

        public bool NotifyDevices
        {
            get => _notifyDevices;
            set
            {
                if (value == _notifyDevices) return;
                _notifyDevices = value;
                OnPropertyChanged();
            }
        }

        public bool NotifyPosition
        {
            get => _notifyPosition;
            set
            {
                if (value == _notifyPosition) return;
                _notifyPosition = value;
                OnPropertyChanged();
            }
        }

        public bool NotifyGaps
        {
            get => _notifyGaps;
            set
            {
                if (value == _notifyGaps) return;
                _notifyGaps = value;
                OnPropertyChanged();
            }
        }

        public bool ShufflePlaylist
        {
            get => _shufflePlaylist;
            set
            {
                if (value == _shufflePlaylist) return;
                _shufflePlaylist = value;
                OnPropertyChanged();
            }
        }

        public bool RepeatPlaylist
        {
            get => _repeatPlaylist;
            set
            {
                if (value == _repeatPlaylist) return;
                _repeatPlaylist = value;
                OnPropertyChanged();
            }
        }

        public bool RememberPlaylist
        {
            get => _rememberPlaylist;
            set
            {
                if (value == _rememberPlaylist) return;
                _rememberPlaylist = value;
                OnPropertyChanged();
            }
        }

        public bool ClickToPlayPause
        {
            get => _clickToPlayPause;
            set
            {
                if (value == _clickToPlayPause) return;
                _clickToPlayPause = value;
                OnPropertyChanged();
            }
        }

        public bool DoubleClickToFullscreen
        {
            get => _doubleClickToFullscreen;
            set
            {
                if (value == _doubleClickToFullscreen) return;
                _doubleClickToFullscreen = value;
                OnPropertyChanged();
            }
        }

        public bool FilterDoubleClicks
        {
            get => _filterDoubleClicks;
            set
            {
                if (value == _filterDoubleClicks) return;
                _filterDoubleClicks = value;
                OnPropertyChanged();
            }
        }

        public bool CheckForNewVersionOnStartup
        {
            get => _checkForNewVersionOnStartup;
            set
            {
                if (value == _checkForNewVersionOnStartup) return;
                _checkForNewVersionOnStartup = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> AdditionalPaths
        {
            get => _additionalPaths;
            set
            {
                if (Equals(value, _additionalPaths)) return;
                _additionalPaths = value;
                OnPropertyChanged();
            }
        }

        public string VlcEndpoint
        {
            get => _vlcEndpoint;
            set
            {
                if (value == _vlcEndpoint) return;
                _vlcEndpoint = value;
                OnPropertyChanged();
            }
        }

        public string VlcPassword
        {
            get => _vlcPassword;
            set
            {
                if (value == _vlcPassword) return;
                _vlcPassword = value;
                OnPropertyChanged();
            }
        }

        public string WhirligigEndpoint
        {
            get => _whirligigEndpoint;
            set
            {
                if (value == _whirligigEndpoint) return;
                _whirligigEndpoint = value;
                OnPropertyChanged();
            }
        }

        public string ButtplugUrl
        {
            get => _buttplugUrl;
            set
            {
                if (value == _buttplugUrl) return;
                _buttplugUrl = value;
                OnPropertyChanged();
            }
        }

        public bool ShowTimeLeft
        {
            get => _showTimeLeft;
            set
            {
                if (value == _showTimeLeft) return;
                _showTimeLeft = value;
                OnPropertyChanged();
            }
        }

        public double FilterRange
        {
            get => _filterRange;
            set
            {
                if (value.Equals(_filterRange)) return;
                _filterRange = value;
                OnPropertyChanged();
            }
        }

        public PositionFilterMode FilterMode
        {
            get => _filterMode;
            set
            {
                if (value == _filterMode) return;
                _filterMode = value;
                OnPropertyChanged();
            }
        }

        public bool LogMarkers
        {
            get => _logMarkers;
            set
            {
                if (value == _logMarkers) return;
                _logMarkers = value;
                OnPropertyChanged();
            }
        }

        public bool FillGaps
        {
            get => _fillGaps;
            set
            {
                if (value == _fillGaps) return;
                _fillGaps = value;
                OnPropertyChanged();
            }
        }

        public bool FillFirstGap
        {
            get => _fillFirstGap;
            set
            {
                if (value == _fillFirstGap) return;
                _fillFirstGap = value;
                OnPropertyChanged();
            }
        }

        public bool FillLastGap
        {
            get => _fillLastGap;
            set
            {
                if (value == _fillLastGap) return;
                _fillLastGap = value;
                OnPropertyChanged();
            }
        }

        public bool ShowFilledGapsInHeatMap
        {
            get { return _showFilledGapsInHeatMap; }
            set
            {
                if (value == _showFilledGapsInHeatMap) return;
                _showFilledGapsInHeatMap = value;
                OnPropertyChanged();
            }
        }

        public bool AutoSkip
        {
            get => _autoSkip;
            set
            {
                if (value == _autoSkip) return;
                _autoSkip = value;
                OnPropertyChanged();
            }
        }

        public byte MinPosition
        {
            get => _minPosition;
            set
            {
                if (value == _minPosition) return;
                _minPosition = value;
                OnPropertyChanged();
            }
        }

        public byte MinSpeed
        {
            get => _minSpeed;
            set
            {
                if (value == _minSpeed) return;
                _minSpeed = value;
                OnPropertyChanged();
            }
        }

        public byte MaxSpeed
        {
            get => _maxSpeed;
            set
            {
                if (value == _maxSpeed) return;
                _maxSpeed = value;
                OnPropertyChanged();
            }
        }

        public byte MaxPosition
        {
            get => _maxPosition;
            set
            {
                if (value == _maxPosition) return;
                _maxPosition = value;
                OnPropertyChanged();
            }
        }

        public double SpeedMultiplier
        {
            get => _speedMultiplier;
            set
            {
                if (value.Equals(_speedMultiplier)) return;
                _speedMultiplier = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("ScriptDelay")]
        public long ScriptDelayWrapper
        {
            get => ScriptDelay.Ticks;
            set => ScriptDelay = TimeSpan.FromTicks(value);
        }

        [XmlIgnore]
        public TimeSpan ScriptDelay
        {
            get => _scriptDelay;
            set
            {
                if (value.Equals(_scriptDelay)) return;
                _scriptDelay = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("CommandDelay")]
        public long CommandDelayWrapper
        {
            get => CommandDelay.Ticks;
            set => CommandDelay = TimeSpan.FromTicks(value);
        }

        [XmlIgnore]
        public TimeSpan CommandDelay
        {
            get => _commandDelay;
            set
            {
                if (value.Equals(_commandDelay)) return;
                _commandDelay = value;
                OnPropertyChanged();
            }
        }

        public ConversionMode ConversionMode
        {
            get => _conversionMode;
            set
            {
                if (value == _conversionMode) return;
                _conversionMode = value;
                OnPropertyChanged();
            }
        }

        public bool ShowScriptPositions
        {
            get => _showScriptPositions;
            set
            {
                if (value == _showScriptPositions) return;
                _showScriptPositions = value;
                OnPropertyChanged();
            }
        }

        public bool ShowHeatMap
        {
            get => _showHeatMap;
            set
            {
                if (value == _showHeatMap) return;
                _showHeatMap = value;
                OnPropertyChanged();
            }
        }

        public bool SoftSeek
        {
            get => _softSeek;
            set
            {
                if (value == _softSeek) return;
                _softSeek = value;
                OnPropertyChanged();
            }
        }

        public bool SeekFreezeFrame
        {
            get => _seekFreezeFrame;
            set
            {
                if (value == _seekFreezeFrame) return;
                _seekFreezeFrame = value;
                OnPropertyChanged();
            }
        }

        public bool NotifyLogging
        {
            get => _notifyLogging;
            set
            {
                if (value == _notifyLogging) return;
                _notifyLogging = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static SettingsViewModel FromFile(string filename)
        {
            try
            {
                using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(SettingsViewModel));
                    return serializer.Deserialize(stream) as SettingsViewModel;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return null;
            }
        }

        public void Save(string filename)
        {
            try
            {
                string dir = Path.GetDirectoryName(filename);
                if (string.IsNullOrWhiteSpace(dir))
                    throw new ArgumentException(@"Directory is null", nameof(filename));

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using (FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(SettingsViewModel));
                    serializer.Serialize(stream, this);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
    }
}