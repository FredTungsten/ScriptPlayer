using System;
using System.ComponentModel;
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
        public static readonly DependencyProperty CloseWhenDoneProperty = DependencyProperty.Register(
            "CloseWhenDone", typeof(bool), typeof(GeneratorProgressDialog), new PropertyMetadata(default(bool), OnCloseWhenDonePropertyChanged));

        private static void OnCloseWhenDonePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GeneratorProgressDialog)d).CloseWhenDoneChanged();
        }

        private void CloseWhenDoneChanged()
        {
            CheckCloseWhenDone();
        }
        
        public bool CloseWhenDone
        {
            get => (bool) GetValue(CloseWhenDoneProperty);
            set => SetValue(CloseWhenDoneProperty, value);
        }

        public static readonly DependencyProperty WorkQueueProperty = DependencyProperty.Register(
            "WorkQueue", typeof(GeneratorWorkQueue), typeof(GeneratorProgressDialog), new PropertyMetadata(default(GeneratorWorkQueue)));

        public GeneratorWorkQueue WorkQueue
        {
            get => (GeneratorWorkQueue) GetValue(WorkQueueProperty);
            set => SetValue(WorkQueueProperty, value);
        }

        public static readonly DependencyProperty CloseButtonTextProperty = DependencyProperty.Register(
            "CloseButtonText", typeof(string), typeof(GeneratorProgressDialog), new PropertyMetadata(default(string)));

        public string CloseButtonText
        {
            get => (string) GetValue(CloseButtonTextProperty);
            set => SetValue(CloseButtonTextProperty, value);
        }

        public GeneratorProgressDialog(MainViewModel viewModel)
        {
            WorkQueue = viewModel.WorkQueue;
            
            CloseButtonText = "Close";
            InitializeComponent();
        }

        private void WorkQueueOnJobStarted(object sender, GeneratorJobEventArgs eventArgs)
        {
            if (Dispatcher.CheckAccess())
                dataGrid.ScrollIntoView(eventArgs.Job.Entry);
            else
                Dispatcher.BeginInvoke(new Action(()=> { dataGrid.ScrollIntoView(eventArgs.Job.Entry); }));
        }

        private void WorkQueueOnJobFinished(object sender, GeneratorJobEventArgs eventArgs)
        {
            if (Dispatcher.CheckAccess())
                CheckCloseWhenDone();
            else
                Dispatcher.BeginInvoke(new Action(CheckCloseWhenDone));
        }

        private void CheckCloseWhenDone()
        {
            if (CloseWhenDone && WorkQueue.UnprocessedJobCount == 0)
                Close();
        }

        private void btnRemoveDone_Click(object sender, RoutedEventArgs e)
        {
            WorkQueue.RemoveDone();
        }

        private void GeneratorProgressDialog_OnClosing(object sender, CancelEventArgs e)
        {
            WorkQueue.JobStarted -= WorkQueueOnJobStarted;
            WorkQueue.JobFinished -= WorkQueueOnJobFinished;
        }

        private void GeneratorProgressDialog_OnLoaded(object sender, RoutedEventArgs e)
        {
            WorkQueue.JobStarted += WorkQueueOnJobStarted;
            WorkQueue.JobFinished += WorkQueueOnJobFinished;
        }

        private void MnuCancel_Click(object sender, RoutedEventArgs e)
        {
            foreach (GeneratorEntry entry in dataGrid.SelectedItems)
            {
                entry.Job.Cancel();
            }
        }
    }
}
