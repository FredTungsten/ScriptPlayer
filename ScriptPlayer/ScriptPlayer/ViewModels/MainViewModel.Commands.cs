using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using ScriptPlayer.Shared;

// ReSharper disable MemberCanBePrivate.Global

namespace ScriptPlayer.ViewModels
{
    public partial class MainViewModel
    {
        #region Properties

        public ScriptplayerCommand ExecuteSelectedTestPatternCommand { get; set; }

        public ScriptplayerCommand VolumeDownCommand { get; set; }

        public ScriptplayerCommand VolumeUpCommand { get; set; }

        public ScriptplayerCommand TogglePlaybackCommand { get; set; }

        public ScriptplayerCommand SkipToNextEventCommand { get; set; }

        public ScriptplayerCommand StartScanningButtplugCommand { get; set; }

        public ScriptplayerCommand SetLoopACommand { get; set; }

        public ScriptplayerCommand SetLoopBCommand { get; set; }

        public ScriptplayerCommand ClearLoopCommand { get; set; }

        public ScriptplayerCommand ConnectButtplugCommand { get; set; }

        public ScriptplayerCommand DisconnectButtplugCommand { get; set; }

        public ScriptplayerCommand ToggleFullScreenCommand { get; set; }

        //public ScriptplayerCommand ConnectLaunchDirectlyCommand { get; set; }

        public ScriptplayerCommand ConnectHandyDirectlyCommand { get; set; }

        public ScriptplayerCommand AddEstimAudioCommand { get; set; }

        public ScriptplayerCommand AddMK312WifiCommand { get; set; }

        public ScriptplayerCommand AddMK312SerialCommand { get; set; }

        public ScriptplayerCommand AddFunstimAudioCommand { get; set; }

        public ScriptplayerCommand AddScriptsToPlaylistFirstCommand { get; set; }

        public ScriptplayerCommand AddScriptsToPlaylistCommand { get; set; }

        public ScriptplayerCommand AddFolderToPlaylistFirstCommand { get; set; }

        public ScriptplayerCommand AddFolderToPlaylistCommand { get; set; }

        public ScriptplayerCommand RemoveMissingEntriesFromPlaylistCommand { get; set; }

        public ScriptplayerCommand RemoveIncompleteEntriesFromPlaylistCommand { get; set; }

        public ScriptplayerCommand LoadPlaylistCommand { get; set; }

        public ScriptplayerCommand SavePlaylistCommand { get; set; }

        public ScriptplayerCommand OpenVideoCommand { get; set; }

        public ScriptplayerCommand OpenScriptCommand { get; set; }

        public ScriptplayerCommand GenerateThumbnailsForLoadedVideoCommand { get; set; }

        public ScriptplayerCommand GenerateThumbnailBannerForLoadedVideoCommand { get; set; }

        public ScriptplayerCommand GeneratePreviewForLoadedVideoCommand { get; set; }

        public ScriptplayerCommand GenerateHeatmapForLoadedVideoCommand { get; set; }

        public ScriptplayerCommand GenerateAllForLoadedVideoCommand { get; set; }

        public ScriptplayerCommand ShowSettingsCommand { get; set; }

        public ScriptplayerCommand ShowDeviceManagerCommand { get; set; }

        public ScriptplayerCommand ExportHandyCsvCommand { get; set; }

        public ScriptplayerCommand SaveScriptAsCommand { get; set; }

        public ScriptplayerCommand ShiftScriptCommand { get; set; }

        public ScriptplayerCommand TrimScriptCommand { get; set; }

        public ScriptplayerCommand ShowGeneratorSettingsCommand { get; set; }

        public ScriptplayerCommand ShowGeneratorProgressCommand { get; set; }

        public ScriptplayerCommand IncreaseHandyStrokeLengthCommand { get; set; }

        public ScriptplayerCommand DecreaseHandyStrokeLengthCommand { get; set; }

        public ScriptplayerCommand EditMetadataCommand { get; set; }

        public ScriptplayerCommand ReloadScriptCommand { get; set; }

        public ScriptplayerCommand ManageBookmarksCommand { get; set; }

