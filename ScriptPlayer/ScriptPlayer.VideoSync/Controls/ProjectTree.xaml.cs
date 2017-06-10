using System.Collections.ObjectModel;
using System.Windows.Controls;
using ScriptPlayer.Shared;

namespace ScriptPlayer.VideoSync.Controls
{
    /// <summary>
    /// Interaction logic for ProjectTree.xaml
    /// </summary>
    public partial class ProjectTree : UserControl
    {
        public ProjectTree()
        {
            InitializeComponent();
        }
    }

    public class BeatProject
    {
        public string VideoFile { get; set; }
        public ObservableCollection<FrameCaptureCollection> FrameCaptures { get; set; }
        public ObservableCollection<BeatCollection> Beats { get; set; }
        public ObservableCollection<SampleCondition> SampleConditions { get; set; }
        
    }
}
