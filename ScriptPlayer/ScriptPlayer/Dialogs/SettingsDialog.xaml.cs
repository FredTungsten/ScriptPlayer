using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using JetBrains.Annotations;

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
            get { return (SettingsPageViewModelCollection) GetValue(PagesProperty); }
            set { SetValue(PagesProperty, value); }
        }

        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(
            "Settings", typeof(SettingsViewModel), typeof(SettingsDialog), new PropertyMetadata(default(SettingsViewModel)));

        public SettingsViewModel Settings
        {
            get { return (SettingsViewModel) GetValue(SettingsProperty); }
            set { SetValue(SettingsProperty, value); }
        }

        public static readonly DependencyProperty SelectedPageProperty = DependencyProperty.Register(
            "SelectedPage", typeof(SettingsPageViewModel), typeof(SettingsDialog), new PropertyMetadata(default(SettingsPageViewModel)));

        public SettingsPageViewModel SelectedPage
        {
            get { return (SettingsPageViewModel) GetValue(SelectedPageProperty); }
            set { SetValue(SelectedPageProperty, value); }
        }

        public static readonly DependencyProperty SettingsTitleProperty = DependencyProperty.Register(
            "SettingsTitle", typeof(string), typeof(SettingsDialog), new PropertyMetadata(default(string)));

        public string SettingsTitle
        {
            get { return (string) GetValue(SettingsTitleProperty); }
            set { SetValue(SettingsTitleProperty, value); }
        }

        public SettingsDialog()
        {
            Pages = new SettingsPageViewModelCollection();
            Pages.Add(new SettingsPageViewModel("External Programs", "EXT"));
            Pages["EXT"].Add(new SettingsPageViewModel("Buttplug", "BUTTPLUG"));
            Pages["EXT"].Add(new SettingsPageViewModel("Whirligig", "WHIRLIGIG"));
            Pages["EXT"].Add(new SettingsPageViewModel("VLC", "VLC"));
            Pages.Add(new SettingsPageViewModel("Interaction", "INTERACTION"));
            Pages.Add(new SettingsPageViewModel("Paths", "PATHS"));


            InitializeComponent();
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
    }

    public class SettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SettingsPageViewModelCollection : List<SettingsPageViewModel>
    {
        public SettingsPageViewModel this[string id]
        {
            get => this.FirstOrDefault(s => s.SettingsId == id);
        }
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
