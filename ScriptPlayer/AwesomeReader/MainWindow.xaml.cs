using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace AwesomeReader
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

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "JSON-Files|*.json";

            if (dialog.ShowDialog(this) != true)
                return;

            JObject root = JObject.Parse(File.ReadAllText(dialog.FileName));

            StringBuilder builder = new StringBuilder();

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

                builder.AppendLine(line);
            }

            txtOut.Text = builder.ToString();
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
