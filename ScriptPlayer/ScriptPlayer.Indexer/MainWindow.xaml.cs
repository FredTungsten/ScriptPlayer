using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;
using ScriptPlayer.Shared;

namespace ScriptPlayer.Indexer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnIndex_Clicked(object sender, RoutedEventArgs e)
        {
            DirectoryEntry root = DirectoryEntry.FromDirectory(txtPath.Text, FileMatcher, DirectoryMatcher);

            XmlSerializer serializer = new XmlSerializer(typeof(DirectoryEntry));
            using (FileStream stream = new FileStream(Path.Combine(txtPath.Text, "index.xml"), FileMode.Create, FileAccess.Write))
                serializer.Serialize(stream, root);
        }

        private bool DirectoryMatcher(string directory)
        {
            string dirName = Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar));
            return !dirName.StartsWith(".");
        }

        private bool FileMatcher(string filename)
        {
            string[] allowedExtensions = { ".txt", ".funscript" };
            return allowedExtensions.Contains(Path.GetExtension(filename).ToLower());
        }
    }
}
