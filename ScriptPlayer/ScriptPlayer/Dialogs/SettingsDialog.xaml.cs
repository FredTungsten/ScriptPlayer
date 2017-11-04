using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JetBrains.Annotations;
using Microsoft.Win32;
using ScriptPlayer.Shared;
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
            "SelectedPage", typeof(SettingsPageViewModel), typeof(SettingsDialog), new PropertyMetadata(default(SettingsPageViewModel)));

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

        public SettingsDialog(Settings initialSettings)
        {
            Pages = new SettingsPageViewModelCollection
            {
                new SettingsPageViewModel("General", "GENERAL"),
                new SettingsPageViewModel("External Programs", "EXT"),
                new SettingsPageViewModel("Interaction", "INTERACTION"),
                new SettingsPageViewModel("Paths", "PATHS")
            };

            Pages["EXT"].Add(new SettingsPageViewModel("Buttplug", "BUTTPLUG"));
            Pages["EXT"].Add(new SettingsPageViewModel("Whirligig", "WHIRLIGIG"));
            Pages["EXT"].Add(new SettingsPageViewModel("VLC", "VLC"));

            InitializeComponent();

            Settings = new SettingsViewModel
            {
                WhirligigEndpoint = WhirligigConnectionSettings.DefaultEndpoint,
                VlcEndpoint = VlcConnectionSettings.DefaultEndpoint,
                ButtplugUrl = ButtplugConnectionSettings.DefaultUrl,
                AdditionalPaths = new ObservableCollection<string>()
            };


            if (initialSettings == null) return;

            Settings.CheckForNewVersionOnStartup = initialSettings.CheckForNewVersionOnStartup;

            if (initialSettings.AdditionalPaths != null)
            {
                foreach(string path in initialSettings.AdditionalPaths)
                    Settings.AdditionalPaths.Add(path);
            }

            if (initialSettings.Vlc != null)
            {
                Settings.VlcPassword = initialSettings.Vlc.Password;
                Settings.VlcEndpoint = initialSettings.Vlc.IpAndPort;
            }
                
            if (initialSettings.Buttplug != null)
            {
                Settings.ButtplugUrl = initialSettings.Buttplug.Url;
            }

            if (initialSettings.Whirligig != null)
            {
                Settings.WhirligigEndpoint = initialSettings.Whirligig.IpAndPort;
            }
        }

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectedPage = e.NewValue as SettingsPageViewModel;

            SettingsTitle = BuildSettingsPath();
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

        private void btnOk_OnClick(object sender, RoutedEventArgs e)
        {
            ((Button) sender).Focus();
            DialogResult = true;
        }

        private void btnRemovePath_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SelectedAdditionalPath)) return;

            int currentIndex = Settings.AdditionalPaths.IndexOf(SelectedAdditionalPath);
            Settings.AdditionalPaths.Remove(SelectedAdditionalPath);
            if (currentIndex >= Settings.AdditionalPaths.Count)
                currentIndex--;

            SelectedAdditionalPath = currentIndex >= 0 ? Settings.AdditionalPaths[currentIndex] : null;
        }

        private void btnAddPath_Click(object sender, RoutedEventArgs e)
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

        private bool IsValidDirectory(string path)
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
    }

    public class SettingsViewModel : INotifyPropertyChanged
    {
        private string _vlcEndpoint;
        private string _vlcPassword;
        private string _whirligigEndpoint;
        private string _buttplugUrl;
        private ObservableCollection<string> _additionalPaths;
        private bool _checkForNewVersionOnStartup;

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

        public SettingsPageViewModel(string name, string id)
        {
            Name = name;
            SettingsId = id;
        }

        public string Name { get; set; }
        public ImageSource Icon { get; set; }
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
