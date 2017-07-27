using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using Octokit;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for VersionDialog.xaml
    /// </summary>
    public partial class VersionDialog : Window
    {
        public static readonly DependencyProperty InstalledVersionProperty = DependencyProperty.Register(
            "InstalledVersion", typeof(string), typeof(VersionDialog), new PropertyMetadata(default(string)));

        public string InstalledVersion
        {
            get { return (string) GetValue(InstalledVersionProperty); }
            set { SetValue(InstalledVersionProperty, value); }
        }

        public static readonly DependencyProperty LatestVersionProperty = DependencyProperty.Register(
            "LatestVersion", typeof(string), typeof(VersionDialog), new PropertyMetadata(default(string)));

        private Release _latestVersion;
        private string _downloadUrl;

        public string LatestVersion
        {
            get { return (string) GetValue(LatestVersionProperty); }
            set { SetValue(LatestVersionProperty, value); }
        }

        public VersionDialog()
        {
            InitializeComponent();
        }

        private void VersionDialog_OnLoaded(object sender, RoutedEventArgs e)
        {
            CheckForNewVersion(false);
        }

        private async void CheckForNewVersion(bool includePrerelease)
        {
            try
            {
                LatestVersion = "Checking ...";

                Version current;
                string version = GetVersion();
                Version.TryParse(version, out current);

                InstalledVersion = current.ToString(3);

                GitHubClient client = new GitHubClient(new ProductHeaderValue("ScriptPlayer", InstalledVersion));

                var releases = (await client.Repository.Release.GetAll("FredTungsten", "ScriptPlayer"))?.Where(r => (!r.Prerelease || includePrerelease) && !r.Draft);

                _latestVersion = releases?.FirstOrDefault();

                if (_latestVersion == null)
                {
                    LatestVersion = "Unknown";
                    return;
                }

                LatestVersion = _latestVersion.TagName;
                _downloadUrl = _latestVersion.Assets.FirstOrDefault(a => a.Name.EndsWith(".exe"))?.BrowserDownloadUrl;

                if (string.IsNullOrWhiteSpace(_downloadUrl))
                {
                    return;
                }

                bool allowdownload = true;


                Version latest;

                if (Version.TryParse(LatestVersion, out latest))
                {
                    if (current >= latest)
                    {
                        allowdownload = false;
                        txtVersion.Text = "You are up-to-date!";
                    }
                    else
                    {
                        txtVersion.Text = "There is a new version available!";
                    }
                }
                else
                {
                    txtVersion.Text = "Unable to determine version";
                }

                if (allowdownload)
                {
                    btnDownload.IsEnabled = true;
                    btnDownload.ToolTip = _downloadUrl;
                }
            }
            catch (Exception)
            {
                LatestVersion = "?";
                txtVersion.Text = "Unable to determine version";
            }
        }

        private string GetVersion()
        {
            string location = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrWhiteSpace(location))
                return "?";

            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(location);
            return fileVersionInfo.ProductVersion;
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(_downloadUrl);
        }

        private void btnCheckAgain_OnClick(object sender, RoutedEventArgs e)
        {
            CheckForNewVersion(true);
        }
    }
}
