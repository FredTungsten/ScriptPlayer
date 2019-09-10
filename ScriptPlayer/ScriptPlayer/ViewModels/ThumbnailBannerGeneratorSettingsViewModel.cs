using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using ScriptPlayer.Generators;

namespace ScriptPlayer.ViewModels
{
    public class ThumbnailBannerGeneratorSettingsViewModel : INotifyPropertyChanged
    {
        private int _columns;
        private int _rows;
        private int _totalWidth;
        public event PropertyChangedEventHandler PropertyChanged;

        public int Columns
        {
            get => _columns;
            set
            {
                if (value == _columns) return;
                _columns = value;
                OnPropertyChanged();
            }
        }

        public int Rows
        {
            get => _rows;
            set
            {
                if (value == _rows) return;
                _rows = value;
                OnPropertyChanged();
            }
        }

        public int TotalWidth
        {
            get => _totalWidth;
            set
            {
                if (value == _totalWidth) return;
                _totalWidth = value;
                OnPropertyChanged();
            }
        }

        public ThumbnailBannerGeneratorSettingsViewModel()
        {
            Rows = 5;
            Columns = 4;
            TotalWidth = 1024;
        }

        public ThumbnailBannerGeneratorSettings GetSettings(out string[] errorMessages)
        {
            List<string> errors = new List<string>();

            if (Rows <= 0)
            {
                errors.Add("Rows must be greater than 0");
            }

            if (Columns <= 0)
            {
                errors.Add("Columns must be greater than 0");
            }

            if (TotalWidth <= 0)
            {
                errors.Add("Total width must be greater than 0");
            }

            errorMessages = errors.ToArray();
            if (errors.Any())
                return null;

            return new ThumbnailBannerGeneratorSettings
            {
                Columns = Columns,
                Rows = Rows,
                TotalWidth = TotalWidth
            };
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ThumbnailBannerGeneratorSettingsViewModel Duplicate()
        {
            return new ThumbnailBannerGeneratorSettingsViewModel
            {
                Rows = Rows,
                TotalWidth = TotalWidth,
                Columns = Columns
            };
        }
    }
}