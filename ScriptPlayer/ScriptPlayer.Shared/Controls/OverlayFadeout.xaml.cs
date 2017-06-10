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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ScriptPlayer.Shared.Controls
{
    /// <summary>
    /// Interaction logic for OverlayFadeout.xaml
    /// </summary>
    public partial class OverlayFadeout : UserControl
    {
        private Storyboard _storyboard;

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
