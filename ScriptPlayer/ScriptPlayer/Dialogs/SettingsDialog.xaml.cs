using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JetBrains.Annotations;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using NAudio.Wave;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Controls;
using ScriptPlayer.Shared.Scripts;
using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog : Window
    {
        public static readonly DependencyProperty FilterProperty = DependencyProperty.Register(
            "Filter", typeof(string), typeof(SettingsDialog), new PropertyMetadata(default(string), OnFilterPropertyChanged));

        private static void OnFilterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SettingsDialog)d).UpdateFilter();
        }

        public string Filter
        {
            get => (string)GetValue(FilterProperty);
            set => SetValue(FilterProperty, value);
        }

        public static readonly DependencyProperty AudioDevicesProperty = DependencyProperty.Register(
            "AudioDevices", typeof(List<AudioDeviceVm>), typeof(SettingsDialog), new PropertyMetadata(default(List<AudioDeviceVm>)));

        public List<AudioDeviceVm> AudioDevices
        {
            get => (List<AudioDeviceVm>) GetValue(AudioDevicesProperty);
            set => SetValue(AudioDevicesProperty, value);
        }

        public static readonly DependencyProperty SelectedAudioDeviceProperty = DependencyProperty.Register(
            "SelectedAudioDevice", typeof(AudioDeviceVm), typeof(SettingsDialog), new PropertyMetadata(default(AudioDeviceVm)));

        public AudioDeviceVm SelectedAudioDevice
        {
            get => (AudioDeviceVm) GetValue(SelectedAudioDeviceProperty);
            set => SetValue(SelectedAudioDeviceProperty, value);
        }

        public static readonly DependencyProperty InputMappingsProperty = DependencyProperty.Register(
            "InputMappings", typeof(ObservableCollection<InputMappingViewModel>), typeof(SettingsDialog), new PropertyMetadata(default(ObservableCollection<InputMappingViewModel>)));

        public ObservableCollection<InputMappingViewModel> InputMappings
        {
            get => (ObservableCollection<InputMappingViewModel>)GetValue(InputMappingsProperty);
            set => SetValue(InputMappingsProperty, value);
        }

        public static readonly DependencyProperty PagesProperty = DependencyProperty.Register(
            "Pages", typeof(SettingsPageViewModelCollection), typeof(SettingsDialog), new PropertyMetadata(default(SettingsPageViewModelCollection)));

        public SettingsPageViewModelCollection Pages
        {
            get => (SettingsPageViewModelCollection)GetValue(PagesProperty);
            set => SetValue(PagesProperty, value);
        }

        public static readonly DependencyProperty FilteredPagesProperty = DependencyProperty.Register(
            "FilteredPages", typeof(SettingsPageViewModelCollection), typeof(SettingsDialog), new PropertyMetadata(default(SettingsPageViewModelCollection)));

        public SettingsPageViewModelCollection FilteredPages
        {
            get => (SettingsPageViewModelCollection)GetValue(FilteredPagesProperty);
            set => SetValue(FilteredPagesProperty, value);
        }

        public static readonly DependencyProperty SelectedAdditionalPathProperty = DependencyProperty.Register(
            "SelectedAdditionalPath", typeof(AdditionalPathView), typeof(SettingsDialog), new PropertyMetadata(default(AdditionalPathView)));

        public AdditionalPathView SelectedAdditionalPath
        {
            get => (AdditionalPathView)GetValue(SelectedAdditionalPathProperty);
            set => SetValue(SelectedAdditionalPathProperty, value);
        }

        public static readonly DependencyProperty SelectedFavouriteFolderProperty = DependencyProperty.Register(
            "SelectedFavouriteFolder", typeof(FavouriteFolder), typeof(SettingsDialog), new PropertyMetadata(default(FavouriteFolder)));

        public FavouriteFolder SelectedFavouriteFolder
        {
            get => (FavouriteFolder) GetValue(SelectedFavouriteFolderProperty);
            set => SetValue(SelectedFavouriteFolderProperty, value);
        }

        public static readonly DependencyProperty LocalAdressesProperty = DependencyProperty.Register(
            "LocalAdresses", typeof(List<string>), typeof(SettingsDialog), new PropertyMetadata(default(List<string>)));

        public List<string> LocalAdresses
        {
            get => (List<string>) GetValue(LocalAdressesProperty);
            set => SetValue(LocalAdressesProperty, value);
        }

        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(
            "Settings", typeof(SettingsViewModel), typeof(SettingsDialog), new PropertyMetadata(default(SettingsViewModel)));

        public SettingsViewModel Settings
        {
            get => (SettingsViewModel)GetValue(SettingsProperty);
            set => SetValue(SettingsProperty, value);
        }

        public static readonly DependencyProperty SelectedPageProperty = DependencyProperty.Register(
            "SelectedPage", typeof(SettingsPageViewModel), typeof(SettingsDialog), new PropertyMetadata(default(SettingsPageViewModel), OnSelectedPageChanged, CoerceSelectedPage));

        private static object CoerceSelectedPage(DependencyObject d, object basevalue)
        {
            SettingsDialog dialog = (SettingsDialog)d;
            SettingsPageViewModel page = (SettingsPageViewModel)basevalue;

            if (page == null && dialog.FilteredPages.Count > 0)
                return dialog.FilteredPages.First();

            return basevalue;
        }

        private static void OnSelectedPageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SettingsDialog)d).SelectedPageHasChanged();
        }

        private void SelectedPageHasChanged()
        {
            UpdatePageTitle();
        }

        public static readonly DependencyProperty AdditionalPathsProperty = DependencyProperty.Register(
            "AdditionalPaths", typeof(ObservableCollection<AdditionalPathView>), typeof(SettingsDialog), new PropertyMetadata(default(ObservableCollection<AdditionalPathView>)));

        public ObservableCollection<AdditionalPathView> AdditionalPaths
        {
            get { return (ObservableCollection<AdditionalPathView>) GetValue(AdditionalPathsProperty); }
            set { SetValue(AdditionalPathsProperty, value); }
        }

        private void UpdatePageTitle()
        {
            SettingsTitle = BuildSettingsPath();
            _lastSelected = SelectedPage?.SettingsId;
        }

        public SettingsPageViewModel SelectedPage
        {
            get => (SettingsPageViewModel)GetValue(SelectedPageProperty);
            set => SetValue(SelectedPageProperty, value);
        }

        public static readonly DependencyProperty SettingsTitleProperty = DependencyProperty.Register(
            "SettingsTitle", typeof(string), typeof(SettingsDialog), new PropertyMetadata(default(string)));

        public string SettingsTitle
        {
            get => (string)GetValue(SettingsTitleProperty);
            set => SetValue(SettingsTitleProperty, value);
        }

        private static string _lastSelected;

        public SettingsDialog(SettingsViewModel initialSettings, string selectedPage = null)
        {
            Settings = initialSettings.Duplicate();
            InitializeAudio();
            InitializeLocalAddresses();

            LoadAdditionalPaths();
            
            CreateInputMappings(GlobalCommandManager.CommandMappings);
            
            InitializeComponent();

            Pages = BuildPages(PageSelector);
            SelectedPage = FindPage(selectedPage) ?? FindPage(_lastSelected) ?? Pages.FirstOrDefault();
            UpdateFilter();
        }

        private void LoadAdditionalPaths()
        {
            AdditionalPaths = new ObservableCollection<AdditionalPathView>(Settings.AdditionalPaths.Select(p => new AdditionalPathView(p)));
        }

        private void InitializeLocalAddresses()
        {
            // TODO: this isn't great but hopefully works for a lot of people?

            List<string> ips = new List<string>();
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                ips.Add(ip.ToString());
            }

            LocalAdresses = ips;
        }

        private void InitializeAudio()
        {
            List<AudioDeviceVm> audioDevices = new List<AudioDeviceVm>();

            audioDevices.Add(new AudioDeviceVm
            {
                Description = "- Disabled -",
                DeviceId = ""
            });

            for(int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var capabilities = WaveOut.GetCapabilities(i);
                if (capabilities.ProductGuid == Guid.Empty)
                    continue;

                audioDevices.Add(new AudioDeviceVm
                {
                    Description = capabilities.ProductName,
                    DeviceId = capabilities.ProductName
                });
            }

            if (audioDevices.All(a => a.Description != Settings.EstimAudioDevice))
            {
                audioDevices.Add(new AudioDeviceVm
                {
                    Description = "- UNKNOWN -",
                    DeviceId = Settings.EstimAudioDevice
                });
            }

            AudioDevices = audioDevices;
            SelectedAudioDevice = AudioDevices.FirstOrDefault(d => d.DeviceId == Settings.EstimAudioDevice);
        }

        private void CreateInputMappings(List<InputMapping> inputMappings)
        {
            ObservableCollection<InputMappingViewModel> mappings = new ObservableCollection<InputMappingViewModel>();

            foreach (ScriptplayerCommand scriptplayerCommand in GlobalCommandManager.Commands.Values)
            {
                var shortcuts = inputMappings.Where(c => c.CommandId == scriptplayerCommand.CommandId).ToList();

                if (shortcuts.Count == 0)
                {
                    InputMappingViewModel mapping = new InputMappingViewModel
                    {
                        CommandId = scriptplayerCommand.CommandId,
                        DisplayText = scriptplayerCommand.DisplayText,
                        Shortcut = "",
                        IsGlobal = false
                    };

                    mappings.Add(mapping);
                }
                else
                {
                    foreach (var shortcut in shortcuts)
                    {
                        InputMappingViewModel mapping = new InputMappingViewModel
                        {
                            CommandId = scriptplayerCommand.CommandId,
                            DisplayText = scriptplayerCommand.DisplayText,
                            Shortcut = shortcut.KeyboardShortcut,
                            IsGlobal = shortcut.IsGlobal
                        };

                        mappings.Add(mapping);
                    }
                }
            }

            InputMappings = mappings;
        }

        private void ApplyInputMappings()
        {
            GlobalCommandManager.CommandMappings.Clear();
            foreach (InputMappingViewModel mapping in InputMappings)
            {
                GlobalCommandManager.CommandMappings.Add(new InputMapping
                {
                    CommandId = mapping.CommandId,
                    KeyboardShortcut = mapping.Shortcut,
                    IsGlobal = mapping.IsGlobal
                });
            }
        }

        private SettingsPageViewModel FindPage(string pageId)
        {
            if (string.IsNullOrWhiteSpace(pageId))
                return null;

            string[] sections = pageId.Split('/');

            SettingsPageViewModel page = Pages[sections[0]];

            for (int i = 1; i < sections.Length; i++)
                page = page?.SubSettings[string.Join("/", sections.Take(i + 1))];

            return page;
        }

        private static SettingsPageViewModelCollection BuildPages(PageSelector pageSelector)
        {
            SettingsPageViewModelCollection pages = new SettingsPageViewModelCollection();

            foreach (UIElement page in pageSelector.Elements)
            {
                string id = PageSelector.GetContentIdentifier(page);
                object header = PageSelector.GetHeader(page);
                if (string.IsNullOrWhiteSpace("id")) continue;

                if (id.Contains("/"))
                {
                    string[] sections = id.Split('/');

                    if (pages[sections[0]] == null)
                    {
                        pages.Add(new SettingsPageViewModel(sections[0], sections[0]));
                    }

                    pages[sections[0]].Add(new SettingsPageViewModel(sections[1], id, header));
                }
                else
                {
                    pages.Add(new SettingsPageViewModel(id, id, header));
                }
            }

            SortPages(pages);

            return pages;
        }

        private static void SortPages(SettingsPageViewModelCollection pages)
        {
            if (pages == null) return;

            pages.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            foreach (SettingsPageViewModel page in pages)
                SortPages(page.SubSettings);
        }

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SettingsPageViewModel page = e.NewValue as SettingsPageViewModel;
            if (page == null) return;

            SelectedPage = page;
        }

        private string BuildSettingsPath()
        {
            SettingsPageViewModel current = SelectedPage;
            string result = "";

            while (current != null)
            {
                if (!string.IsNullOrWhiteSpace(result))
                    result = " - " + result;
                result = current.Name + result;
                current = current.Parent;
            }

            return result;
        }


        private void TxtPasswordVlcPassword_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            Settings.VlcPassword = ((PasswordBox)sender).Password;
        }

        private void TxtPasswordKodiPassword_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            Settings.KodiPassword = ((PasswordBox)sender).Password;
        }

        private void SettingsDialog_OnLoaded(object sender, RoutedEventArgs e)
        {
            txtPasswordVlcPassword.Password = Settings.VlcPassword;
            txtPasswordKodiPassword.Password = Settings.KodiPassword;

            GlobalCommandManager.IsEnabled = false;
        }

        private void BtnOk_OnClick(object sender, RoutedEventArgs e)
        {
            ((Button)sender).Focus();
            ApplyInputMappings();
            Settings.EstimAudioDevice = SelectedAudioDevice?.DeviceId ?? "";
            Settings.AdditionalPaths = new ObservableCollection<string>(AdditionalPaths.Select(p => p.ToPath()));
            DialogResult = true;
        }

        private void BtnRemovePath_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedAdditionalPath == null) return;

            int currentIndex = AdditionalPaths.IndexOf(SelectedAdditionalPath);
            AdditionalPaths.Remove(SelectedAdditionalPath);
            if (currentIndex >= AdditionalPaths.Count)
                currentIndex--;

            SelectedAdditionalPath = currentIndex >= 0 ? AdditionalPaths[currentIndex] : null;
        }

        private void BtnAddPath_Click(object sender, RoutedEventArgs e)
        {
            string newPath = GetDirectory();
            if (string.IsNullOrEmpty(newPath))
                return;

            if (string.IsNullOrWhiteSpace(newPath))
                return;

            if (AdditionalPaths.Any(p => p.Path == newPath))
            {
                MessageBox.Show(this, "This path has already been added.", "Path already added", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (!IsValidDirectory(newPath))
            {
                MessageBox.Show("This directoy doesn't exist or is not accessible!", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            AdditionalPaths.Add(new AdditionalPathView(newPath));
        }

        private string GetDirectory()
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                AddToMostRecentlyUsedList = true,
                AllowNonFileSystemItems = false,
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true
            };

            if (dlg.ShowDialog() != CommonFileDialogResult.Ok)
                return null;

            return dlg.FileName;
        }

        private static bool IsValidDirectory(string path)
        {
            try
            {
                if (path.EndsWith("*"))
                    return Directory.Exists(path.Substring(0, path.Length - 1));

                return Directory.Exists(path);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
        }

        private void BtnButtplugDefault_Click(object sender, RoutedEventArgs e)
        {
            Settings.ButtplugUrl = ButtplugConnectionSettings.DefaultUrl;
        }

        private void BtnVlcDefault_Click(object sender, RoutedEventArgs e)
        {
            Settings.VlcEndpoint = VlcConnectionSettings.DefaultEndpoint;
        }

        private void BtnWhirligigDefault_Click(object sender, RoutedEventArgs e)
        {
            Settings.WhirligigEndpoint = WhirligigConnectionSettings.DefaultEndpoint;
        }

        private void BtnMpcHcDefault_Click(object sender, RoutedEventArgs e)
        {
            Settings.MpcHcEndpoint = MpcTimeSource.DefaultEndpoint;
        }

        private void BtnHereSphereDefault_Click(object sender, RoutedEventArgs e)
        {
            Settings.DeoVrEndpoint = HereSphereTimeSource.DefaultEndpoint;
        }

        private void BtnSelectFallBackScript_Click(object sender, RoutedEventArgs e)
        {
            ScriptFileFormatCollection formats = ScriptLoaderManager.GetFormats();

            OpenFileDialog dialog = new OpenFileDialog { Filter = formats.BuildFilter(true) };
            if (dialog.ShowDialog(this) != true) return;

            Settings.FallbackScriptFile = dialog.FileName;
        }

        private void BtnZoomDefault_Click(object sender, RoutedEventArgs e)
        {
            Settings.ZoomPlayerEndpoint = ZoomPlayerConnectionSettings.DefaultEndpoint;
        }

        private void BtnSamsungDefault_Click(object sender, RoutedEventArgs e)
        {
            Settings.SamsungVrUdpPort = SamsungVrConnectionSettings.DefaultPort;
        }

        private void BtnReset_OnClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(this,
                    "All Settings will we reverted to their defaults. Are you sure you want to continue?",
                    "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            Settings = new SettingsViewModel();
            LoadAdditionalPaths();
        }

        private void BtnKodiDefault_Click(object sender, RoutedEventArgs e)
        {
            string tag = (string)((Button)sender).Tag;
            switch (tag)
            {
                case "IpDefault":
                    Settings.KodiIp = KodiConnectionSettings.DefaultIp;
                    break;
                case "HttpPortDefault":
                    Settings.KodiHttpPort = KodiConnectionSettings.DefaultHttpPort;
                    break;
                case "TcpPortDefault":
                    Settings.KodiTcpPort = KodiConnectionSettings.DefaultTcpPort;
                    break;
                case "UserDefault":
                    Settings.KodiUser = KodiConnectionSettings.DefaultUser;
                    break;
                case "PasswordDefault":
                    Settings.KodiPassword = KodiConnectionSettings.DefaultPassword;
                    txtPasswordKodiPassword.Password = KodiConnectionSettings.DefaultPassword;
                    break;
                default:
                    break;
            }
        }

        private void BtnEditShortCut_Click(object sender, RoutedEventArgs e)
        {
            InputMappingViewModel inputMapping = ((FrameworkElement)sender).DataContext as InputMappingViewModel;

            ShortcutInputDialog dialog = new ShortcutInputDialog()
            {
                Owner = this,
                Shortcut = inputMapping.Shortcut
            };

            if (dialog.ShowDialog() != true)
                return;

            inputMapping.Shortcut = dialog.Shortcut;
        }

        private void BtnRemoveShortCut_Click(object sender, RoutedEventArgs e)
        {
            InputMappingViewModel inputMapping = ((FrameworkElement)sender).DataContext as InputMappingViewModel;

            if (InputMappings.Count(i => i.CommandId == inputMapping.CommandId && i != inputMapping) > 0)
            {
                InputMappings.Remove(inputMapping);
            }
            else
            {
                inputMapping.Shortcut = "";
            }
        }

        private void BtnAddShortCut_Click(object sender, RoutedEventArgs e)
        {
            InputMappingViewModel inputMapping = ((FrameworkElement)sender).DataContext as InputMappingViewModel;

            ShortcutInputDialog dialog = new ShortcutInputDialog()
            {
                Owner = this,
                Shortcut = inputMapping.Shortcut
            };

            if (dialog.ShowDialog() != true)
                return;

            InputMappingViewModel newShortcut = new InputMappingViewModel
            {
                CommandId = inputMapping.CommandId,
                DisplayText = inputMapping.DisplayText,
                IsGlobal = false,
                Shortcut = dialog.Shortcut
            };

            int currentIndex = InputMappings.IndexOf(inputMapping);
            InputMappings.Insert(currentIndex + 1, newShortcut);
        }

        private void BtnResetInputMappings_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(this, "Are you sure you want to reset all input mappings to default?", "Confirm Reset",
                    MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            CreateInputMappings(GlobalCommandManager.GetDefaultCommandMappings());
        }

        private void CommandMappingRow_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            InputMappingViewModel inputMapping = ((FrameworkElement)sender).DataContext as InputMappingViewModel;

            ShortcutInputDialog dialog = new ShortcutInputDialog()
            {
                Owner = this,
                Shortcut = inputMapping.Shortcut
            };

            if (dialog.ShowDialog() != true)
                return;

            inputMapping.Shortcut = dialog.Shortcut;
        }

        private void BtnFfmpeg_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.ffmpeg.org/download.html");
        }

        private void BtnBrowseForFfmpeg_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                FileName = "ffmpeg.exe",
                Filter = "ffmpeg.exe|ffmpeg.exe"
            };

            if (dialog.ShowDialog(this) != true)
                return;

            Settings.FfmpegPath = dialog.FileName;
        }

        private void BtnButtplugExePath_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Executable|*.exe";

            if (dialog.ShowDialog(this) != true)
                return;

            Settings.ButtplugExePath = dialog.FileName;
        }

        private void UpdateFilter()
        {
            string filter = Filter?.Trim() ?? "";

            var filteredPages = new SettingsPageViewModelCollection();

            if (string.IsNullOrEmpty(filter))
            {
                filteredPages.AddRange(Pages);
            }
            else
            {
                foreach (SettingsPageViewModel page in Pages)
                {
                    if (page.SettingsId.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        filteredPages.Add(page);
                        continue;
                    }

                    UIElement root = PageSelector.GetContent(page.SettingsId);
                    if (root == null)
                        continue;

                    if (ContainsText(root, filter))
                        filteredPages.Add(page);
                }
            }

            FilteredPages = filteredPages;
        }

        private bool ContainsText(UIElement root, string filter)
        {
            List<UIElement> children = FindVisualChildren<UIElement>(root).ToList();

            foreach (UIElement child in children)
            {
                if (child is TextBlock text)
                {
                    if (text.Text.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) >= 0)
                        return true;
                }

                if (child is CheckBox cck)
                {
                    if (cck.Content != null && cck.Content.ToString().IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) >= 0)
                        return true;
                }

                if (child is RadioButton rb)
                {
                    if (rb.Content != null && rb.Content.ToString().IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) >= 0)
                        return true;
                }
            }

            return false;
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            GlobalCommandManager.IsEnabled = true;
        }

        private void FavouriteFoldersEntry_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(((ListBoxItem) sender).DataContext is FavouriteFolder folder))
                return;

            EditFavouriteFolderDialog dialog = new EditFavouriteFolderDialog(folder){Owner = this};
            if (dialog.ShowDialog() != true)
                return;

            if (dialog.IsDefault)
            {
                foreach (FavouriteFolder fav in Settings.FavouriteFolders)
                    fav.IsDefault = false;
            }

            folder.Path = dialog.Path;
            folder.Name = dialog.FolderName;
            folder.IsDefault = dialog.IsDefault;
        }

        private void BtnAddFolder_Click(object sender, RoutedEventArgs e)
        {
            EditFavouriteFolderDialog dialog = new EditFavouriteFolderDialog() { Owner = this };
            if (dialog.ShowDialog() != true)
                return;

            if (dialog.IsDefault)
            {
                foreach (FavouriteFolder fav in Settings.FavouriteFolders)
                    fav.IsDefault = false;
            }

            Settings.FavouriteFolders.Add(new FavouriteFolder{
                Path = dialog.Path,
                Name = dialog.FolderName,
                IsDefault = dialog.IsDefault
            });

            CheckFavouriteDefault();
        }

        private void BtnRemoveFolder_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFavouriteFolder == null) return;

            int currentIndex = Settings.FavouriteFolders.IndexOf(SelectedFavouriteFolder);
            Settings.FavouriteFolders.Remove(SelectedFavouriteFolder);
            if (currentIndex >= Settings.FavouriteFolders.Count)
                currentIndex--;

            SelectedFavouriteFolder = currentIndex >= 0 ? Settings.FavouriteFolders[currentIndex] : null;

            CheckFavouriteDefault();
        }

        private void CheckFavouriteDefault()
        {
            if (Settings.FavouriteFolders.Count == 0)
                return;

            if (Settings.FavouriteFolders.Any(f => f.IsDefault))
                return;

            Settings.FavouriteFolders.First().IsDefault = true;
        }

        private void MnuCopyCommandId_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string commandId = ((InputMappingViewModel) ((MenuItem) sender).DataContext).CommandId;
                Clipboard.SetText(commandId);
            }
            catch
            {
                //
            }
        }
    }

    public class InputMappingViewModel : INotifyPropertyChanged
    {
        private string _shortcut;
        private bool _isGlobal;

        public string DisplayText { get; set; }

        public string CommandId { get; set; }

        public string Shortcut
        {
            get => _shortcut;
            set
            {
                if (value == _shortcut) return;
                _shortcut = value;
                OnPropertyChanged();
            }
        }

        public bool IsGlobal
        {
            get => _isGlobal;
            set
            {
                if (value == _isGlobal) return;
                _isGlobal = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SettingsPageViewModelCollection : List<SettingsPageViewModel>
    {
        public SettingsPageViewModel this[string id] => this.FirstOrDefault(s => s.SettingsId == id);
    }

    public class SettingsPageViewModel
    {
        public SettingsPageViewModel()
        { }

        public SettingsPageViewModel(string name, string id, object header = null)
        {
            Name = name;
            SettingsId = id;
            Header = header;
        }

        public string Name { get; set; }
        public object Header { get; set; }
        public SettingsPageViewModelCollection SubSettings { get; set; }
        public string SettingsId { get; set; }
        public SettingsPageViewModel Parent { get; set; }
        public void Add(SettingsPageViewModel subSetting)
        {
            if (SubSettings == null)
                SubSettings = new SettingsPageViewModelCollection();
            subSetting.Parent = this;
            SubSettings.Add(subSetting);
        }
    }

    public class AudioDeviceVm
    {
        public string DeviceId { get; set; }
        public string Description { get; set; }
    }

    public class AdditionalPathView : INotifyPropertyChanged
    {
        private bool _includeSubDirectories;
        private string _path;

        public AdditionalPathView(string s)
        {
            Path = s.TrimEnd('*');
            IncludeSubDirectories = s.EndsWith("*");
        }

        public string ToPath()
        {
            return Path + (IncludeSubDirectories ? "*" : "");
        }

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

        public bool IncludeSubDirectories
        {
            get => _includeSubDirectories;
            set
            {
                if (value == _includeSubDirectories) return;
                _includeSubDirectories = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
