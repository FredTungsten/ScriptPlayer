using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Classes.Wrappers;
using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for SceneSelectorDialog.xaml
    /// </summary>
    public partial class SceneSelectorDialog : Window
    {
        public static readonly DependencyProperty ScenesProperty = DependencyProperty.Register(
            "Scenes", typeof(List<SceneViewModel>), typeof(SceneSelectorDialog), new PropertyMetadata(default(List<SceneViewModel>)));

        public List<SceneViewModel> Scenes
        {
            get { return (List<SceneViewModel>) GetValue(ScenesProperty); }
            set { SetValue(ScenesProperty, value); }
        }

        private string _video;
        private MainViewModel _viewmodel;

        public SceneSelectorDialog(MainViewModel viewmodel, string video)
        {
            _viewmodel = viewmodel;
            _video = video;
            InitializeComponent();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            SceneExtractorArguments arguments = new SceneExtractorArguments
            {
                InputFile = _video,
                OutputDirectory = FfmpegWrapper.CreateRandomTempDirectory()
            };

            SceneExtractorWrapper wrapper = new SceneExtractorWrapper(arguments, _viewmodel.Settings.FfmpegPath);
            wrapper.Execute();

            var scenes = new List<SceneViewModel>();

            foreach (string imageFile in Directory.EnumerateFiles(arguments.OutputDirectory))
            {
                string number = Path.GetFileNameWithoutExtension(imageFile);
                int frame = int.Parse(number);

                SceneFrame scene = arguments.Result.Single(w => w.Index == frame);

                scenes.Add(new SceneViewModel
                {
                    IsSelected = false,
                    TimeStamp = scene.TimeStamp,
                    Duration = scene.Duration,
                    Preview = new BitmapImage(new Uri(imageFile, UriKind.Absolute))
                });
            }

            Scenes = scenes;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Scenes == null)
                return;

            var usedScenes = Scenes.Where(s => s.IsSelected).ToList();
            if (usedScenes.Count == 0)
                return;
        }
    }

    public class SceneViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan TimeStamp { get; set; }
        public ImageSource Preview { get; set; }
        public TimeSpan Duration { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
