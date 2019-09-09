using System.ComponentModel;
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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
