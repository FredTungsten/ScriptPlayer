using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml.Serialization;
using JetBrains.Annotations;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Scripts;
using ScriptPlayer.Shared.TheHandy;

namespace ScriptPlayer.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private string _handyDeviceId;

        private string _vlcEndpoint;
        private string _vlcPassword;
        private string _whirligigEndpoint;
        private string _buttplugUrl;

        private ObservableCollection<string> _additionalPaths = new ObservableCollection<string>();
        private ObservableCollection<FavouriteFolder> _favouriteFolders = new ObservableCollection<FavouriteFolder>();

        private bool _checkForNewVersionOnStartup = true;
        private bool _autoSkip;

        private TimeSpan _commandDelay = TimeSpan.FromMilliseconds(166);
        private TimeSpan _scriptDelay = TimeSpan.Zero;

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
        private bool _filterDoubleClicks = true;
        private bool _doubleClickToFullscreen = true;
        private bool _clickToPlayPause = true;
        private bool _rememberPlaylist = true;
        private bool _shufflePlaylist;
        private bool _repeatPlaylist;
        private bool _softSeekGaps = true;
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
        private bool _showSkipButton;
        private bool _notifyFileLoadedOnlyFailed;

        private TimeSpan _fillGapIntervall = TimeSpan.FromMilliseconds(500);
        private TimeSpan _fillGapGap = TimeSpan.FromSeconds(2);
        private TimeSpan _minGapDuration = TimeSpan.FromSeconds(10); //This is for fillers, not the same as MainViewModel._gapDuration!
        private bool _invertPosition;
        private bool _randomChapters;
        private bool _softSeekFiles = true;
        private bool _softSeekLoops = true;
        private NoScriptBehaviors _noScriptBehavior = NoScriptBehaviors.KeepLastScript;
        private string _fallbackScriptFile;
        private TimeSpan _softSeekGapDuration = TimeSpan.FromSeconds(3);
        private TimeSpan _softSeekFilesDuration = TimeSpan.FromSeconds(3);
        private TimeSpan _softSeekLoopsDuration = TimeSpan.FromSeconds(3);
        private string _mpcHcEndpoint;
        private int _samsungVrUdpPort;
        private string _zoomPlayerEndpoint;
        private bool _autoHomingEnabled = false;

        // kodi settings
        private string _kodiIp;
        private int _kodiTcpPort;
        private int _kodiHttpPort;
        private string _kodiUser;
        private string _kodiPassword;

        public string HandyDeviceId 
        {
            get => _handyDeviceId;
            set
            {
                if (value == _handyDeviceId) return;
                _handyDeviceId = value;
                OnPropertyChanged();
            }
        }

        public HandyHost HandyScriptHost
        {
            get => _handyScriptHost;
            set
            {
                if (value == _handyScriptHost) return;
                _handyScriptHost = value;
                OnPropertyChanged();
            }
        }

        public string HandyLocalIp
        {
            get => _handyLocalIp;
            set
            {
                if (value == _handyLocalIp) return;
                _handyLocalIp = value;
                OnPropertyChanged();
            }
        }

        public int HandyLocalPort
        {
            get => _handyLocalPort;
            set
            {
                if (value == _handyLocalPort) return;
                _handyLocalPort = value;
                OnPropertyChanged();
            }
        }

        public VibratorConversionMode VibratorConversionMode
        {
            get => _vibratorConversionMode;
            set
            {
                if (value == _vibratorConversionMode) return;
                _vibratorConversionMode = value;
                OnPropertyChanged();
            }
        }

        public string KodiIp
        {
            get => _kodiIp;
            set
            {
                if (value == _kodiIp) return;
                _kodiIp = value;
                OnPropertyChanged();
            }
        }

        public int KodiTcpPort
        {
            get => _kodiTcpPort;
            set
            {
                if (value == _kodiTcpPort) return;
                _kodiTcpPort = value;
                OnPropertyChanged();
            }
        }

        public int KodiHttpPort
        {
            get => _kodiHttpPort;
            set
            {
                if (value == _kodiHttpPort) return;
                _kodiHttpPort = value;
                OnPropertyChanged();
            }
        }

        public string KodiUser
        {
            get => _kodiUser;
            set
            {
                if (value == _kodiUser) return;
                _kodiUser = value;
                OnPropertyChanged();
            }
        }

        public string KodiPassword
        {
            get => _kodiPassword;
            set
            {
                if (value == _kodiPassword) return;
                _kodiPassword = value;
                OnPropertyChanged();
            }
        }
        // end kodi settings




        private bool _stayOnTop;
        private bool _repeatSingleFile;
        private bool _rememberVolume;
        private bool _rememberPlaybackMode;
        private TimeSpan _patternSpeed = TimeSpan.FromMilliseconds(300);
        private ChapterMode _chapterMode = ChapterMode.RandomChapter;
        private TimeSpan _chapterTargetDuration = TimeSpan.FromSeconds(60);
        private bool _rememberWindowPosition;
        private int _rangeExtender;
        private string _ffmpegPath;
        private TimeDisplayMode _timeDisplayMode;
        private VibratorConversionMode _vibratorConversionMode = VibratorConversionMode.PositionToSpeed;
        private bool _limitDisplayedTimeToSelection = true;
        private bool _autogenerateThumbnails;
        private string _scriptFormatPreference;
        private string _mediaFormatPreference;
        private string _buttplugExePath;
        private bool _autoStartButtplug;
        private bool _autoConnectToButtplug;
        private bool _autoSearchForButtplugDevices;
        private bool _autoShowDeviceManager;
        private int _buttplugConnectionAttempts;
        private int _buttplugConnectionDelay;
        private bool _loopFallbackScript;
        private bool _removeGapsFromFallbackScript;
        private bool _useExternalOsd;
        private bool _autoShowGeneratorProgress;
        private bool _autogenerateAllForPlaylist;
        private bool _fuzzyMatchingEnabled;
        private string _fuzzyMatchingPattern;
        private string _fuzzyMatchingReplacement;
        private double _cropLeft;
        private double _cropRight;
        private double _cropTop;
        private double _cropBottom;
        private Rect _cropRect;
        private bool _rebuildingCropRect;
        private bool _cropVideo;
        private bool _autoReloadScript;
        private bool _includeFilledGapsInRandomSelection;
        private string _estimAudioDevice;
        private string _funstimFrequencies = "420,520,620";
        private TimeSpan _funstimFadeMs = TimeSpan.FromMilliseconds(1000);
        private int _funstimRamp = 0;
        private bool _funstimFadeOnPause;
        private TimeSpan _audioDelay;
        private string _deoVrEndpoint;
        private HandyHost _handyScriptHost;
        private string _handyLocalIp;
        private int _handyLocalPort;
        private bool _urlDecodeFilenames;
        private bool _subtitlesEnabled;
        private string _subtitleFormatPreference;
        private PlaylistViewStyle _playlistViewStyle;

        public SettingsViewModel()
        {
            WhirligigEndpoint = null;
            VlcEndpoint = null;
            ButtplugUrl = null;
            MpcHcEndpoint = null;
            SamsungVrUdpPort = 0;
            ZoomPlayerEndpoint = null;
            KodiUser = null;
            KodiPassword = null;
            KodiIp = null;
            KodiTcpPort = 0;
            KodiHttpPort = 0;

            HandyDeviceId = HandyHelper.DefaultDeviceId;
            HandyScriptHost = HandyHost.Local;
            HandyLocalPort = 80;
            HandyLocalIp = "";

            TimeDisplayMode = TimeDisplayMode.ContentOnly;
            AutogenerateThumbnails = true;
            AutogenerateAllForPlaylist = true;
            ScriptFormatPreference = "funscript, txt";
            ButtplugConnectionAttempts = 10;
            ButtplugConnectionDelay = 5;
            AutoShowGeneratorProgress = true;

            FuzzyMatchingPattern = @"^(?<Title>.+?)\s?\(\d{4}\)$";
            FuzzyMatchingReplacement = @"${Title}";

            SubtitlesEnabled = true;
            SubtitleFormatPreference = "ass, ssa, srt";

            CropLeft = 0;
            CropRight = 1;
            CropTop = 0;
            CropBottom = 1;
        }

        public SettingsViewModel Duplicate()
        {
            SettingsViewModel duplicate = new SettingsViewModel();

            foreach (PropertyInfo property in GetType().GetProperties())
            {
                try
                {
                    property.SetValue(duplicate, property.GetValue(this));
                }
                catch
                {
                    // o_O
                }
            }

            duplicate.AdditionalPaths = new ObservableCollection<string>(AdditionalPaths ?? new ObservableCollection<string>());
            duplicate.FavouriteFolders = new ObservableCollection<FavouriteFolder>(FavouriteFolders?.Select(f => f.Duplicate()) ?? new ObservableCollection<FavouriteFolder>());

            return duplicate;
        }

        public bool UseExternalOsd
        {
            get => _useExternalOsd;
            set
            {
                if (value == _useExternalOsd) return;
                _useExternalOsd = value;
                OnPropertyChanged();
            }
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

        public bool StayOnTop
        {
            get => _stayOnTop;
            set
            {
                if (value == _stayOnTop) return;
                _stayOnTop = value;
                OnPropertyChanged();
            }
        }

        public bool NotifyFileLoadedOnlyFailed
        {
            get => _notifyFileLoadedOnlyFailed;
            set
            {
                if (value == _notifyFileLoadedOnlyFailed) return;
                _notifyFileLoadedOnlyFailed = value;
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

        public bool RandomChapters
        {
            get => _randomChapters;
            set
            {
                if (value == _randomChapters) return;
                _randomChapters = value;
                OnPropertyChanged();
            }
        }

        public PlaylistViewStyle PlaylistViewStyle
        {
            get => _playlistViewStyle;
            set
            {
                if (value == _playlistViewStyle) return;
                _playlistViewStyle = value;
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

        public bool RepeatSingleFile
        {
            get => _repeatSingleFile;
            set
            {
                if (value == _repeatSingleFile) return;
                _repeatSingleFile = value;
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

        public bool RememberVolume
        {
            get => _rememberVolume;
            set
            {
                if (value == _rememberVolume) return;
                _rememberVolume = value;
                OnPropertyChanged();
            }
        }

        public bool RememberPlaybackMode
        {
            get => _rememberPlaybackMode;
            set
            {
                if (value == _rememberPlaybackMode) return;
                _rememberPlaybackMode = value;
                OnPropertyChanged();
            }
        }

        public bool RememberWindowPosition
        {
            get => _rememberWindowPosition;
            set
            {
                if (value == _rememberWindowPosition) return;
                _rememberWindowPosition = value;
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

        public ObservableCollection<FavouriteFolder> FavouriteFolders
        {
            get => _favouriteFolders;
            set
            {
                if (Equals(value, _favouriteFolders)) return;
                _favouriteFolders = value;
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

        public string DeoVrEndpoint
        {
            get => _deoVrEndpoint;
            set
            {
                if (value == _deoVrEndpoint) return;
                _deoVrEndpoint = value;
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

        public string MpcHcEndpoint
        {
            get => _mpcHcEndpoint;
            set
            {
                if (value == _mpcHcEndpoint) return;
                _mpcHcEndpoint = value;
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

        public string ButtplugExePath
        {
            get => _buttplugExePath;
            set
            {
                if (value == _buttplugExePath) return;
                _buttplugExePath = value;
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

        public bool LimitDisplayedTimeToSelection
        {
            get => _limitDisplayedTimeToSelection;
            set
            {
                if (value == _limitDisplayedTimeToSelection) return;
                _limitDisplayedTimeToSelection = value;
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

        public int RangeExtender
        {
            get => _rangeExtender;
            set
            {
                if (value == _rangeExtender) return;
                _rangeExtender = value;
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
            get => _showFilledGapsInHeatMap;
            set
            {
                if (value == _showFilledGapsInHeatMap) return;
                _showFilledGapsInHeatMap = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public TimeSpan FillGapIntervall
        {
            get => _fillGapIntervall;
            set
            {
                if (value.Equals(_fillGapIntervall)) return;
                _fillGapIntervall = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("FillGapIntervall")]
        public long FillGapIntervallWrapper
        {
            get => FillGapIntervall.Ticks;
            set => FillGapIntervall = TimeSpan.FromTicks(value);
        }

        [XmlIgnore]
        public TimeSpan FillGapGap
        {
            get => _fillGapGap;
            set
            {
                if (value.Equals(_fillGapGap)) return;
                _fillGapGap = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("FillGapGap")]
        public long FillGapGapWrapper
        {
            get => FillGapGap.Ticks;
            set => FillGapGap = TimeSpan.FromTicks(value);
        }

        [XmlIgnore]
        public TimeSpan MinGapDuration
        {
            get => _minGapDuration;
            set
            {
                if (value.Equals(_minGapDuration)) return;
                _minGapDuration = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("MinGapDuration")]
        public long MinGapDurationWrapper
        {
            get => MinGapDuration.Ticks;
            set => MinGapDuration = TimeSpan.FromTicks(value);
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

        public bool InvertPosition
        {
            get => _invertPosition;
            set
            {
                if (value == _invertPosition) return;
                _invertPosition = value;
                OnPropertyChanged();
            }
        }


        public bool AutoHomingEnabled
        {
            get => _autoHomingEnabled;
            set
            {
                if (value == _autoHomingEnabled) return;
                _autoHomingEnabled = value;
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

        [XmlElement("AudioDelay")]
        public long AudioDelayWrapper
        {
            get => AudioDelay.Ticks;
            set => AudioDelay = TimeSpan.FromTicks(value);
        }

        [XmlIgnore]
        public TimeSpan AudioDelay
        {
            get => _audioDelay;
            set
            {
                if (value.Equals(_audioDelay)) return;
                _audioDelay = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("FunstimFadeMs")]
        public long FunstimFadeMsWrapper
        {
            get => FunstimFadeMs.Ticks;
            set => FunstimFadeMs = TimeSpan.FromTicks(value);
        }

        [XmlIgnore]
        public TimeSpan FunstimFadeMs
        {
            get => _funstimFadeMs;
            set
            {
                if (value.Equals(_funstimFadeMs)) return;
                _funstimFadeMs = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("FunstimRamp")]
        public int FunstimFadeRampWrapper
        {
            get => _funstimRamp;
            set => FunstimRamp = value;
        }

        [XmlIgnore]
        public int FunstimRamp
        {
            get => _funstimRamp;
            set
            {
                if (value.Equals(_funstimRamp)) return;
                _funstimRamp = value;
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

        [XmlIgnore]
        public TimeSpan PatternSpeed
        {
            get { return _patternSpeed; }
            set
            {
                if (value.Equals(_patternSpeed)) return;
                _patternSpeed = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("PatternSpeed")]
        public long PatternSpeedWrapper
        {
            get => PatternSpeed.Ticks;
            set => PatternSpeed = TimeSpan.FromTicks(value);
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

        public bool SoftSeekGaps
        {
            get => _softSeekGaps;
            set
            {
                if (value == _softSeekGaps) return;
                _softSeekGaps = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public TimeSpan SoftSeekGapDuration
        {
            get => _softSeekGapDuration;
            set
            {
                if (value.Equals(_softSeekGapDuration)) return;
                _softSeekGapDuration = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("SoftSeekGapDuration")]
        public long SoftSeekGapDurationWrapper
        {
            get => SoftSeekGapDuration.Ticks;
            set => SoftSeekGapDuration = TimeSpan.FromTicks(value);
        }

        public bool SoftSeekFiles
        {
            get => _softSeekFiles;
            set
            {
                if (value == _softSeekFiles) return;
                _softSeekFiles = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public TimeSpan SoftSeekFilesDuration
        {
            get => _softSeekFilesDuration;
            set
            {
                if (value.Equals(_softSeekFilesDuration)) return;
                _softSeekFilesDuration = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("SoftSeekFilesDuration")]
        public long SoftSeekFilesDurationWrapper
        {
            get => SoftSeekFilesDuration.Ticks;
            set => SoftSeekFilesDuration = TimeSpan.FromTicks(value);
        }

        public bool SoftSeekLoops
        {
            get => _softSeekLoops;
            set
            {
                if (value == _softSeekLoops) return;
                _softSeekLoops = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public TimeSpan SoftSeekLoopsDuration
        {
            get => _softSeekLoopsDuration;
            set
            {
                if (value.Equals(_softSeekLoopsDuration)) return;
                _softSeekLoopsDuration = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("SoftSeekLoopsDuration")]
        public long SoftSeekLoopsDurationWrapper
        {
            get => SoftSeekLoopsDuration.Ticks;
            set => SoftSeekLoopsDuration = TimeSpan.FromTicks(value);
        }

        [XmlIgnore]
        public TimeSpan ChapterTargetDuration
        {
            get => _chapterTargetDuration;
            set
            {
                if (value.Equals(_chapterTargetDuration)) return;
                _chapterTargetDuration = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("ChapterTargetDuration")]
        public long ChapterTargetDurationWrapper
        {
            get => ChapterTargetDuration.Ticks;
            set => ChapterTargetDuration = TimeSpan.FromTicks(value);
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

        public bool ShowSkipButton
        {
            get => _showSkipButton;
            set
            {
                if (value == _showSkipButton) return;
                _showSkipButton = value;
                OnPropertyChanged();
            }
        }

        public string FallbackScriptFile
        {
            get => _fallbackScriptFile;
            set
            {
                if (value == _fallbackScriptFile) return;
                _fallbackScriptFile = value;
                OnPropertyChanged();
            }
        }

        public NoScriptBehaviors NoScriptBehavior
        {
            get => _noScriptBehavior;
            set
            {
                if (value == _noScriptBehavior) return;
                _noScriptBehavior = value;
                OnPropertyChanged();
            }
        }

        public int SamsungVrUdpPort
        {
            get => _samsungVrUdpPort;
            set
            {
                if (value == _samsungVrUdpPort) return;
                _samsungVrUdpPort = value;
                OnPropertyChanged();
            }
        }

        public string ZoomPlayerEndpoint
        {
            get => _zoomPlayerEndpoint;
            set
            {
                if (value == _zoomPlayerEndpoint) return;
                _zoomPlayerEndpoint = value;
                OnPropertyChanged();
            }
        }

        public ChapterMode ChapterMode
        {
            get => _chapterMode;
            set
            {
                if (value == _chapterMode) return;
                _chapterMode = value;
                OnPropertyChanged();
            }
        }

        public string FfmpegPath
        {
            get => _ffmpegPath;
            set
            {
                if (value == _ffmpegPath) return;
                _ffmpegPath = value;
                OnPropertyChanged();
            }
        }

        public TimeDisplayMode TimeDisplayMode
        {
            get => _timeDisplayMode;
            set
            {
                if (value == _timeDisplayMode) return;
                _timeDisplayMode = value;
                OnPropertyChanged();
            }
        }

        public bool AutogenerateThumbnails
        {
            get => _autogenerateThumbnails;
            set
            {
                if (value == _autogenerateThumbnails) return;
                _autogenerateThumbnails = value;
                OnPropertyChanged();
            }
        }

        public bool AutogenerateAllForPlaylist
        {
            get => _autogenerateAllForPlaylist;
            set
            {
                if (value == _autogenerateAllForPlaylist) return;
                _autogenerateAllForPlaylist = value;
                OnPropertyChanged();
            }
        }

        public string MediaFormatPreference
        {
            get => _mediaFormatPreference;
            set
            {
                if (value == _mediaFormatPreference) return;
                _mediaFormatPreference = value;
                OnPropertyChanged();
            }
        }

        public string ScriptFormatPreference
        {
            get => _scriptFormatPreference;
            set
            {
                if (value == _scriptFormatPreference) return;
                _scriptFormatPreference = value;
                OnPropertyChanged();
            }
        }

        public bool AutoStartButtplug
        {
            get => _autoStartButtplug;
            set
            {
                if (value == _autoStartButtplug) return;
                _autoStartButtplug = value;
                OnPropertyChanged();
            }
        }

        public bool AutoConnectToButtplug
        {
            get => _autoConnectToButtplug;
            set
            {
                if (value == _autoConnectToButtplug) return;
                _autoConnectToButtplug = value;
                OnPropertyChanged();
            }
        }

        public bool AutoSearchForButtplugDevices
        {
            get => _autoSearchForButtplugDevices;
            set
            {
                if (value == _autoSearchForButtplugDevices) return;
                _autoSearchForButtplugDevices = value;
                OnPropertyChanged();
            }
        }

        public bool AutoShowDeviceManager
        {
            get => _autoShowDeviceManager;
            set
            {
                if (value == _autoShowDeviceManager) return;
                _autoShowDeviceManager = value;
                OnPropertyChanged();
            }
        }

        public int ButtplugConnectionAttempts
        {
            get => _buttplugConnectionAttempts;
            set
            {
                if (value == _buttplugConnectionAttempts) return;
                _buttplugConnectionAttempts = value;
                OnPropertyChanged();
            }
        }

        public int ButtplugConnectionDelay
        {
            get => _buttplugConnectionDelay;
            set
            {
                if (value == _buttplugConnectionDelay) return;
                _buttplugConnectionDelay = value;
                OnPropertyChanged();
            }
        }

        public bool LoopFallbackScript
        {
            get => _loopFallbackScript;
            set
            {
                if (value == _loopFallbackScript) return;
                _loopFallbackScript = value;
                OnPropertyChanged();
            }
        }

        public bool RemoveGapsFromFallbackScript
        {
            get => _removeGapsFromFallbackScript;
            set
            {
                if (value == _removeGapsFromFallbackScript) return;
                _removeGapsFromFallbackScript = value;
                OnPropertyChanged();
            }
        }

        public bool AutoShowGeneratorProgress
        {
            get => _autoShowGeneratorProgress;
            set
            {
                if (value == _autoShowGeneratorProgress) return;
                _autoShowGeneratorProgress = value;
                OnPropertyChanged();
            }
        }

        public bool UrlDecodeFilenames
        {
            get => _urlDecodeFilenames;
            set
            {
                if (value == _urlDecodeFilenames) return;
                _urlDecodeFilenames = value;
                OnPropertyChanged();
            }
        }

        public bool SubtitlesEnabled
        {
            get => _subtitlesEnabled;
            set
            {
                if (value == _subtitlesEnabled) return;
                _subtitlesEnabled = value;
                OnPropertyChanged();
            }
        }

        public string SubtitleFormatPreference
        {
            get => _subtitleFormatPreference;
            set
            {
                if (value == _subtitleFormatPreference) return;
                _subtitleFormatPreference = value;
                OnPropertyChanged();
            }
        }

        public bool FuzzyMatchingEnabled
        {
            get => _fuzzyMatchingEnabled;
            set
            {
                if (value == _fuzzyMatchingEnabled) return;
                _fuzzyMatchingEnabled = value;
                OnPropertyChanged();
            }
        }

        public string FuzzyMatchingPattern
        {
            get => _fuzzyMatchingPattern;
            set
            {
                if (value == _fuzzyMatchingPattern) return;
                _fuzzyMatchingPattern = value;
                OnPropertyChanged();
            }
        }

        public string FuzzyMatchingReplacement

        {
            get => _fuzzyMatchingReplacement;
            set
            {
                if (value == _fuzzyMatchingReplacement) return;
                _fuzzyMatchingReplacement = value;
                OnPropertyChanged();
            }
        }

        public double CropLeft
        {
            get => _cropLeft;
            set
            {
                if (value.Equals(_cropLeft)) return;
                _cropLeft = value;
                OnPropertyChanged();
                RebuildCropRect();
            }
        }

        public double CropRight
        {
            get => _cropRight;
            set
            {
                if (value.Equals(_cropRight)) return;
                _cropRight = value;
                OnPropertyChanged();
                RebuildCropRect();
            }
        }

        public double CropTop
        {
            get => _cropTop;
            set
            {
                if (value.Equals(_cropTop)) return;
                _cropTop = value;
                OnPropertyChanged();
                RebuildCropRect();
            }
        }
        
        public double CropBottom
        {
            get => _cropBottom;
            set
            {
                if (value.Equals(_cropBottom)) return;
                _cropBottom = value;
                OnPropertyChanged();
                RebuildCropRect();
            }
        }

        [XmlIgnore]
        public Rect CropRect
        {
            get => _cropRect;
            set
            {
                if (value.Equals(_cropRect)) return;
                _cropRect = value;
                OnPropertyChanged();

                if (!_rebuildingCropRect)
                {
                    CropLeft = _cropRect.Left;
                    CropRight = _cropRect.Right;
                    CropTop = _cropRect.Top;
                    CropBottom = _cropRect.Bottom;
                }
            }
        }

        public bool CropVideo
        {
            get => _cropVideo;
            set
            {
                if (value == _cropVideo) return;
                _cropVideo = value;
                OnPropertyChanged();
            }
        }

        public bool AutoReloadScript
        {
            get => _autoReloadScript;
            set
            {
                if (value == _autoReloadScript) return;
                _autoReloadScript = value;
                OnPropertyChanged();
            }
        }

        public bool IncludeFilledGapsInRandomSelection
        {
            get => _includeFilledGapsInRandomSelection;
            set
            {
                if (value == _includeFilledGapsInRandomSelection) return;
                _includeFilledGapsInRandomSelection = value;
                OnPropertyChanged();
            }
        }

        public string EstimAudioDevice
        {
            get => _estimAudioDevice;
            set
            {
                if (Equals(value,_estimAudioDevice)) return;
                _estimAudioDevice = value;
                OnPropertyChanged();
            }
        }

        public string FunstimFrequencies
        {
            get => _funstimFrequencies;
            set
            {
                if (value == _funstimFrequencies) return;
                _funstimFrequencies = value;
                OnPropertyChanged();
            }
        }

        public bool FunstimFadeOnPause
        {
            get => _funstimFadeOnPause;
            set
            {
                if (value == _funstimFadeOnPause) return;
                _funstimFadeOnPause = value;
                OnPropertyChanged();
            }
        }

        private void RebuildCropRect()
        {
            if (_rebuildingCropRect)
                return;

            _rebuildingCropRect = true;

            CropRect = new Rect(new Point(CropLeft, CropTop), new Point(CropRight, CropBottom));

            _rebuildingCropRect = false;
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
                if (!File.Exists(filename))
                    return null;

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

    public enum NoScriptBehaviors
    {
        KeepLastScript = 1,
        ClearScript = 2,
        FallbackScript = 3
    }

    public class FavouriteFolder : INotifyPropertyChanged
    {
        private string _path;
        private string _name;
        private bool _isDefault;

        public string Path
        {
            get => _path;
            set
            {
                if (value == _path) return;
                _path = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public bool IsDefault
        {
            get => _isDefault;
            set
            {
                if (value == _isDefault) return;
                _isDefault = value;
                OnPropertyChanged();
            }
        }

        public FavouriteFolder Duplicate()
        {
            return new FavouriteFolder
            {
                Path = Path,
                Name = Name,
                IsDefault = IsDefault
            };
        }

        [field: XmlIgnore]
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}