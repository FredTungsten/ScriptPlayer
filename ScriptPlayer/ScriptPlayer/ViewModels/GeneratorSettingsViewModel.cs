using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace ScriptPlayer.ViewModels
{
    public class GeneratorSettingsViewModel : INotifyPropertyChanged
    {
        private ThumbnailGeneratorSettingsViewModel _thumbnails;
        private ThumbnailBannerGeneratorSettingsViewModel _banner;
        private PreviewGeneratorSettingsViewModel _preview;
        private HeatmapGeneratorSettingsViewModel _heatmap;
        private GeneralGeneratorSettingsViewModel _general;

        public ThumbnailGeneratorSettingsViewModel Thumbnails
        {
            get => _thumbnails;
            set
            {
                if (Equals(value, _thumbnails)) return;
                _thumbnails = value;
                OnPropertyChanged();
            }
        }

        public ThumbnailBannerGeneratorSettingsViewModel Banner
        {
            get => _banner;
            set
            {
                if (Equals(value, _banner)) return;
                _banner = value;
                OnPropertyChanged();
            }
        }

        public PreviewGeneratorSettingsViewModel Preview
        {
            get => _preview;
            set
            {
                if (Equals(value, _preview)) return;
                _preview = value;
                OnPropertyChanged();
            }
        }

        public HeatmapGeneratorSettingsViewModel Heatmap
        {
            get => _heatmap;
            set
            {
                if (Equals(value, _heatmap)) return;
                _heatmap = value;
                OnPropertyChanged();
            }
        }

        public GeneralGeneratorSettingsViewModel General
        {
            get => _general;
            set
            {
                if (Equals(value, _general)) return;
                _general = value;
                OnPropertyChanged();
            }
        }

        public GeneratorSettingsViewModel()
        {
            Banner = new ThumbnailBannerGeneratorSettingsViewModel();
            Thumbnails = new ThumbnailGeneratorSettingsViewModel();   
            Preview = new PreviewGeneratorSettingsViewModel();
            Heatmap = new HeatmapGeneratorSettingsViewModel();
            General = new GeneralGeneratorSettingsViewModel();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool HasErrors(out string[] errorMessages)
        {
            List<string> errors = new List<string>();

            this.Banner.GetSettings(out string[] errBanner);
            errors.AddRange(errBanner.Select(err => "Thumbnail Banner: " + err));

            this.Thumbnails.GetSettings(out string[] errThumbs);
            errors.AddRange(errThumbs.Select(err => "Thumbnails: " + err));

            this.Preview.GetSettings(out string[] errPreview);
            errors.AddRange(errPreview.Select(err => "Preview: " + err));

            this.Heatmap.GetSettings(out string[] errHeatmap);
            errors.AddRange(errHeatmap.Select(err => "Heatmap: " + err));

            errorMessages = errors.ToArray();

            return errors.Count > 0;
        }

        public GeneratorSettingsViewModel Duplicate()
        {
            return new GeneratorSettingsViewModel
            {
                Banner = Banner.Duplicate(),
                Thumbnails = Thumbnails.Duplicate(),
                Heatmap = Heatmap.Duplicate(),
                General = General.Duplicate(),
                Preview = Preview.Duplicate(),
            };
        }
    }
}
