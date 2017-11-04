using System.Windows;
using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for VersionDialog.xaml
    /// </summary>
    public partial class VersionDialog : Window
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof(VersionViewModel), typeof(VersionDialog), new PropertyMetadata(default(VersionViewModel)));

        public VersionViewModel ViewModel
        {
            get { return (VersionViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public VersionDialog(VersionViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }

        private void VersionDialog_OnLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel.CheckIfYouHaventAlready();
        }
    }
}
