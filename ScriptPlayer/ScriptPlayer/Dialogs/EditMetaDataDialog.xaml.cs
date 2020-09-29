using System;
using System.Linq;
using System.Windows;
using ScriptPlayer.Shared.Scripts;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for EditMetaDataDialog.xaml
    /// </summary>
    public partial class EditMetaDataDialog : Window
    {
        public FunScriptMetaData MetaData { get; set; }

        public EditMetaDataDialog(FunScriptMetaData initialValues = null)
        {
            InitializeComponent();

            if (initialValues != null)
            {
                txtCreator.Text = initialValues.Creator;
                txtOriginalName.Text = initialValues.OriginalName;
                txtUrl.Text = initialValues.Url;
                txtVideoUrl.Text = initialValues.UrlVideo;
                txtComment.Text = initialValues.Comment;
                cckPaid.IsChecked = initialValues.Paid;

                txtTags.Text = initialValues.Tags == null ? "" : string.Join("; ", initialValues.Tags);
                txtPerformers.Text = initialValues.Performers == null ? "" : string.Join("; ", initialValues.Performers);
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            MetaData = new FunScriptMetaData();

            MetaData.Creator = txtCreator.Text;
            MetaData.OriginalName = txtOriginalName.Text;
            MetaData.Url = txtUrl.Text;
            MetaData.UrlVideo = txtVideoUrl.Text;
            MetaData.Comment = txtComment.Text;
            MetaData.Paid = cckPaid.IsChecked == true;

            MetaData.Tags = txtTags.Text
                .Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            MetaData.Performers = txtPerformers.Text
                .Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            DialogResult = true;
        }
    }
}
