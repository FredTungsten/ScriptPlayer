using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Classes;

namespace ScriptPlayer.Generators
{
    public class ThumbnailGenerator : FfmpegGenerator<ThumbnailGeneratorSettings>
    {
        private bool _canceled;
        private FrameConverterWrapper _wrapper;

        protected override string ProcessingType => "Thumbnails";

        protected override void ProcessInternal(ThumbnailGeneratorSettings settings, GeneratorEntry entry)
        {
            try
            {
                entry.State = JobStates.Processing;

                _wrapper = new FrameConverterWrapper(FfmpegExePath)
                {
                    Intervall = settings.Intervall,
                    Width = settings.Width,
                    Height = settings.Height
                };

                _wrapper.ProgressChanged += (s, progress) => { entry.Update(null, progress); };

                string thumbfile = Path.ChangeExtension(settings.VideoFile, "thumbs");

                entry.Update("Extracting Frames", 0);

                _wrapper.VideoFile = settings.VideoFile;
                _wrapper.GenerateRandomOutputPath();
                string tempPath = _wrapper.OutputPath;
                _wrapper.Execute();

                if (_canceled)
                    return;

                entry.Update("Saving Thumbnails", 1);

                VideoThumbnailCollection thumbnails = new VideoThumbnailCollection();

                List<string> usedFiles = new List<string>();

                foreach (string file in Directory.EnumerateFiles(tempPath))
                {
                    string number = Path.GetFileNameWithoutExtension(file);
                    int index = int.Parse(number);

                    TimeSpan position = TimeSpan.FromSeconds(index * 10 - 5);

                    var frame = new BitmapImage();
                    frame.BeginInit();
                    frame.CacheOption = BitmapCacheOption.OnLoad;
                    frame.UriSource = new Uri(file, UriKind.Absolute);
                    frame.EndInit();

                    thumbnails.Add(position, frame);
                    usedFiles.Add(file);
                }

                using (FileStream stream = new FileStream(thumbfile, FileMode.Create, FileAccess.Write))
                {
                    thumbnails.Save(stream);
                }

                thumbnails.Dispose();

                foreach (string tempFile in usedFiles)
                    File.Delete(tempFile);

                Directory.Delete(tempPath);

                entry.DoneType = JobDoneTypes.Success;
                entry.Update("Done", 1);
            }
            catch (Exception)
            {
                entry.DoneType = JobDoneTypes.Failure;
            }
            finally
            {
                entry.State = JobStates.Done;

                if (_canceled)
                    entry.DoneType = JobDoneTypes.Cancelled;
            }
        }

        public override void Cancel()
        {
            _canceled = true;
            _wrapper.Cancel();
        }

        public ThumbnailGenerator(string ffmpegExePath) : base(ffmpegExePath)
        {
        }
    }
}