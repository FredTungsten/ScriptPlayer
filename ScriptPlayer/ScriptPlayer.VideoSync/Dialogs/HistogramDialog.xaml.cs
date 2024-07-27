using System.Collections.Generic;
using System.Windows;
using ScriptPlayer.VideoSync.Controls;

namespace ScriptPlayer.VideoSync.Dialogs
{
    /// <summary>
    /// Interaction logic for HistogramDialog.xaml
    /// </summary>
    public partial class HistogramDialog : Window
    {
        public HistogramDialog(List<HistogramEntry> entries)
        {
            InitializeComponent();
            histogram.Entries = entries;
        }
    }
}
