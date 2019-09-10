using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using JetBrains.Annotations;
using ScriptPlayer.Generators;

namespace ScriptPlayer.ViewModels
{
    public class ThumbnailGeneratorSettingsViewModel : INotifyPropertyChanged
    {
        private int _intervall;
        private int _width;
        private int _height;
        private bool _autoWidth;
        private bool _autoHeight;
        private bool _autoIntervall;

        public ThumbnailGeneratorSettingsViewModel(ThumbnailGeneratorSettings settings)
        {
            Height = settings.Height;
            Width = settings.Width;
            Intervall = settings.Intervall;
        }

        public int Intervall
        {
            get => _intervall;
            set
            {
                if (value == _intervall) return;
                _intervall = value;
                OnPropertyChanged();
            }
        }

        public int Width
        {
            get => _width;
            set
            {
                if (value == _width) return;
                _width = value;
                OnPropertyChanged();
            }
        }

        public int Height
        {
            get => _height;
            set
            {
                if (value == _height) return;
                _height = value;
                OnPropertyChanged();
            }
        }

        public bool AutoWidth
        {
            get => _autoWidth;
            set
            {
                if (value == _autoWidth) return;
                _autoWidth = value;
                OnPropertyChanged();
            }
        }

        public bool AutoHeight
        {
            get => _autoHeight;
            set
            {
                if (value == _autoHeight) return;
                _autoHeight = value;
                OnPropertyChanged();
            }
        }

        public bool AutoIntervall
        {
            get => _autoIntervall;
            set
            {
                if (value == _autoIntervall) return;
                _autoIntervall = value;
                OnPropertyChanged();
            }
        }

        public ThumbnailGeneratorSettingsViewModel()
        {
            Width = 200;
            Height = 150;
            AutoHeight = true;
            AutoIntervall = true;
            Intervall = 10;
        }

        public ThumbnailGeneratorSettings GetSettings(out string[] errorMessages)
        {
            List<string> errors = new List<string>();

            if (Width <= 0 && !AutoWidth)
            {
                errors.Add("Width must be greater than zero or automatic");
            }

            if (Height <= 0 && !AutoHeight)
            {
                errors.Add("Height must be greater than zero or automatic");
            }

            if (Intervall <= 0 && !AutoIntervall)
            {
                errors.Add("Intervall must be greater than zero");
            }

            errorMessages = errors.ToArray();
            if (errors.Any())
                return null;

            var result = new ThumbnailGeneratorSettings
            {
                Height = AutoHeight ? -1 : Height,
                Width = AutoWidth ? -1 : Width,
                Intervall = AutoIntervall ? -1 : Intervall,
            };

            return result;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ThumbnailGeneratorSettingsViewModel Duplicate()
        {
            return new ThumbnailGeneratorSettingsViewModel
            {
                Width = Width,
                Height = Height,
                Intervall = Intervall,
                AutoHeight = AutoHeight,
                AutoIntervall = AutoIntervall,
                AutoWidth = AutoWidth
            };
        }
    }
}