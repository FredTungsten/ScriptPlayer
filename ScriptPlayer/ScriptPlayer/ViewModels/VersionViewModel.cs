using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using JetBrains.Annotations;
using Octokit;
using ScriptPlayer.Shared;

namespace ScriptPlayer.ViewModels
{
    public class VersionViewModel : INotifyPropertyChanged
    {
        private string _latestVersion;
        private string _installedVersion;
        private string _versionText;
        private bool _canDownload;
        private bool _canCheck;
        private string _downloadUrl;
        private Version _currentVersion;
        private bool _hasSuccessfullyChecked;


        public string DownloadUrl
        {
            get => _downloadUrl;
            private set
            {
                if (value == _downloadUrl) return;
                _downloadUrl = value;
                OnPropertyChanged();
            }
        }

        public string LatestVersion
        {
            get => _latestVersion;
            private set
            {
                if (value == _latestVersion) return;
                _latestVersion = value;
                OnPropertyChanged();
            }
        }

        public string InstalledVersion
        {
            get => _installedVersion;
            private set
            {
                if (value == _installedVersion) return;
                _installedVersion = value;
                OnPropertyChanged();
            }
        }

        public string VersionText
        {
            get => _versionText;
            private set
            {
                if (value == _versionText) return;
                _versionText = value;
                OnPropertyChanged();
            }
        }

        public bool CanDownload
        {
            get => _canDownload;
            private set
            {
                if (value == _canDownload) return;
                _canDownload = value;
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged();
            }
        }

        public bool CanCheck
        {
            get => _canCheck;
            private set
            {
                if (value == _canCheck) return;
                _canCheck = value;
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged();
            }
        }

        public RelayCommand DownloadLatestVersionCommand { get; set; }
        public RelayCommand CheckForLatestVersionCommand { get; set; }

        public VersionViewModel()
        {
            CanCheck = true;
            CanDownload = false;

            DownloadLatestVersionCommand = new RelayCommand(ExecuteDownloadLatestVersion, CanDownloadLatestVersion);
            CheckForLatestVersionCommand = new RelayCommand(ExecuteCheckForNewVersion, CanCheckForNewVersion);

            DetermineCurrentVersion();
        }

        private void DetermineCurrentVersion()
        {
            string version = GetVersion();
            if (!Version.TryParse(version, out Version current)) return;
            _currentVersion = current;
            InstalledVersion = current.ToString(3);
        }

        private bool CanCheckForNewVersion()
        {
            return CanCheck;
        }

        private void ExecuteCheckForNewVersion()
        {
            CheckForNewVersion(true);
        }

        private bool CanDownloadLatestVersion()
        {
            return CanDownload;
        }

        private void ExecuteDownloadLatestVersion()
        {
            DownloadLatestVersion();
        }

        private void DownloadLatestVersion()
        {
            Process.Start(DownloadUrl);
        }

        public async void CheckForNewVersion(bool includePrerelease)
        {
            try
            {
                CanCheck = false;
                CanDownload = false;
                VersionText = "Determining Version...";
                LatestVersion = "Checking ...";

                GitHubClient client = new GitHubClient(new ProductHeaderValue("ScriptPlayer", InstalledVersion));

                IEnumerable<Release> releases =
                    (await client.Repository.Release.GetAll("FredTungsten", "ScriptPlayer"))?.Where(
                        r => (!r.Prerelease || includePrerelease) && !r.Draft);

                Release latestVersion = releases?.FirstOrDefault();

                if (latestVersion == null)
                {
                    LatestVersion = "Unknown";
                    return;
                }

                LatestVersion = latestVersion.TagName;

                // Might be better to open the release page so people see what changed
                //DownloadUrl = latestVersion.Assets.FirstOrDefault(a => a.Name.EndsWith(".exe"))?.BrowserDownloadUrl;

                DownloadUrl = latestVersion.HtmlUrl;

                if (string.IsNullOrWhiteSpace(DownloadUrl))
                {
                    return;
                }

                _hasSuccessfullyChecked = true;
                bool allowdownload = true;

                if (Version.TryParse(LatestVersion, out Version latest))
                {
                    if (_currentVersion >= latest)
                    {
                        allowdownload = false;
                        VersionText = "You are up-to-date!";
                    }
                    else
                    {
                        VersionText = "There is a new version available!";
                    }
                }
                else
                {
                    VersionText = "Unable to determine version";
                }

                if (allowdownload)
                {
                    CanDownload = true;
                }
            }
            catch (Exception)
            {
                LatestVersion = "?";
                VersionText = "Unable to determine version";
            }
            finally
            {
                CanCheck = true;
            }
        }

        private static string GetVersion()
        {
            string location = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrWhiteSpace(location))
                return "?";

            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(location);
            return fileVersionInfo.ProductVersion;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void CheckIfYouHaventAlready()
        {
            if(!_hasSuccessfullyChecked)
                if(CanCheck)
                    CheckForNewVersion(false);
        }
    }
}
