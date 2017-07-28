using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using Octokit;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for ScriptDownloadDialog.xaml
    /// </summary>
    public partial class ScriptDownloadDialog : Window
    {
        public static readonly DependencyProperty ScriptsProperty = DependencyProperty.Register(
            "Scripts", typeof(List<ScriptViewModel>), typeof(ScriptDownloadDialog), new PropertyMetadata(default(List<ScriptViewModel>)));

        public List<ScriptViewModel> Scripts
        {
            get { return (List<ScriptViewModel>)GetValue(ScriptsProperty); }
            set { SetValue(ScriptsProperty, value); }
        }

        public ScriptDownloadDialog()
        {
            InitializeComponent();
            LoadScripts();
        }

        public async void LoadScripts()
        {
            try
            {
                List<RepositoryContent> allscripts = new List<RepositoryContent>();

                GitHubClient client = new GitHubClient(new ProductHeaderValue("ScriptPlayer"));

                allscripts.AddRange(await GetScripts(client, "FredTungsten", "Scripts"));
                allscripts.AddRange(await GetScripts(client, "funjack", "funscripts"));
                var usableScripts =
                    allscripts.Where(
                        s => new[] {".txt", ".funscript"}.Contains(System.IO.Path.GetExtension(s.Name).ToLower()));

                var limit = client.GetLastApiInfo()?.RateLimit;

                Title = limit.Remaining + " GitHub requests left until " +
                        limit.Reset.ToLocalTime().ToString("HH:mm:ss");

                Scripts = usableScripts.OrderBy(s => s.Name).Select(s => new ScriptViewModel
                {
                    Name = s.Name,
                    DownloadUrl = s.DownloadUrl,
                    IsSelected = false
                }).ToList();
            }
            catch (RateLimitExceededException)
            {
                MessageBox.Show(
                    "Unfortunately you have exceeded the allowed request limit. Come back in an hour or so.",
                    "Limit exceeded", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task<IEnumerable<RepositoryContent>> GetScripts(GitHubClient client, string owner, string repo)
        {
            var contents = await client.Repository.Content.GetAllContents(owner, repo);

            List<RepositoryContent> results = new List<RepositoryContent>();

            foreach (var subcontent in contents)
            {
                if (subcontent.Type == ContentType.Dir)
                    results.AddRange(await GetDir(client, owner, repo, subcontent.Name));
                else if (subcontent.Type == ContentType.File)
                    results.Add(subcontent);
            }

            return results;
        }

        private async Task<IEnumerable<RepositoryContent>> GetDir(GitHubClient client, string owner, string repo, string path)
        {
            var contents = await client.Repository.Content.GetAllContents(owner, repo, path);
            List<RepositoryContent> results = new List<RepositoryContent>();
            foreach (var subcontent in contents)
            {
                if (subcontent.Type == ContentType.Dir)
                    results.AddRange(await GetDir(client, owner, repo, path + "/" + subcontent.Name));
                else if (subcontent.Type == ContentType.File)
                    results.Add(subcontent);
            }
            return results;
        }
    }

    public class ScriptViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;
        public string Name { get; set; }
        public Uri DownloadUrl { get; set; }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
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
