using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using Accord.Video.FFMPEG;

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
