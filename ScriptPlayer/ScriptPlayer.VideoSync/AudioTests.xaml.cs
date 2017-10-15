using System.Windows;

namespace ScriptPlayer.VideoSync
{
    /// <summary>
    /// Interaction logic for AudioTests.xaml
    /// </summary>
    public partial class AudioTests : Window
    {
        private string _video;

        public AudioTests(string video)
        {
            Loaded += OnLoaded;

            _video = video;
            InitializeComponent();
            
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            //var decoder = new Accord.Audio.Formats.WaveDecoder()
        }
    }
}