        public ScriptplayerCommand AddBookmarkCommand { get; set; }

        public RelayCommand<BookmarkViewModel> OpenBookmarkCommand { get; set; }

        #endregion

        private void InitializeCommands()
        {
            ManageBookmarksCommand = new ScriptplayerCommand(ManageBookmarks);

            OpenBookmarkCommand = new RelayCommand<BookmarkViewModel>(ExecuteOpenBookmark);

            AddBookmarkCommand = new ScriptplayerCommand(AddBookmark, CanAddBookmark);

            SetTimeDisplayModeCommand = new RelayCommand<TimeDisplayMode>(SetTimeDisplayMode);

            SetShowTimeLeftCommand = new RelayCommand<bool>(SetShowTimeLeft);

            ShiftScriptCommand = new ScriptplayerCommand(ShiftScript, IsScriptLoaded);

            TrimScriptCommand = new ScriptplayerCommand(TrimScript, IsScriptLoaded);

            ShowGeneratorSettingsCommand = new ScriptplayerCommand(ModifyGeneratorSettings);

            EditMetadataCommand = new ScriptplayerCommand(EditMetadata, IsScriptLoaded);

            ShowGeneratorProgressCommand = new ScriptplayerCommand(ShowGeneratorProgress);

            IncreaseHandyStrokeLengthCommand = new ScriptplayerCommand(IncreaseHandyStrokeLength)
            {
                CommandId = "IncreaseHandyStrokeLength",
                DisplayText = "Increase Handy Stroke Length"
            };

            DecreaseHandyStrokeLengthCommand = new ScriptplayerCommand(DecreaseHandyStrokeLength)
            {
                CommandId = "DecreaseHandyStrokeLength",
                DisplayText = "Decrease Handy Stroke Length"
            };

            SaveScriptAsCommand = new ScriptplayerCommand(SaveScriptAs, IsScriptLoaded)
            {
                CommandId = "SaveScriptAs",
                DisplayText = "Save Script As"
            };

            ExportHandyCsvCommand = new ScriptplayerCommand(ExportHandyCsv, IsScriptLoaded)
            {
                CommandId = "ExportHandyCsv",
                DisplayText = "Export as Handy CSV"
            };

            ShowSettingsCommand = new ScriptplayerCommand(ShowSettings)
            {
                CommandId = "ShowSettings",
                DisplayText = "Show Settings"
            };

            ShowDeviceManagerCommand = new ScriptplayerCommand(ShowDeviceManager)
            {
                CommandId = "ShowDeviceManager",
                DisplayText = "Show Device Manager"
            };

            GenerateThumbnailsForLoadedVideoCommand = new ScriptplayerCommand(GenerateThumbnailsForLoadedVideo,
                IsVideoLoaded)
            {
                CommandId = "GenerateThumbnailsForLoadedVideo",
                DisplayText = "Generate Thumbnails for loaded Video"
            };

            GenerateThumbnailBannerForLoadedVideoCommand = new ScriptplayerCommand(GenerateThumbnailBannerForLoadedVideo, IsVideoLoaded)
            {
                CommandId = "GenerateThumbnailBannerForLoadedVideo",
                DisplayText = "Generate Thumbnail Banner for loaded Video"
            };

            GeneratePreviewForLoadedVideoCommand = new ScriptplayerCommand(GeneratePreviewForLoadedVideo, IsVideoLoaded)
            {
                CommandId = "GeneratePreviewForLoadedVideo",
                DisplayText = "Generate Preview GIF for loaded Video"
            };

            GenerateHeatmapForLoadedVideoCommand = new ScriptplayerCommand(GenerateHeatmapForLoadedVideo, IsVideoLoaded)
            {
                CommandId = "GenerateHeatmapForLoadedVideo",
                DisplayText = "Generate Heatmap for loaded Video"
            };

            GenerateAllForLoadedVideoCommand = new ScriptplayerCommand(GenerateAllForLoadedVideo, IsVideoLoaded)
            {
                CommandId = "GenerateAllForLoadedVideo",
                DisplayText = "Generate All for loaded Video"
            };

            OpenScriptCommand = new ScriptplayerCommand(OpenScript)
            {
                CommandId = "OpenScriptFile",
                DisplayText = "Open Script"
            };

            OpenVideoCommand = new ScriptplayerCommand(OpenVideo)
            {
                CommandId = "OpenVideoFile",
                DisplayText = "Open Video"
            };

            AddScriptsToPlaylistCommand = new ScriptplayerCommand(AddFilesToPlaylist)
            {
                CommandId = "AddFileToPlaylist",
                DisplayText = "Add File To Playlist (last)"
            };

            AddScriptsToPlaylistFirstCommand = new ScriptplayerCommand(AddFilesToPlaylistFirst)
            {
                CommandId = "AddFileToPlaylist",
                DisplayText = "Add File To Playlist (first)"
            };

            AddFolderToPlaylistCommand = new ScriptplayerCommand(() => AddFolderToPlaylist(false))
            {
                CommandId = "AddFolderToPlaylist",
                DisplayText = "Add Folder To Playlist (last)"
            };

            AddFolderToPlaylistFirstCommand = new ScriptplayerCommand(() => AddFolderToPlaylist(true))
            {
                CommandId = "AddFolderToPlaylistFirst",
                DisplayText = "Add Folder To Playlist (first)"
            };

            //ConnectLaunchDirectlyCommand = new ScriptplayerCommand(ConnectLaunchDirectly)
            //{
            //    CommandId = "ConnectLaunchDirectly",
            //    DisplayText = "Connect Launch Directly"
            //};

            ConnectHandyDirectlyCommand = new ScriptplayerCommand(ConnectHandyDirectly)
            {
                CommandId = "ConnectHandyDirectly",
                DisplayText = "Connect to Handy directly",
            };

            AddEstimAudioCommand = new ScriptplayerCommand(AddEstimAudioDevice)
            {
                CommandId = "AddEstimAudioDevice",
                DisplayText = "Add E-Stim Audio Device"
            };

            AddFunstimAudioCommand = new ScriptplayerCommand(AddFunstimAudioDevice)
            {
                CommandId = "AddFunstimAudioDevice",
                DisplayText = "Add Funstim Audio Device"
            };

            AddMK312WifiCommand = new ScriptplayerCommand(AddMK312WifiDevice)
            {
                CommandId = "AddMK312WifiDevice",
                DisplayText = "Add MK312 Wifi Device"
            };

            AddMK312SerialCommand = new ScriptplayerCommand(AddMK312SerialDevice)
            {
                CommandId = "AddMK312SerialDevice",
                DisplayText = "Add MK312 Serial Device"
            };

            ConnectButtplugCommand = new ScriptplayerCommand(ConnectButtplug)
            {
                CommandId = "ConnectButtplug",
                DisplayText = "Connect Buttplug"
            };

            DisconnectButtplugCommand = new ScriptplayerCommand(DisconnectButtplug)
            {
                CommandId = "DisconnectButtplug",
                DisplayText = "Disconnect Buttplug"
            };

            StartScanningButtplugCommand = new ScriptplayerCommand(StartScanningButtplug)
            {
                CommandId = "StartScanningButtplug",
                DisplayText = "Start Scanning Buttplug"
            };

            SkipToNextEventCommand = new ScriptplayerCommand(SkipToNextEvent, CanSkipToNextEvent)
            {
                CommandId = "SkipToNextEvent",
                DisplayText = "Skip To Next Event"
            };

            TogglePlaybackCommand = new ScriptplayerCommand(TogglePlayback, CanTogglePlayback)
            {
                CommandId = "TogglePlayback",
                DisplayText = "Toggle Play / Pause",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.Space, ModifierKeys.None),
                    GlobalCommandManager.GetShortcut(Key.MediaPlayPause, ModifierKeys.None, true)
                }
            };

