using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ScriptPlayer
{
    /// <summary>
    /// Interaction logic for FlashOverlay.xaml
    /// </summary>
    public partial class FlashOverlay : UserControl
    {
        private DoubleAnimation _animation;

        public FlashOverlay()
        {
            InitializeComponent();

            if(!DesignerProperties.GetIsInDesignMode(this))
                Opacity = 0.0;

            _animation = new DoubleAnimation(1,0, new Duration(TimeSpan.FromMilliseconds(80)));
        }

        public void Flash()
        {
            BeginAnimation(OpacityProperty, _animation);
        }
    }
}
