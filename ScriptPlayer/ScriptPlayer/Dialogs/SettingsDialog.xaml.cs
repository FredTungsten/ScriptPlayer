using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using JetBrains.Annotations;
using Microsoft.Win32;
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
            get => (string) GetValue(FilterProperty);
            set => SetValue(FilterProperty, value);
        }

        public static readonly DependencyProperty InputMappingsProperty = DependencyProperty.Register(
            "InputMappings", typeof(List<InputMappingViewModel>), typeof(SettingsDialog), new PropertyMetadata(default(List<InputMappingViewModel>)));

        public List<InputMappingViewModel> InputMappings
        {
            get => (List<InputMappingViewModel>) GetValue(InputMappingsProperty);
            set => SetValue(InputMappingsProperty, value);
        }

        public static readonly DependencyProperty PagesProperty = DependencyProperty.Register(
            "Pages", typeof(SettingsPageViewModelCollection), typeof(SettingsDialog), new PropertyMetadata(default(SettingsPageViewModelCollection)));

        public SettingsPageViewModelCollection Pages
        {
            get => (SettingsPageViewModelCollection) GetValue(PagesProperty);
            set => SetValue(PagesProperty, value);
        }

        public static readonly DependencyProperty FilteredPagesProperty = DependencyProperty.Register(
            "FilteredPages", typeof(SettingsPageViewModelCollection), typeof(SettingsDialog), new PropertyMetadata(default(SettingsPageViewModelCollection)));

        public SettingsPageViewModelCollection FilteredPages
        {
            get => (SettingsPageViewModelCollection) GetValue(FilteredPagesProperty);
            set => SetValue(FilteredPagesProperty, value);
        }

        public static readonly DependencyProperty SelectedAdditionalPathProperty = DependencyProperty.Register(
            "SelectedAdditionalPath", typeof(string), typeof(SettingsDialog), new PropertyMetadata(default(string)));

        public string SelectedAdditionalPath
        {
            get => (string) GetValue(SelectedAdditionalPathProperty);
            set => SetValue(SelectedAdditionalPathProperty, value);
        }

        public static readonly DependencyProperty AdditionalPathProperty = DependencyProperty.Register(
            "AdditionalPath", typeof(string), typeof(SettingsDialog), new PropertyMetadata(default(string)));

        public string AdditionalPath
        {
            get => (string) GetValue(AdditionalPathProperty);
            set => SetValue(AdditionalPathProperty, value);
        }

        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(
            "Settings", typeof(SettingsViewModel), typeof(SettingsDialog), new PropertyMetadata(default(SettingsViewModel)));

        public SettingsViewModel Settings
        {
            get => (SettingsViewModel) GetValue(SettingsProperty);
            set => SetValue(SettingsProperty, value);
        }

        public static readonly DependencyProperty SelectedPageProperty = DependencyProperty.Register(
            "SelectedPage", typeof(SettingsPageViewModel), typeof(SettingsDialog), new PropertyMetadata(default(SettingsPageViewModel), OnSelectedPageChanged, CoerceSelectedPage));

        private static object CoerceSelectedPage(DependencyObject d, object basevalue)
        {
            SettingsDialog dialog = (SettingsDialog) d;
            SettingsPageViewModel page = (SettingsPageViewModel) basevalue;

            if (page == null && dialog.FilteredPages.Count > 0)
                return dialog.FilteredPages.First();

            return basevalue;
        }

        private static void OnSelectedPageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SettingsDialog) d).SelectedPageHasChanged();
        }

        private void SelectedPageHasChanged()
        {
            UpdatePageTitle();
        }

        private void UpdatePageTitle()
        {
            SettingsTitle = BuildSettingsPath();
            _lastSelected = SelectedPage?.SettingsId;
        }

        public SettingsPageViewModel SelectedPage
        {
            get => (SettingsPageViewModel) GetValue(SelectedPageProperty);
            set => SetValue(SelectedPageProperty, value);
        }

        public static readonly DependencyProperty SettingsTitleProperty = DependencyProperty.Register(
            "SettingsTitle", typeof(string), typeof(SettingsDialog), new PropertyMetadata(default(string)));

        public string SettingsTitle
        {
            get => (string) GetValue(SettingsTitleProperty);
            set => SetValue(SettingsTitleProperty, value);
        }

        private static string _lastSelected;

        public SettingsDialog(SettingsViewModel initialSettings, string selectedPage = null)
        {
            Settings = initialSettings.Duplicate();
            CreateInputMappings(GlobalCommandManager.CommandMappings);

            InitializeComponent();

            Pages = BuildPages(PageSelector);
            SelectedPage = FindPage(selectedPage) ?? FindPage(_lastSelected) ?? Pages.FirstOrDefault();
            UpdateFilter();
        }

        private void CreateInputMappings(List<InputMapping> inputMappings)
        {
            List<InputMappingViewModel> mappings = new List<InputMappingViewModel>();

            foreach (ScriptplayerCommand scriptplayerCommand in GlobalCommandManager.Commands.Values)
            {
                InputMappingViewModel mapping = new InputMappingViewModel
                {
                    CommandId = scriptplayerCommand.CommandId,
                    DisplayText = scriptplayerCommand.DisplayText
                };

                var shortcut = inputMappings.FirstOrDefault(c => c.CommandId == scriptplayerCommand.CommandId);
                if (shortcut != null)
                {
                    mapping.Shortcut = shortcut.KeyboardShortcut;
                    mapping.IsGlobal = shortcut.IsGlobal;
                }
                else
                    mapping.Shortcut = "";

                mappings.Add(mapping);
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
                page = page?.SubSettings[string.Join("/", sections.Take(i+1))];

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
                    pages.Add(new SettingsPageViewModel(id,id, header));
                }
            }

            SortPages(pages);

            return pages;
        }

        private static void SortPages(SettingsPageViewModelCollection pages)
        {
            if (pages == null) return;

            pages.Sort((a,b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

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
            Settings.VlcPassword = ((PasswordBox) sender).Password;
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
            ((Button) sender).Focus();
            ApplyInputMappings();
            DialogResult = true;
        }

        private void BtnRemovePath_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SelectedAdditionalPath)) return;

            int currentIndex = Settings.AdditionalPaths.IndexOf(SelectedAdditionalPath);
            Settings.AdditionalPaths.Remove(SelectedAdditionalPath);
            if (currentIndex >= Settings.AdditionalPaths.Count)
                currentIndex--;

            SelectedAdditionalPath = currentIndex >= 0 ? Settings.AdditionalPaths[currentIndex] : null;
        }

        private void BtnAddPath_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AdditionalPath))
                return;

            if (Settings.AdditionalPaths.Contains(AdditionalPath))
            {
                MessageBox.Show(this, "This path has already been added.", "Path already added", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (!IsValidDirectory(AdditionalPath))
            {
                MessageBox.Show("This directoy doesn't exist or is not accessible!", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            Settings.AdditionalPaths.Add(AdditionalPath);
        }

        private static bool IsValidDirectory(string path)
        {
            try
            {
                if(path.EndsWith("*"))
                    return Directory.Exists(path.Substring(0,path.Length-1));

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
            Settings.MpcHcEndpoint = MpcConnectionSettings.DefaultEndpoint;
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
            InputMappingViewModel inputMapping = ((FrameworkElement) sender).DataContext as InputMappingViewModel;

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

            inputMapping.Shortcut = "";
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

                    if(ContainsText(root, filter))
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
            if(SubSettings == null)
                SubSettings = new SettingsPageViewModelCollection();
            subSetting.Parent = this;
            SubSettings.Add(subSetting);
        }
    }
}