            VolumeUpCommand = new ScriptplayerCommand(VolumeUp)
            {
                CommandId = "VolumeUp",
                DisplayText = "Volume Up",
                DefaultShortCuts = { GlobalCommandManager.GetShortcut(Key.Up, ModifierKeys.None) }
            };

            VolumeDownCommand = new ScriptplayerCommand(VolumeDown)
            {
                CommandId = "VolumeDown",
                DisplayText = "Volume Down",
                DefaultShortCuts = { GlobalCommandManager.GetShortcut(Key.Down, ModifierKeys.None) }
            };

            ExecuteSelectedTestPatternCommand = new ScriptplayerCommand(ExecuteSelectedTestPattern, CanExecuteSelectedTestPattern);
            ToggleFullScreenCommand = new ScriptplayerCommand(ExecuteToggleFullScreen)
            {
                CommandId = "ToggleFullscreen",
                DisplayText = "Toggle Fullscreen",
                DefaultShortCuts = { GlobalCommandManager.GetShortcut(Key.Enter, ModifierKeys.None) }
            };

            ReloadScriptCommand = new ScriptplayerCommand(ReloadScript)
            {
                CommandId = "ReloadScript",
                DisplayText = "Reload Script"
            };

            LoadPlaylistCommand = new ScriptplayerCommand(ExecuteLoadPlaylist);
            SavePlaylistCommand = new ScriptplayerCommand(ExecuteSavePlaylist);
            RemoveMissingEntriesFromPlaylistCommand = new ScriptplayerCommand(ExecuteRemoveMissingEntriesFromPlaylist);
            RemoveIncompleteEntriesFromPlaylistCommand = new ScriptplayerCommand(ExecuteRemoveIncompleteEntriesFromPlaylist);

