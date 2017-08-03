using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ScriptPlayer.Shared;

namespace LaunchControl.BeatEditor
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

        private void sldViewPort_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = (Slider)sender;

            timePanel.ViewPort = TimeSpan.FromSeconds(slider.Value);
            timePanel.Offset = VideoPlayer.GetPosition() - timePanel.ViewPort.Divide(2);
        }

        private void mnuOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog(this) != true) return;

            VideoPlayer.Open(dialog.FileName);
            VideoPlayer.TimeSource.Play();
        }

        private void SeekBar_OnSeek(object sender, double relative, TimeSpan absolute, int downmoveup)
        {
            VideoPlayer.SetPosition(absolute);
        }

        private void btnAddBeat_Click(object sender, RoutedEventArgs e)
        {
            BeatContainer container = new BeatContainer();
            TimePanel.SetPosition(container, VideoPlayer.GetPosition());
            TimePanel.SetDuration(container, TimeSpan.FromSeconds(1));
            timePanel.Children.Add(container);
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.TimeSource.Play();
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.TimeSource.Pause();
        }

        private void VideoPlayer_MediaOpened(object sender, EventArgs e)
        {
            VideoPlayer.TimeSource.ProgressChanged += TimeSourceOnProgressChanged;
        }

        private void TimeSourceOnProgressChanged(object sender, TimeSpan timeSpan)
        {
            timePanel.Offset = timeSpan - timePanel.ViewPort.Divide(2);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Text-File|*.txt";
            if (dialog.ShowDialog(this) != true) return;

            SaveBeatsFile(dialog.FileName);
        }

        private void SaveBeatsFile(string filename)
        {
            List<TimeSpan> beats = GetBeats();

            using (var stream = File.Create(filename))
            {
                using (TextWriter writer = new StreamWriter(stream))
                {
                    foreach (TimeSpan beat in beats)
                        writer.WriteLine(beat.TotalSeconds.ToString("f3"));
                }
            }
        }

        private List<TimeSpan> GetBeats()
        {
            List<TimeSpan> beats = new List<TimeSpan>();
            foreach (BeatContainer container in timePanel.Children)
            {
                beats.AddRange(container.GetBeats());
            }

            beats.Sort();
            return beats;
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            BeatBar.Beats = new BeatCollection(GetBeats());
        }

        private void mnuLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog { Filter = "XML-File|*.xml" };
            if (dialog.ShowDialog(this) != true) return;

            BeatProject project = BeatProject.Load(dialog.FileName);
            if(project.VideoFile != VideoPlayer.OpenedFile)
                VideoPlayer.Open(project.VideoFile);

            timePanel.Children.Clear();

            foreach (var beatSegment in project.Segments)
            {
                BeatContainer container = new BeatContainer();
                container.SetBeatSegment(beatSegment);

                timePanel.Children.Add(container);
            }
        }

        private void mnuSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog { Filter = "XML-File|*.xml"};
            if (dialog.ShowDialog(this) != true) return;

            BeatProject project = new BeatProject();
            project.VideoFile = VideoPlayer.OpenedFile;

            foreach (BeatContainer container in timePanel.Children)
            {
                project.Segments.Add(container.GetBeatSegment());
            }

            project.Save(dialog.FileName);
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            new BeatToPatternConverterDialog().Show();
        }
    }
}
