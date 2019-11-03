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
using ScriptPlayer.Shared.Scripts;

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

            string[] files = paths.SelectMany(path => Directory.EnumerateFiles(path, "*.funscript", SearchOption.AllDirectories)).ToArray();

            List<ScriptStats> stats = new List<ScriptStats>();
            FunScriptLoader loader = new FunScriptLoader();

            foreach (string textFile in files)
            {
                try
                {
                    var collection = new BeatCollection(loader.Load(textFile).Select(l => l.TimeStamp));

                    var chapters = GetChapters(collection);
                    if (chapters == null || chapters.Count < 1)
                        continue;

                    TimeSpan duration = chapters.Aggregate(TimeSpan.Zero, (total, current) => total + (current.Last() - current.First()));
                    double bpm = chapters.Sum(c => c.Count - 1) / duration.TotalMinutes;

                    stats.Add(new ScriptStats
                    {
                        Beats = collection.ToList(),
                        Bpm = bpm,
                        ContentDuration = duration,
                        File = textFile
                    });
                }
                catch
                {
                    
                }
            }

            stats = stats.OrderByDescending(s => s.Bpm).ToList();

            int pos = 0;
            foreach (var stat in stats)
            {
                pos++;
                Debug.WriteLine($"[*] {stat.Bpm:f0} BMP, {stat.ContentDuration:h\\:mm\\:ss}, {System.IO.Path.GetFileNameWithoutExtension(stat.File)}");
            }
        }

        private List<List<TimeSpan>> GetChapters(BeatCollection timeStamps)
        {
            TimeSpan gapDuration = TimeSpan.FromSeconds(10);

            if (timeStamps.Count < 2)
                return null;

            int chapterBegin = int.MinValue;
            int chapterEnd = int.MinValue;

            List<List<TimeSpan>> chapters = new List<List<TimeSpan>>();

            for (int index = 0; index < timeStamps.Count; index++)
            {
                if (chapterBegin == int.MinValue)
                {
                    chapterBegin = index;
                    chapterEnd = index;
                }
                else if (timeStamps[index] - timeStamps[chapterEnd] < gapDuration)
                {
                    chapterEnd = index;
                }
                else
                {
                    chapters.Add(GetRange(timeStamps,chapterBegin, chapterEnd));

                    chapterBegin = index;
                    chapterEnd = index;
                }
            }

            if (chapterBegin != int.MinValue && chapterEnd != int.MinValue)
            {
                chapters.Add(GetRange(timeStamps, chapterBegin, chapterEnd));
            }

            return chapters;
        }

        private List<T> GetRange<T>(IEnumerable<T> source, int firstIndex, int lastIndex)
        {
            return source.Skip(firstIndex).Take(lastIndex - firstIndex + 1).ToList();
        }
    }

    public class ScriptStats
    {
        public double Bpm { get; set; }
        public List<TimeSpan> Beats { get; set; }
        public TimeSpan ContentDuration { get; set; }
        public string File { get; set; }
    }
}
