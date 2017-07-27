using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Octokit;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for ScriptDownloadDialog.xaml
    /// </summary>
    public partial class ScriptDownloadDialog : Window
    {
        public ScriptDownloadDialog()
        {
            InitializeComponent();
            Whatever();
        }

        public async void Whatever()
        {
            var scripts = await GetScripts();
            var usableScripts = scripts.Where(s => new string[] { ".txt", ".funscript" }.Contains(System.IO.Path.GetExtension(s.Name).ToLower())).Select(f => f.Name);
        }

        private async Task<IEnumerable<RepositoryContent>> GetScripts()
        {
            GitHubClient client = new GitHubClient(new ProductHeaderValue("ScriptPlayer"));
            var contents = await client.Repository.Content.GetAllContents("FredTungsten", "ScriptPlayer", "Scripts");

            List<RepositoryContent> results = new List<RepositoryContent>();

            foreach (var subcontent in contents)
            {
                if (subcontent.Type == ContentType.Dir)
                    results.AddRange(await GetDir(subcontent.Name));
                else if (subcontent.Type == ContentType.File)
                    results.Add(subcontent);
            }

            return results;
        }

        private async Task<IEnumerable<RepositoryContent>> GetDir(string content)
        {
            GitHubClient client = new GitHubClient(new ProductHeaderValue("ScriptPlayer"));
            var contents = await client.Repository.Content.GetAllContents("FredTungsten", "ScriptPlayer", "Scripts/" + content);
            List<RepositoryContent> results = new List<RepositoryContent>();
            foreach (var subcontent in contents)
            {
                if (subcontent.Type == ContentType.Dir)
                    results.AddRange(await GetDir(content + "/" + subcontent.Name));
                else if (subcontent.Type == ContentType.File)
                    results.Add(subcontent);
            }
            return results;
        }
    }
}
