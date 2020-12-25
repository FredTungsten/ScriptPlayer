using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using JetBrains.Annotations;
using ScriptPlayer.Generators;

namespace ScriptPlayer.ViewModels
{
    public class GeneralGeneratorSettingsViewModel : INotifyPropertyChanged
    {
        private bool _generateHeatmap;
        private bool _generatePreview;
        private bool _generateThumbnailBanner;
        private bool _generateThumbnails;

        [XmlElement("GenerateThumbnails")]
        public bool GenerateThumbnails
        {
            get => _generateThumbnails;
            set
            {
                if (value == _generateThumbnails) return;
                _generateThumbnails = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("GenerateThumbnailBanner")]
        public bool GenerateThumbnailBanner
        {
            get => _generateThumbnailBanner;
            set
            {
                if (value == _generateThumbnailBanner) return;
                _generateThumbnailBanner = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("GeneratePreview")]
        public bool GeneratePreview
        {
            get => _generatePreview;
            set
            {
                if (value == _generatePreview) return;
                _generatePreview = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("GenerateHeatmap")]
        public bool GenerateHeatmap
        {
            get => _generateHeatmap;
            set
            {
                if (value == _generateHeatmap) return;
                _generateHeatmap = value;
                OnPropertyChanged();
            }
        }

        private ExistingFileStrategy _existingFileStrategy;
        private string _saveFilesToThisPath;
        private bool _saveFilesToDifferentPath;

        [XmlElement("SaveFilesToDifferentPath")]
        public bool SaveFilesToDifferentPath
        {
            get => _saveFilesToDifferentPath;
            set
            {
                if (value == _saveFilesToDifferentPath) return;
                _saveFilesToDifferentPath = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("SaveFilesToThisPath")]
        public string SaveFilesToThisPath
        {
            get => _saveFilesToThisPath;
            set
            {
                if (value == _saveFilesToThisPath) return;
                _saveFilesToThisPath = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("ExistingFileStrategy")]
        public ExistingFileStrategy ExistingFileStrategy
        {
            get => _existingFileStrategy;
            set
            {
                if (value == _existingFileStrategy) return;
                _existingFileStrategy = value;
                OnPropertyChanged();
            }
        }

        public GeneralGeneratorSettingsViewModel()
        {
            ExistingFileStrategy = ExistingFileStrategy.Skip;
            SaveFilesToDifferentPath = false;

            GenerateHeatmap = true;
            GeneratePreview = true;
            GenerateThumbnails = true;
            GenerateThumbnailBanner = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public GeneralGeneratorSettingsViewModel Duplicate()
        {
            return new GeneralGeneratorSettingsViewModel()
            {
                ExistingFileStrategy = ExistingFileStrategy,
                SaveFilesToDifferentPath = SaveFilesToDifferentPath,
                SaveFilesToThisPath = SaveFilesToThisPath,
                GenerateThumbnails = GenerateThumbnails,
                GenerateHeatmap = GenerateHeatmap,
                GeneratePreview = GeneratePreview,
                GenerateThumbnailBanner = GenerateThumbnailBanner
            };
        }

        /// <summary>
        /// true = skip, false = don't skip
        /// </summary>
        public bool ApplyToOrSkip(string video, FfmpegGeneratorSettings settings, string extension)
        {
            settings.VideoFile = video;
            settings.SkipIfExists = false;

            string fileName = Path.GetFileNameWithoutExtension(video);
            string newDirectory = SaveFilesToDifferentPath ? SaveFilesToThisPath : Path.GetDirectoryName(video);
            string newPath = Path.Combine(newDirectory, fileName + "." + extension);

            if (File.Exists(newPath))
            {
                switch (ExistingFileStrategy)
                {
                    case ExistingFileStrategy.Skip:
                        return true;
                    case ExistingFileStrategy.Replace:
                    {
                        settings.OutputFile = newPath;
                        break;
                    }
                    case ExistingFileStrategy.RenameOld:
                    {
                        settings.OutputFile = newPath;
                        settings.RenameBeforeExecute = new JitRenamer(newDirectory, fileName, extension);
                        break;
                    }
                    case ExistingFileStrategy.RenameNew:
                    {
                        settings.OutputFile = JitRenamer.FindNextName(newDirectory, fileName, extension);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                settings.OutputFile = newPath;
            }

            //false = don't skip
            return false;
        }
    }

    public class JitRenamer
    {
        private readonly string _directory;
        private readonly string _fileName;
        private readonly string _extension;
        private readonly string _existingFile;

        public bool RenameNow()
        {
            try
            {
                string newName = FindNextName(_directory, _fileName, _extension);
                File.Move(_existingFile, newName);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in JitRenamer.RenameNow: " + ex.Message);
                return false;
            }
        }

        public JitRenamer(string directory, string fileName, string extension)
        {
            _existingFile = Path.Combine(directory, $"{fileName}.{extension}");
            _directory = directory;
            _fileName = fileName;
            _extension = extension;
        }

        public static string FindNextName(string directory, string fileName, string extension)
        {
            int i = 1;
            string newPath;

            do
            {
                newPath = Path.Combine(directory, $"{fileName}_{i}.{extension}");
                i++;
            } while (File.Exists(newPath));

            return newPath;
        }
    }
}