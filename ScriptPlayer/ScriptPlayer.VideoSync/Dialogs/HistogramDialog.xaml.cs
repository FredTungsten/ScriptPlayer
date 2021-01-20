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
