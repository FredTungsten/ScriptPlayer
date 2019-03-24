using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using JetBrains.Annotations;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Classes;
using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for CreateThumbnailsDialog.xaml
    /// </summary>
    public partial class CreateThumbnailsDialog : Window
    {
        public static readonly DependencyProperty EntriesProperty = DependencyProperty.Register(
            "Entries", typeof(List<ThumbnailProgressEntry>), typeof(CreateThumbnailsDialog),
            new PropertyMetadata(default(List<ThumbnailProgressEntry>)));

        private readonly ThumbnailGeneratorSettings _settings;
        private FrameConverterWrapper _wrapper;
        private bool _canceled;
        private bool _done;
        private Thread _processThread;

        public List<ThumbnailProgressEntry> Entries
        {
            get { return (List<ThumbnailProgressEntry>) GetValue(EntriesProperty); }
            set { SetValue(EntriesProperty, value); }
        }

        public static readonly DependencyProperty CloseButtonTextProperty = DependencyProperty.Register(
            "CloseButtonText", typeof(string), typeof(CreateThumbnailsDialog), new PropertyMetadata("Cancel"));

        public string CloseButtonText
        {
            get { return (string) GetValue(CloseButtonTextProperty); }
            set { SetValue(CloseButtonTextProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof(MainViewModel), typeof(CreateThumbnailsDialog), new PropertyMetadata(default(MainViewModel)));

        public MainViewModel ViewModel
        {
            get { return (MainViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public CreateThumbnailsDialog(MainViewModel viewmodel, ThumbnailGeneratorSettings settings)
        {
            ViewModel = viewmodel;
            _settings = settings;
            Entries = settings.Videos.Select(vf => new ThumbnailProgressEntry(vf)).ToList();

            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TaskbarItemInfo = new TaskbarItemInfo
            {
                ProgressState = TaskbarItemProgressState.Normal
            };

            var entries = Entries;

            string ffmpegexe = ViewModel.Settings.FfmpegPath;

            _processThread = new Thread(() =>
            {
                FrameConverterWrapper wrapper = new FrameConverterWrapper(ffmpegexe);
                _wrapper = wrapper;

                wrapper.Intervall = _settings.Intervall;
                wrapper.Width = _settings.Width;
                wrapper.Height = _settings.Height;

                ThumbnailProgressEntry currentEntry = null;

                List<ThumbnailProgressEntry> skipped = new List<ThumbnailProgressEntry>();

                wrapper.ProgressChanged += (s, progress) => { SetStatus(currentEntry, null, progress); };

                foreach (var entry in entries)
                {
                    if (_canceled)
                        return;

                    string thumbfile = Path.ChangeExtension(entry.FilePath, "thumbs");

                    if (File.Exists(thumbfile) && _settings.SkipExisting)
                    {
                        SetStatus(entry, "Skipped", 1);
                        skipped.Add(entry);
                        continue;
                    }
                }

                foreach (var entry in entries)
                {
                    if (_canceled)
                        return;

                    if (skipped.Contains(entry))
                        continue;

                    currentEntry = entry;

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        dataGrid.SelectedItem = currentEntry;
                        dataGrid.ScrollIntoView(currentEntry);
                    }));

                    string thumbfile = Path.ChangeExtension(currentEntry.FilePath, "thumbs");

                    if (File.Exists(thumbfile) && _settings.SkipExisting)
                    {
                        SetStatus(currentEntry, "Skipped", 1);
                        continue;
                    }

                    SetStatus(currentEntry, "Extracting Frames", 0);

                    wrapper.VideoFile = currentEntry.FilePath;
                    wrapper.GenerateRandomOutputPath();
                    string tempPath = wrapper.OutputPath;
                    wrapper.Execute();

                    if (_canceled)
                        return;

                    SetStatus(currentEntry, "Saving Thumbnails", 1);

                    VideoThumbnailCollection thumbnails = new VideoThumbnailCollection();

                    List<string> usedFiles = new List<string>();

                    foreach (string file in Directory.EnumerateFiles(tempPath))
                    {
                        string number = Path.GetFileNameWithoutExtension(file);
                        int index = int.Parse(number);


                        TimeSpan position = TimeSpan.FromSeconds(index * 10 - 5);

                        var frame = new BitmapImage();
                        frame.BeginInit();
                        frame.CacheOption = BitmapCacheOption.OnLoad;
                        frame.UriSource = new Uri(file, UriKind.Absolute);
                        frame.EndInit();

                        thumbnails.Add(position, frame);
                        usedFiles.Add(file);
                    }
                    
                    using (FileStream stream = new FileStream(thumbfile, FileMode.Create, FileAccess.Write))
                    {
                        thumbnails.Save(stream);
                    }

                    thumbnails.Dispose();

                    foreach (string tempFile in usedFiles)
                        File.Delete(tempFile);

                    Directory.Delete(tempPath);

                    SetStatus(currentEntry, "Done", 1);
                }

                _done = true;

                Dispatcher.BeginInvoke(new Action(() => { CloseButtonText = "Close"; }));
            });

            _processThread.Start();
        }


        private void SetStatus(ThumbnailProgressEntry entry, string text, double progress)
        {
            if (!this.CheckAccess())
            {
                this.Dispatcher.BeginInvoke(new Action(() => { SetStatus(entry, text, progress); }));
                return;
            }

            if (text != null)
                entry.Status = text;

            if (progress >= 0)
            {
                entry.Progress = progress;

                double totalProgress = Entries.Sum(e => Math.Min(1, Math.Max(0, e.Progress))) / Entries.Count;
                TaskbarItemInfo.ProgressValue = totalProgress;
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_done)
            {
                DialogResult = true;
            }
            else
            {

                this.IsEnabled = false;

                _canceled = true;
                _wrapper?.Cancel();

                if (!_processThread.Join(TimeSpan.FromSeconds(5)))
                    _processThread.Abort();

                DialogResult = false;
            }

            Close();
        }
    }

    public class ThumbnailProgressEntry : INotifyPropertyChanged
    {
        private double _progress;
        private string _status;
        public string FilePath { get; set; }

        public string FileName { get; set; }

        public double Progress
        {
            get => _progress;
            set
            {
                if (value.Equals(_progress)) return;
                _progress = value;
                OnPropertyChanged();
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (value == _status) return;
                _status = value;
                OnPropertyChanged();
            }
        }

        public ThumbnailProgressEntry(string filePath)
        {
            FilePath = filePath;
            FileName = System.IO.Path.GetFileName(filePath);
            Progress = 0;
            Status = "Queued";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
