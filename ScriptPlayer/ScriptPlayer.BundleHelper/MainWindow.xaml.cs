using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using MediaInfo;
using ScriptPlayer.Shared.Helpers;

namespace ScriptPlayer.BundleHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            string[] scripts = Directory.GetFiles(txtBundleDir.Text, "*.funscript");

            List<ResultSet> results = new List<ResultSet>();

            List<string> additionalPaths = new List<string> { txtVideoDir.Text };
            additionalPaths.AddRange(Directory.GetDirectories(txtVideoDir.Text));

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

                results.Add(new ResultSet
                {
                    MediaBaseName = Path.GetFileNameWithoutExtension(script),
                    Duration = MediaHelper.GetDuration(video)
                });
            }

            results.Sort((a,b) => StringComparer.Ordinal.Compare(a.MediaBaseName, b.MediaBaseName));

            StringBuilder builder = new StringBuilder();
            foreach (ResultSet result in results)
            {
                builder.AppendLine($"[*]{result.MediaBaseName} [{result.GetDuration}]");
            }

            txtOutput.Text = builder.ToString();
        }
    }

    public class ResultSet
    {
        public string MediaBaseName { get; set; }
        public TimeSpan? Duration { get; set; }

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
    }
}
