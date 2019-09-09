using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace ScriptPlayer.ViewModels
{
    public class GeneralGeneratorSettingsViewModel : INotifyPropertyChanged
    {
        public bool SaveFilesToDifferentPath { get; set; }

        public string SaveFilesToThisPath { get; set; }

        public ExistingFileStrategy ExistingFileStrategy { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}