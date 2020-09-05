using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using JetBrains.Annotations;
using ScriptPlayer.Shared.Helpers;

namespace ScriptPlayer.BundleHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
            "Data", typeof(ObservableCollection<ResultSet>), typeof(MainWindow), new PropertyMetadata(default(ObservableCollection<ResultSet>)));

        public ObservableCollection<ResultSet> Data
        {
            get { return (ObservableCollection<ResultSet>) GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        private readonly string[] _supportedVideoExtensions =
            {"mp4", "mpg", "mpeg", "m4v", "avi", "mkv", "mp4v", "mov", "wmv", "asf", "webm"};

        private readonly string[] _supportedAudioExtensions =
            {"mp3", "wav", "wma"};

        private readonly string[] _supportedMediaExtensions;

        public MainWindow()
        {
            _supportedMediaExtensions = _supportedVideoExtensions.Concat(_supportedAudioExtensions).ToArray();

            InitializeComponent();
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(txtBundleDir.Text))
            {
                MessageBox.Show("Directory doesn't exist!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string[] scripts = Directory.GetFiles(txtBundleDir.Text, "*.funscript", SearchOption.AllDirectories);

            List<ResultSet> results = new List<ResultSet>();

            List<string> additionalPaths = new List<string> { txtVideoDir.Text };
            additionalPaths.AddRange(Directory.GetDirectories(txtVideoDir.Text));

            Dictionary<string, string> knownSources = LoadSources(@"D:\Videos\CH\2. Work in Progress\~SOURCES.txt");

            foreach (string script in scripts)
            {
                string video = FileFinder.FindFile(script, _supportedMediaExtensions, additionalPaths.ToArray());
                if (String.IsNullOrWhiteSpace(video))
                {
                    Debug.WriteLine("No Video found for " + Path.GetFileNameWithoutExtension(script));

                    results.Add(new ResultSet
                    {
                        MediaBaseName = Path.GetFileNameWithoutExtension(script),
                        Duration = null
                    });

                    continue;
                }

                var resultSet = new ResultSet
                {
                    MediaBaseName = Path.GetFileNameWithoutExtension(script),
                    Duration = MediaHelper.GetDuration(video)
                };

                string key = resultSet.MediaBaseName.ToUpperInvariant();

                if (knownSources.ContainsKey(key))
                    resultSet.Url = knownSources[key];

                results.Add(resultSet);
            }

            results.Sort((a, b) => StringComparer.Ordinal.Compare(a.MediaBaseName, b.MediaBaseName));

            Data = new ObservableCollection<ResultSet>(results);
        }

        private Dictionary<string, string> LoadSources(string file)
        {
            string[] lines = File.ReadAllLines(file);

            Dictionary<string,string> sources = new Dictionary<string, string>();

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                int index = line.IndexOf(":", 0, StringComparison.InvariantCultureIgnoreCase);
                if (index <= 0)
                    continue;

                string title = line.Substring(0, index).Trim().ToUpperInvariant();
                string source = line.Substring(index + 1).Trim();

                if(!sources.ContainsKey(title))
                    sources.Add(title, source);
            }

            return sources;
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (Data == null)
                return;

            StringBuilder builder = new StringBuilder();
            foreach (ResultSet result in Data)
            {
                if (!String.IsNullOrWhiteSpace(result.Url))
                    builder.AppendLine($"[*][url={result.Url}]{result.MediaBaseName} [{result.GetDuration}][/url]");
                else
                    builder.AppendLine($"[*]{result.MediaBaseName} [{result.GetDuration}]");
            }

            txtOutput.Text = builder.ToString();
        }

        private void btnGenerateMarkdown_Click(object sender, RoutedEventArgs e)
        {
            if (Data == null)
                return;

            StringBuilder builder = new StringBuilder();
            foreach (ResultSet result in Data)
            {
                if (!String.IsNullOrWhiteSpace(result.Url))
                    builder.AppendLine($"* [{result.MediaBaseName} [{result.GetDuration}]]({result.Url})");
                else
                    builder.AppendLine($"* {result.MediaBaseName} [{result.GetDuration}]");
            }

            txtOutput.Text = builder.ToString();
        }

        private void btnSerach_OnClick(object sender, RoutedEventArgs e)
        {
            ResultSet set = ((Button)sender).DataContext as ResultSet;

            string searchUrl = $"https://www.pornhub.com/video/search?search={WebUtility.UrlEncode(set.MediaBaseName)}";

            Process.Start(searchUrl);
        }
    }

    public class ResultSet : INotifyPropertyChanged
    {
        private TimeSpan? _duration;
        private string _mediaBaseName;
        private string _url;

        public string Url
        {
            get => _url;
            set
            {
                if (value == _url) return;
                _url = value;
                OnPropertyChanged();
            }
        }

        public string MediaBaseName
        {
            get => _mediaBaseName;
            set
            {
                if (value == _mediaBaseName) return;
                _mediaBaseName = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan? Duration
        {
            get => _duration;
            set
            {
                if (value.Equals(_duration)) return;
                _duration = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GetDuration));
            }
        }

        public string GetDuration
        {
            get
            {
                if (Duration == null)
                    return "?";

                if (Duration.Value >= TimeSpan.FromHours(1))
                    return Duration.Value.ToString("h\\:mm\\:ss");

                return Duration.Value.ToString("mm\\:ss");
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
