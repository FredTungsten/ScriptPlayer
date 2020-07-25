using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for CompactControlWindow.xaml
    /// </summary>
    public partial class CompactControlWindow : Window
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof(MainViewModel), typeof(CompactControlWindow), new PropertyMetadata(default(MainViewModel)));

        public MainViewModel ViewModel
        {
            get { return (MainViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public CompactControlWindow(MainViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }

        private void SeekBar_OnSeek(object sender, double relative, TimeSpan absolute, int downmoveup)
        {
            ViewModel.Seek(absolute, downmoveup);
        }

        private void TimeDisplay_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel.Settings.ShowTimeLeft ^= true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Topmost = true;
        }

        private void TopGird_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();   
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
