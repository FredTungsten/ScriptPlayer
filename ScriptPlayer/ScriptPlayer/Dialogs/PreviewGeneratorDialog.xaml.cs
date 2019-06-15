using System;
using System.Windows;
using ScriptPlayer.Generators;
using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for PreviewGeneratorDialog.xaml
    /// </summary>
    public partial class PreviewGeneratorDialog : Window
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof(MainViewModel), typeof(PreviewGeneratorDialog), new PropertyMetadata(default(MainViewModel)));

        public MainViewModel ViewModel
        {
            get { return (MainViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty GeneratorEntryProperty = DependencyProperty.Register(
            "GeneratorEntry", typeof(GeneratorEntry), typeof(PreviewGeneratorDialog), new PropertyMetadata(default(GeneratorEntry)));

        public GeneratorEntry GeneratorEntry
        {
            get { return (GeneratorEntry) GetValue(GeneratorEntryProperty); }
            set { SetValue(GeneratorEntryProperty, value); }
        }

        private readonly PreviewGeneratorSettings _settings;

        private bool _done;
        private PreviewGenerator _generator;
        private bool _success;

        public PreviewGeneratorDialog(MainViewModel viewModel, PreviewGeneratorSettings settings)
        {
            ViewModel = viewModel;
            _settings = settings;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GeneratePreviewGif();
        }

        private void GeneratePreviewGif()
        {
            _generator = new PreviewGenerator(ViewModel.Settings.FfmpegPath);
            _generator.Done += GeneratorOnDone;

            GeneratorEntry entry = _generator.CreateEntry(_settings);
            _generator.ProcessInThread(_settings, entry);
        }

        private void GeneratorOnDone(object sender, Tuple<bool, string> tuple)
        {
            string gifFileName = tuple.Item2;

            _success = tuple.Item1;
            _done = true;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                btnClose.Content = "Close";

                if (_success)
                {
                    SetStatus("Done!", 3 / 3.0);
                    gifPlayer.Load(gifFileName);
                }
                else
                {
                    SetStatus("Failed!", 3 / 3.0);
                }
            }));
        }

        private void SetStatus(string text, double progress = -1)
        {
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SetStatus(text, progress);
                }));
                return;
            }

            if (progress < 0)
            {
                proConversion.IsIndeterminate = true;
            }
            else
            {
                proConversion.IsIndeterminate = false;
                proConversion.Value = Math.Min(1, Math.Max(0, progress));
            }

            txtStatus.Text = text;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_done)
            {
                DialogResult = _success;
            }
            else
            {
                _generator.Cancel();
                DialogResult = false;
            }
        }
    }
}
