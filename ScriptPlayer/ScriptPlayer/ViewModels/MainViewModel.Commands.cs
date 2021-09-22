using System;
using System.Globalization;
using System.IO;
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

        public ScriptplayerCommand ConnectLaunchDirectlyCommand { get; set; }

        public ScriptplayerCommand ConnectHandyDirectlyCommand { get; set; }

        public ScriptplayerCommand AddEstimAudioCommand { get; set; }

        public ScriptplayerCommand AddFunstimAudioCommand { get; set; }

        public ScriptplayerCommand AddScriptsToPlaylistFirstCommand { get; set; }

        public ScriptplayerCommand AddScriptsToPlaylistCommand { get; set; }

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

        #endregion

        private void InitializeCommands()
        {
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

            AddFolderToPlaylistCommand = new ScriptplayerCommand(AddFolderToPlaylist)
            {
                CommandId = "AddFolderToPlaylist",
                DisplayText = "Add Folder To Playlist"
            };

            ConnectLaunchDirectlyCommand = new ScriptplayerCommand(ConnectLaunchDirectly)
            {
                CommandId = "ConnectLaunchDirectly",
                DisplayText = "Connect Launch Directly"
            };

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
            GlobalCommandManager.RegisterCommand(ConnectLaunchDirectlyCommand);
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

        private void InitializeActions()
        {
            GlobalCommandManager.RegisterAction(new ScriptPlayerAction("OpenFile", OpenFileAction));
            GlobalCommandManager.RegisterAction(new ScriptPlayerAction("Seek", SeekAction));
            GlobalCommandManager.RegisterAction(new ScriptPlayerAction("SetRange", SetRangeAction));
        }

        private ActionResult SetRangeAction(string[] args)
        {
            if(args.Length != 2)
                return new ActionResult(false, "Expected two parameters, got " + args.Length);

            if(!byte.TryParse(args[0], out byte min) || !byte.TryParse(args[1], out byte max))
                return new ActionResult(false, "Invalid parameter value (can't parse)");

            if(min > 100 ||max > 100 || min > max)
                return new ActionResult(false, "Invalid parameter value (out of range 0-100)");

            if (min == 100)
                min = 99;

            if (max == 100)
                max = 99;

            Settings.MinPosition = min;
            Settings.MaxPosition = max;

            return new ActionResult(true, "Range set");
        }

        private ActionResult SeekAction(string[] args)
        {
            if (args.Length != 1)
                return new ActionResult(false, "Expected one parameter, got " + args.Length);

            TimeSpan timespan = ParseTimespan(args[0], out bool isRelative, out bool success);

            if(!success)
                return new ActionResult(false, "Couln't parse timespan");

            if (isRelative)
                timespan += TimeSource.Progress;

            if(timespan < TimeSpan.Zero)
                timespan = TimeSpan.Zero;

            if (timespan > TimeSource.Duration)
                timespan = TimeSource.Duration;

            Seek(timespan, 1);

            return new ActionResult(true, "Seeking");
        }

        private TimeSpan ParseTimespan(string arg, out bool isRelative, out bool success)
        {
            isRelative = false;
            success = true;

            if (string.IsNullOrWhiteSpace(arg))
            {
                success = false;    
                return TimeSpan.Zero;
            }

            bool isNegative = false;

            if (arg.StartsWith("-") || arg.StartsWith("+"))
            {
                isNegative = arg.StartsWith("-");
                isRelative = true;
                arg = arg.Substring(1);
            }

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
                    success = false;
                    return TimeSpan.Zero;
                }
            }

            if (isNegative)
                return result.Negate();

            return result;
        }

        private ActionResult OpenFileAction(string[] args)
        {
            if (args.Length != 1)
                return new ActionResult(false, "Expected one parameter, got " + args.Length);

            if (!File.Exists(args[0]))
                return new ActionResult(false, "File doesn't exist");

            LoadFile(args[0]);

            return new ActionResult(true, "Loading file");
        }
    }
}