            SetLoopACommand = new ScriptplayerCommand(ExecuteSetLoopA);
            SetLoopBCommand = new ScriptplayerCommand(ExecuteSetLoopB);
            ClearLoopCommand = new ScriptplayerCommand(ExecuteClearLoop);

            GlobalCommandManager.RegisterCommand(OpenScriptCommand);
            GlobalCommandManager.RegisterCommand(OpenVideoCommand);
            GlobalCommandManager.RegisterCommand(AddScriptsToPlaylistCommand);
            //GlobalCommandManager.RegisterCommand(ConnectLaunchDirectlyCommand);
            GlobalCommandManager.RegisterCommand(ConnectHandyDirectlyCommand);
            GlobalCommandManager.RegisterCommand(ConnectButtplugCommand);
            GlobalCommandManager.RegisterCommand(DisconnectButtplugCommand);
            GlobalCommandManager.RegisterCommand(StartScanningButtplugCommand);
            GlobalCommandManager.RegisterCommand(SkipToNextEventCommand);
            GlobalCommandManager.RegisterCommand(TogglePlaybackCommand);
            GlobalCommandManager.RegisterCommand(VolumeUpCommand);
            GlobalCommandManager.RegisterCommand(VolumeDownCommand);
            GlobalCommandManager.RegisterCommand(ToggleFullScreenCommand);
            GlobalCommandManager.RegisterCommand(IncreaseHandyStrokeLengthCommand);
            GlobalCommandManager.RegisterCommand(DecreaseHandyStrokeLengthCommand);
            GlobalCommandManager.RegisterCommand(ReloadScriptCommand);

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(ToggleCommandSourceVideoPattern)
            {
                CommandId = "ToggleCommandSourceVideoPattern",
                DisplayText = "Toggle Source Video/Pattern"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(ToggleCommandSourceVideoNone)
            {
                CommandId = "ToggleCommandSourceVideoNone",
                DisplayText = "Toggle Source Video/None",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.S, ModifierKeys.None),
                    GlobalCommandManager.GetShortcut(Key.MediaStop, ModifierKeys.None, true)
                }
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(SetCommandSourceToNone)
            {
                CommandId = "SetCommandSourceToNone",
                DisplayText = "Set Command Source None"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(SetCommandSourceToVideo)
            {
                CommandId = "SetCommandSourceToVideo",
                DisplayText = "Set Command Source Video"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(SetCommandSourceToPattern)
            {
                CommandId = "SetCommandSourceToPattern",
                DisplayText = "Set Command Source Pattern"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(SetCommandSourceToRandom)
            {
                CommandId = "SetCommandSourceToRandom",
                DisplayText = "Set Command Source Random"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(NextPattern)
            {
                CommandId = "NextPattern",
                DisplayText = "Next Pattern"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(PreviousPattern)
            {
                CommandId = "PreviousPattern",
                DisplayText = "Previous Pattern"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(IncreaseUpperRange)
            {
                CommandId = "IncreaseUpperRange",
                DisplayText = "Increase Upper Range"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(DecreaseUpperRange)
            {
                CommandId = "DecreaseUpperRange",
                DisplayText = "Decrease Upper Range"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(IncreaseLowerRange)
            {
                CommandId = "IncreaseLowerRange",
                DisplayText = "Increase Lower Range"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(DecreaseLowerRange)
            {
                CommandId = "DecreaseLowerRange",
                DisplayText = "Decrease Lower Range"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(IncreasePlaybackSpeed)
            {
                CommandId = "IncreasePlaybackRate",
                DisplayText = "Increase Playback Rate"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(DecreasePlaybackSpeed)
            {
                CommandId = "DecreasePlaybackRate",
                DisplayText = "Decrease Playback Rate"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(IncreaseFilterRange)
            {
                CommandId = "IncreaseFilterRange",
                DisplayText = "Increase Filter Range"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(DecreaseFilterRange)
            {
                CommandId = "DecreaseFilterRange",
                DisplayText = "Decrease Filter Range"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(IncreaseScriptDelay)
            {
                CommandId = "IncreaseScriptDelay",
                DisplayText = "Increase Script Delay",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.OemPlus, ModifierKeys.None),
                    GlobalCommandManager.GetShortcut(Key.Add, ModifierKeys.None)
                }
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(DecreaseScriptDelay)
            {
                CommandId = "DecreaseScriptDelay",
                DisplayText = "Decrease Script Delay",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.OemMinus, ModifierKeys.None),
                    GlobalCommandManager.GetShortcut(Key.Subtract, ModifierKeys.None)
                }
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(IncreaseAudioDelay)
            {
                CommandId = "IncreaseAudioDelay",
                DisplayText = "Increase Audio Delay",
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(DecreaseAudioDelay)
            {
                CommandId = "DecreaseAudioDelay",
                DisplayText = "Decrease Audio Delay",
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(IncreasePatternSpeed)
            {
                CommandId = "IncreasePatternSpeed",
                DisplayText = "Increase Pattern Speed",
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(DecreasePatternSpeed)
            {
                CommandId = "DecreasePatternSpeed",
                DisplayText = "Decrease Pattern Speed",
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(IncreaseMinSpeed)
            {
                CommandId = "IncreaseMinSpeed",
                DisplayText = "Increase Min Speed",
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(DecreaseMinSpeed)
            {
                CommandId = "DecreaseMinSpeed",
                DisplayText = "Decrease Min Speed",
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(IncreaseMaxSpeed)
            {
                CommandId = "IncreaseMaxSpeed",
                DisplayText = "Increase Max Speed",
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(DecreaseMaxSpeed)
            {
                CommandId = "DecreaseMaxSpeed",
                DisplayText = "Decrease Max Speed",
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(IncreaseRangeExtender)
            {
                CommandId = "IncreaseRangeExtender",
                DisplayText = "Increase Range Extender",
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(DecreaseRangeExtender)
            {
                CommandId = "DecreaseRangeExtender",
                DisplayText = "Decrease Range Extender",
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(Play, CanTogglePlayback)
            {
                CommandId = "Play",
                DisplayText = "Play",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.Play, ModifierKeys.None, true)
                }
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(Pause, CanTogglePlayback)
            {
                CommandId = "Pause",
                DisplayText = "Pause",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.Pause, ModifierKeys.None, true)
                }
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(() => { OnRequestSetFullscreen(false); })
            {
                CommandId = "ExitFullscreen",
                DisplayText = "Exit Fullscreen",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.Escape, ModifierKeys.None),
                }
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(() => { OnRequestSetFullscreen(true); })
            {
                CommandId = "EnterFullscreen",
                DisplayText = "Enter Fullscreen",
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(() => { ShiftPosition(TimeSpan.FromSeconds(-5)); })
            {
                CommandId = "Seek[-5]",
                DisplayText = "Go back 5 seconds",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.Left, ModifierKeys.None),
                }
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(() => { ShiftPosition(TimeSpan.FromSeconds(5)); })
            {
                CommandId = "Seek[5]",
                DisplayText = "Go forwards 5 seconds",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.Right, ModifierKeys.None),
                }
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(Mute)
            {
                CommandId = "Mute",
                DisplayText = "Mute",
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(() => { Playlist.PlayNextEntry(); })
            {
                CommandId = "PlayNextEntry",
                DisplayText = "Play next playlist entry",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.PageDown, ModifierKeys.None),
                    GlobalCommandManager.GetShortcut(Key.MediaNextTrack, ModifierKeys.None, true)
                }
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(() => { Playlist.PlayPreviousEntry(); })
            {
                CommandId = "PlayPreviousEntry",
                DisplayText = "Play previous playlist entry",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.PageUp, ModifierKeys.None),
                    GlobalCommandManager.GetShortcut(Key.MediaPreviousTrack, ModifierKeys.None, true)
                }
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(() => { MoveRemoveNext(false, false); })
            {
                CommandId = "MoveCurrentToDefault",
                DisplayText = "Move to default folder"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(() => { MoveRemoveNext(true, false); })
            {
                CommandId = "MoveCurrentToDefaultRemoveFromPlaylist",
                DisplayText = "Move to default folder, remove from playlist"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(() => { MoveRemoveNext(true, true); })
            {
                CommandId = "MoveCurrentToDefaultRemoveFromPlaylistPlayNext",
                DisplayText = "Move to default folder, remove from playlist, play next"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(MoveToRecycleBin)
            {
                CommandId = "MoveCurrentToRecycleBinPlayNext",
                DisplayText = "Move to recycle bin, play next"
            });

            InitializeActions();
        }

        private bool CanAddBookmark()
        {
            return LoadedMedia != null;
        }

        private void AddBookmark()
        {
            //TODO RequestEditBookmark (values)
        }

        private void ExecuteOpenBookmark(BookmarkViewModel obj)
        {
            //TODO pass initial position
            LoadFile(obj.FilePath);
        }

        private void ManageBookmarks()
        {
            //TODO RequestOpenBookmakrsmanager
        }

        private void IncreaseRangeExtender()
        {
            SetRangeExtenderAction(new ArInt(5, true));
        }


        private void DecreaseRangeExtender()
        {
            SetRangeExtenderAction(new ArInt(-5, true));
        }
        
        private void InitializeActions()
        {
            GlobalCommandManager.RegisterAction(new ScriptPlayerDelegateAction("OpenFile", new Func<string, ActionResult>(OpenFileAction)));
            GlobalCommandManager.RegisterAction(new ScriptPlayerDelegateAction("Seek", new Func<ArTimeSpan, ActionResult>(SeekAction)));
            GlobalCommandManager.RegisterAction(new ScriptPlayerDelegateAction("SetRange", new Func<byte, byte, ActionResult>(SetRangeAction)));
            GlobalCommandManager.RegisterAction(new ScriptPlayerDelegateAction("SetUpperRange", new Func<byte, ActionResult>(SetUpperRangeAction)));
            GlobalCommandManager.RegisterAction(new ScriptPlayerDelegateAction("SetLowerRange", new Func<byte, ActionResult>(SetLowerRangeAction)));
            GlobalCommandManager.RegisterAction(new ScriptPlayerDelegateAction("SetRangeExtender", new Action<ArInt>(SetRangeExtenderAction)));
            GlobalCommandManager.RegisterAction(new ScriptPlayerDelegateAction("SetPatternSpeed", new Action<ArInt>(SetPatternSpeedAction)));
        }

        private void SetPatternSpeedAction(ArInt value)
        {
            int newValue = value.Adjust((int) Settings.PatternSpeed.TotalMilliseconds, 100, 1000);

            Settings.PatternSpeed = TimeSpan.FromMilliseconds(newValue);

            OsdShowMessage("Pattern Speed: " + Settings.PatternSpeed.TotalMilliseconds.ToString("F0") + " ms / command", TimeSpan.FromSeconds(2), "PatternSpeed");
        }

        private void DecreasePatternSpeed()
        {
            SetPatternSpeedAction(new ArInt(25, true));
        }

        private void IncreasePatternSpeed()
        {
            SetPatternSpeedAction(new ArInt(-25, true));
        }
        
        private void SetRangeExtenderAction(ArInt value)
        {
            int newValue;

            if (value.IsRelative)
                newValue = Settings.RangeExtender + value.Value;
            else
                newValue = value.Value;

            newValue = Math.Min(99, Math.Max(0, newValue));

            Settings.RangeExtender = newValue;
        }

        private ActionResult SetRangeAction(byte min, byte max)
        {
            if (min > 100 || max > 100 || min > max)
                return new ActionResult(false, "Invalid parameter value (out of range 0-100)");

            if (min == 100)
                min = 99;

            if (max == 100)
                max = 99;

            Settings.MinPosition = min;
            Settings.MaxPosition = max;

            return new ActionResult(true, "Range set");
        }

        private ActionResult SetUpperRangeAction(byte value)
        {
            if (value > 100)
                return new ActionResult(false, "Invalid parameter value (out of range 0-100)");

            if (value == 100)
                value = 99;

            Settings.MaxPosition = value;

            return new ActionResult(true, "Upper range set");
        }

        private ActionResult SetLowerRangeAction(byte value)
        {
            if (value > 100)
                return new ActionResult(false, "Invalid parameter value (out of range 0-100)");

            if (value == 100)
                value = 99;

            Settings.MinPosition = value;

            return new ActionResult(true, "Lower range set");
        }

        private ActionResult SeekAction(ArTimeSpan value)
        {
            TimeSpan newValue;
            if (value.IsRelative)
                newValue = TimeSource.Progress + value.Value;
            else
                newValue = value.Value;

            if (newValue < TimeSpan.Zero)
                newValue = TimeSpan.Zero;

            if (newValue > TimeSource.Duration)
                newValue = TimeSource.Duration;

            Seek(newValue, 1);

            return new ActionResult(true, "Seeking");
        }

        private ActionResult OpenFileAction(string filename)
        {
            if (!File.Exists(filename))
                return new ActionResult(false, "File doesn't exist");

            LoadFile(filename);

            return new ActionResult(true, "Loading file");
        }

        private void InitializePlaylistCommands(PlaylistViewModel playlist)
        {
            GlobalCommandManager.RegisterCommand(playlist.RemoveSelectedEntryCommand);
        }
    }

    public class ArTimeSpan : ArValue<TimeSpan>
    {
        protected override bool TryParseInternal(string arg, bool negate, out TimeSpan parsedValue)
        {
            parsedValue = TimeSpan.Zero;

            if (string.IsNullOrWhiteSpace(arg))
                return false;

            TimeSpan result;

            if (long.TryParse(arg, NumberStyles.None, CultureInfo.InvariantCulture, out long ms))
                result = TimeSpan.FromMilliseconds(ms);
            else
            {
                var formats = new[]
                {
                    "h\\:mm\\:ss\\.fff",
                    "h\\:mm\\:ss\\.ff",
                    "h\\:mm\\:ss\\.f",
                    "h\\:mm\\:ss",

                    "m\\:ss\\.fff",
                    "m\\:ss\\.ff",
                    "m\\:ss\\.f",
                    "m\\:ss"
                };

                if (TimeSpan.TryParseExact(arg, formats, CultureInfo.InvariantCulture, out TimeSpan ts1))
                    result = ts1;
                else
                {
                    return false;
                }
            }

            if (negate)
            {
                parsedValue = result.Negate();
            }
            else
            {
                parsedValue = result;
            }

            return true;
        }
    }

    public class ArInt : ArValue<int>
    {
        protected override bool TryParseInternal(string value, bool negate, out int parsedValue)
        {
            bool success = int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out parsedValue);

            if (success && negate)
                parsedValue = -parsedValue;

            return success;
        }


        public ArInt()
        { }

        public ArInt(int value, bool isRelative)
        {
            Value = value;
            IsRelative = isRelative;
        }

        public int Adjust(int baseValue, int min, int max)
        {
            int newValue = IsRelative ? baseValue + Value : Value;
            return Math.Min(max, Math.Max(min, newValue));
        }
    }

    public abstract class ArValue<T>
    {
        public bool IsRelative { get; protected set; }

        public T Value { get; protected set; }

        public bool TryParse(string value)
        {
            bool isNegative = false;
            IsRelative = false;

            if (!string.IsNullOrEmpty(value))
            {
                if (value.StartsWith("-"))
                {
                    isNegative = true;
                    IsRelative = true;
                    value = value.Substring(1);
                }
                else if (value.StartsWith("+"))
                {
                    IsRelative = true;
                    value = value.Substring(1);
                }
            }
            
            bool success = TryParseInternal(value, isNegative, out T parsedValue);
            Value = success ? parsedValue : default(T);
            return success;
        }

        protected abstract bool TryParseInternal(string value, bool negate, out T parsedValue);
    }

    public class ScriptPlayerDelegateAction : ScriptPlayerAction
    {
        public override string Name { get; }
        private readonly Delegate _action;

        public ScriptPlayerDelegateAction(string name, Delegate action)
        {
            if (action.Method.ReturnType != typeof(void) && action.Method.ReturnType != typeof(string))
            {
                if (!typeof(ActionResult).IsAssignableFrom(action.Method.ReturnType))
                    throw new Exception(action.Method.Name + " doesn't return a " + nameof(ActionResult) + ", string or void");
            }

            Name = name;
            _action = action;
        }

        public override ActionResult Execute(string[] parameters)
        {
            try
            {
                ParameterInfo[] methodParams = _action.Method.GetParameters();
                object[] invokationParameters = new object[methodParams.Length];

                for (int i = 0; i < methodParams.Length; i++)
                {
                    if (parameters.Length > i)
                    {
                        object parameterValue = ConvertParameter(parameters[i], methodParams[i].ParameterType,
                            out bool success);
                        if (success)
                            invokationParameters[i] = parameterValue;
                        else
                        {
                            return new ActionResult(false, $"Parameter {i} couln't be parsed");
                        }
                    }
                    else
                    {
                        object defaultValue = GetDefaultValue(methodParams[i], out bool hasDefault);
                        if (hasDefault)
                            invokationParameters[i] = defaultValue;
                        else
                        {
                            return new ActionResult(false, $"Parameter {i} is missing");
                        }
                    }
                }

                if (_action.Method.ReturnType == typeof(void))
                {
                    _action.DynamicInvoke(invokationParameters);
                    return new ActionResult(true, "Success");
                }

                if (_action.Method.ReturnType == typeof(string))
                {
                    string message = (string)_action.DynamicInvoke(invokationParameters);
                    return new ActionResult(true, message);
                }

                return (ActionResult)_action.DynamicInvoke(invokationParameters);
            }
            catch(Exception ex)
            {
                return new ActionResult(false, ex.Message);
            }
        }

        private object GetDefaultValue(ParameterInfo methodParam, out bool hasDefault)
        {
            if (methodParam.HasDefaultValue)
            {
                hasDefault = true;
                return methodParam.DefaultValue;
            }
            
            hasDefault = false;
            return null;
        }

        private object ConvertParameter(string value, Type targetType, out bool success)
        {
            success = true;

            try
            {

                if (typeof(IConvertible).IsAssignableFrom(targetType))
                {
                    return Convert.ChangeType(value, targetType);
                }

                if (targetType == typeof(string))
                {
                    return value;
                }

                if (targetType == typeof(ArTimeSpan))
                {
                    ArTimeSpan result = new ArTimeSpan();
                    if (!result.TryParse(value))
                    {
                        success = false;
                        return null;
                    }

                    return result;
                }

                if (targetType == typeof(ArInt))
                {
                    ArInt result = new ArInt();
                    if (!result.TryParse(value))
                    {
                        success = false;
                        return null;
                    }

                    return result;
                }
            }
            catch
            {
                //
            }

            success = false;
            return false;
        }
    }
}
