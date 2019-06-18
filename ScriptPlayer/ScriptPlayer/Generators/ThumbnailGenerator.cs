using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Classes;
using ScriptPlayer.Shared.Classes.Wrappers;

namespace ScriptPlayer.Generators
{
    public class ThumbnailGenerator : FfmpegGenerator<ThumbnailGeneratorSettings>
    {
        private bool _canceled;
        private FfmpegWrapper _wrapper;

        protected override string ProcessingType => "Thumbnails";

        protected override void ProcessInternal(ThumbnailGeneratorSettings settings, GeneratorEntry entry)
        {
            try
            {
                entry.State = JobStates.Processing;

                _wrapper = new FfmpegWrapper(FfmpegExePath);

                double intervall = settings.Intervall;

                if (settings.Intervall < 1)
                {
                    var info = _wrapper.GetVideoInfo(settings.VideoFile);
                    if (!info.IsGoodEnough())
                    {
                        entry.Update("Failed", 1);
                        entry.DoneType = JobDoneTypes.Failure;
                        return;
                    }

                    intervall = info.Duration.TotalSeconds / 500.0;
                    intervall = Math.Min(Math.Max(1, intervall), 10);
                }

                FrameConverterArguments arguments = new FrameConverterArguments
                {
                    StatusUpdateHandler = (progress) => { entry.Update(null, progress); },
                    InputFile = settings.VideoFile,
                    OutputDirectory = FfmpegWrapper.CreateRandomTempDirectory(),
                    Intervall = intervall,
                    Width = settings.Width,
                    Height = settings.Height
                };
                
                string thumbfile = Path.ChangeExtension(settings.VideoFile, "thumbs");
                entry.Update("Extracting Frames", 0);

                var frames =_wrapper.ExtractFrames(arguments);

                if (_canceled)
                    return;

                entry.Update("Saving Thumbnails", 1);

                VideoThumbnailCollection thumbnails = new VideoThumbnailCollection();
                foreach(var frame in frames)
                    thumbnails.Add(frame.Item1, frame.Item2);

                using (FileStream stream = new FileStream(thumbfile, FileMode.Create, FileAccess.Write))
                {
                    thumbnails.Save(stream);
                }

                thumbnails.Dispose();

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