using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        public static readonly DependencyProperty PagesProperty = DependencyProperty.Register(
            "Pages", typeof(SettingsPageViewModelCollection), typeof(SettingsDialog), new PropertyMetadata(default(SettingsPageViewModelCollection)));

        public SettingsPageViewModelCollection Pages
        {
            get => (SettingsPageViewModelCollection) GetValue(PagesProperty);
            set => SetValue(PagesProperty, value);
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
            "SelectedPage", typeof(SettingsPageViewModel), typeof(SettingsDialog), new PropertyMetadata(default(SettingsPageViewModel), OnSelectedPageChanged));

        private static void OnSelectedPageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SettingsDialog) d).UpdatePageTitle();
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

        public SettingsDialog(SettingsViewModel initialSettings)
        {
            Settings = initialSettings.Duplicate();

            InitializeComponent();

            Pages = BuildPages(PageSelector);
            SelectedPage = FindPage(_lastSelected) ?? Pages.FirstOrDefault();
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

        private void SettingsDialog_OnLoaded(object sender, RoutedEventArgs e)
        {
            txtPasswordVlcPassword.Password = Settings.VlcPassword;
        }

        private void BtnOk_OnClick(object sender, RoutedEventArgs e)
        {
            ((Button) sender).Focus();
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
            }

            Settings.AdditionalPaths.Add(AdditionalPath);
        }

        private static bool IsValidDirectory(string path)
        {
            try
            {
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
