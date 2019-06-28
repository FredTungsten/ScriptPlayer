using ScriptPlayer.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

namespace ScriptPlayer.BeatFileChecker
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string[] paths = txtPath.Text.Split(';');

            string[] files = paths.SelectMany(path => Directory.EnumerateFiles(path, "*.txt", SearchOption.AllDirectories)).ToArray();

            List<Tuple<string, int>> brokenFiles = new List<Tuple<string, int>>();

            foreach (string textFile in files)
            {
                try
                {
                    TimeSpan lastbeat = TimeSpan.Zero;
                    int beatcount = 0;

                    using (FileStream stream = new FileStream(textFile, FileMode.Open, FileAccess.Read))
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            while (!reader.EndOfStream)
                            {
                                string line = reader.ReadLine();
                                if (string.IsNullOrWhiteSpace(line))
                                    continue;

                                TimeSpan beat = TimeSpan.FromSeconds(double.Parse(line.Replace(",", "."), CultureInfo.InvariantCulture));
                                beatcount++;

                                if (lastbeat > beat)
                                {
                                    brokenFiles.Add(new Tuple<string, int>(textFile, beatcount));
                                    break;
                                }

                                lastbeat = beat;
                            }
                        }
                    }
                }
                catch
                {
                    
                }
            }

            foreach(var tup in brokenFiles)
                Debug.WriteLine($"{tup.Item2:######} | {tup.Item1}");
        }
    }
}
