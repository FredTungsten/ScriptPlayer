using System;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ScriptPlayer.Shared
{
    /// <summary>
    /// Interaction logic for OverlayFadeout.xaml
    /// </summary>
    public partial class OverlayFadeout : UserControl
    {
        private readonly Storyboard _storyboard;

        public OverlayFadeout()
        {
            InitializeComponent();
            _storyboard = (Storyboard) Resources["Fade"];
        }

        public void Animate(string text)
        {
            if (CheckAccess())
            {
                TextBlock.Text = text;
                BeginStoryboard(_storyboard);
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() => Animate(text)));
            }
        }
    }
}
