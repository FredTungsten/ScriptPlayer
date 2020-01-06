using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using ScriptPlayer.Shared;

namespace AwesomeReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty PathsProperty = DependencyProperty.Register(
            "Paths", typeof(List<AwesomePath>), typeof(MainWindow), new PropertyMetadata(default(List<AwesomePath>)));

        public List<AwesomePath> Paths
        {
            get => (List<AwesomePath>) GetValue(PathsProperty);
            set => SetValue(PathsProperty, value);
        }

        public MainWindow()
        {
            Paths = Enum.GetValues(typeof(AwesomePath)).Cast<AwesomePath>().ToList();

            InitializeComponent();
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "JSON-Files|*.json";

            if (dialog.ShowDialog(this) != true)
                return;

            JObject root = JObject.Parse(File.ReadAllText(dialog.FileName));

            StringBuilder unicodeBuilder = new StringBuilder();
            StringBuilder exampleBuilder = new StringBuilder();
            StringBuilder pathStringBuilder = new StringBuilder();
            StringBuilder enumBuilder = new StringBuilder();
            StringBuilder switchBuilder = new StringBuilder();

            HashSet<string> usedNames = new HashSet<string>();

            foreach (JToken child in root.Children())
            {
                JToken obj = child.Children().First();

                if (obj == null)
                    continue;

                string label = obj["label"].Value<string>();
                string unicode = obj["unicode"].Value<string>();

                int index = 1;
                string name;

                do
                {
                    name = TransformLabelToName(label, index);
                    index++;
                } while (usedNames.Contains(name));

                usedNames.Add(name);
                
                string charSeq = $"\\x{unicode}";

                string line = $"public const string {name} = \"{charSeq}\"; // {label}";

                unicodeBuilder.AppendLine(line);

                string actualChar = char.ConvertFromUtf32(Convert.ToInt32(unicode, 16));

                exampleBuilder.AppendLine($"{actualChar} = {name}");


                string[] styles = obj["styles"].Values<string>().ToArray();

                foreach (string style in styles)
                {
                    string path = obj["svg"][style]["path"].Value<string>();
                    string fullName = $"{name}_{UpFirst(style)}";

                    string line3 = $"public const string {fullName} = \"{path}\"; // {label}";
                    pathStringBuilder.AppendLine(line3);
                    enumBuilder.AppendLine($"{fullName},");
                    switchBuilder.AppendLine($"case AwesomePath.{fullName}: return {fullName};");
                }
            }

            txtOut.Text = unicodeBuilder.ToString();
            txtPaths.Text = pathStringBuilder + "\r\n\r\n" + switchBuilder + "\r\n\r\n" + enumBuilder;
        }

        private string TransformLabelToName(string label, int index)
        {
            string filteredLabel = "";

            foreach (char c in label)
            {
                if (char.IsLetterOrDigit(c) || c == ' ')
                    filteredLabel += c;
            }

            string[] parts = filteredLabel.Split(new[]{' ','_','-'}, StringSplitOptions.RemoveEmptyEntries);

            label = string.Join("_", parts.Select(UpFirst));
            
            if (!char.IsLetter(label[0]))
                label = "x" + label;

            if (index > 1)
                label += "_" + index;

            return label;
        }

        private string UpFirst(string s)
        {
            string result = "";

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                if(i == 0)
                    result += Char.ToUpper(c);
                else
                    result += Char.ToLower(c);
            }

            return result;
        }
    }
}
