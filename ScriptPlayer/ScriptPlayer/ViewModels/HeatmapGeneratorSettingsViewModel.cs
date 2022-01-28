using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using JetBrains.Annotations;
using ScriptPlayer.Generators;

namespace ScriptPlayer.ViewModels
{
    public class HeatmapGeneratorSettingsViewModel : INotifyPropertyChanged
    {
        private int _width;
        private int _height;
        private bool _addShadow;
        private bool _showMovementRange;
        private bool _transparentBackground;

        [XmlElement("Width")]
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

        [XmlElement("Height")]
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

        [XmlElement("AddShadow")]
        public bool AddShadow
        {
            get => _addShadow;
            set
            {
                if (value == _addShadow) return;
                _addShadow = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("ShowMovementRange")]
        public bool ShowMovementRange
        {
            get => _showMovementRange;
            set
            {
                if (value == _showMovementRange) return;
                _showMovementRange = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("TransparentBackground")]
        public bool TransparentBackground
        {
            get => _transparentBackground;
            set
            {
                if (value == _transparentBackground) return;
                _transparentBackground = value;
                OnPropertyChanged();
            }
        }

        public HeatmapGeneratorSettingsViewModel()
        {
            Width = 400;
            Height = 20;
            AddShadow = true;
        }

        public HeatmapGeneratorSettings GetSettings(out string[] errorMessages)
        {
            List<string> errors = new List<string>();

            if (Width <= 0)
            {
                errors.Add("Width must be greater than 0");
            }

            if (Height <= 0)
            {
                errors.Add("Height must be greater than 0");
            }

            errorMessages = errors.ToArray();
            if (errors.Any())
                return null;

            return new HeatmapGeneratorSettings
            {
                Width = Width,
                Height = Height,
                AddShadow = AddShadow,
                TransparentBackground = TransparentBackground,
                MovementRange = ShowMovementRange
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public HeatmapGeneratorSettingsViewModel Duplicate()
        {
            return new HeatmapGeneratorSettingsViewModel()
            {
                Width = Width,
                Height = Height,
                AddShadow = AddShadow,
                ShowMovementRange = ShowMovementRange,
                TransparentBackground = TransparentBackground
            };
        }
    }
}