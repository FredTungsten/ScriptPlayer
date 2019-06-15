using System.Collections.Generic;
using System.Windows;
using ScriptPlayer.Generators;
using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for GeneratorProgressDialog.xaml
    /// </summary>
    public partial class GeneratorProgressDialog : Window
    {
        public static readonly DependencyProperty WorkQueueProperty = DependencyProperty.Register(
            "WorkQueue", typeof(GeneratorWorkQueue), typeof(GeneratorProgressDialog), new PropertyMetadata(default(GeneratorWorkQueue)));

        public GeneratorWorkQueue WorkQueue
        {
            get { return (GeneratorWorkQueue) GetValue(WorkQueueProperty); }
            set { SetValue(WorkQueueProperty, value); }
        }

        public static readonly DependencyProperty CloseButtonTextProperty = DependencyProperty.Register(
            "CloseButtonText", typeof(string), typeof(GeneratorProgressDialog), new PropertyMetadata(default(string)));

        private bool _closeWhenDone;

        public string CloseButtonText
        {
            get { return (string) GetValue(CloseButtonTextProperty); }
            set { SetValue(CloseButtonTextProperty, value); }
        }

        public GeneratorProgressDialog(MainViewModel viewModel)
        {
            WorkQueue = viewModel.WorkQueue;

            CloseButtonText = "Close/Cancel";
            InitializeComponent();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            //if (_done)
            //{
            //    DialogResult = true;
            //}
            //else
            //{

            //    this.IsEnabled = false;

            //    _canceled = true;
            //    _wrapper?.Cancel();

            //    if (!_processThread.Join(TimeSpan.FromSeconds(5)))
            //        _processThread.Abort();

            //    DialogResult = false;
            //}

            //Close();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //_closeWhenDone = true;
            //if (_done)
            //    Close();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _closeWhenDone = false;
        }

        private void btnSkip_Click(object sender, RoutedEventArgs e)
        {
            WorkQueue.RemoveDone();
        }
    }
}
