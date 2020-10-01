using System;
using System.IO;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Classes;
using ScriptPlayer.Shared.Classes.Wrappers;
using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Generators
{
    public class ThumbnailGenerator : FfmpegGenerator<ThumbnailGeneratorSettings>
    {
        private bool _canceled;
        private FfmpegWrapper _wrapper;

        protected override string ProcessingType => "Thumbnails";

        protected override GeneratorResult ProcessInternal(ThumbnailGeneratorSettings settings, GeneratorEntry entry)
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
                        return GeneratorResult.Failed();
                    }

                    const double targetFrameCount = 500.0;

                    intervall = info.Duration.TotalSeconds / targetFrameCount;
                    intervall = Math.Min(Math.Max(1, intervall), 10);
                }

                FrameConverterArguments arguments = new FrameConverterArguments
                {
                    StatusUpdateHandler = (progress) => { entry.Update(null, progress); },
                    InputFile = settings.VideoFile,
                    OutputDirectory = FfmpegWrapper.CreateRandomTempDirectory(),
                    Intervall = intervall,
                    Width = settings.Width,
                    Height = settings.Height,
                    ClipLeft = settings.ClipLeft,
                    DeLense = settings.ClipLeft
                };
                
                string thumbfile = Path.ChangeExtension(settings.VideoFile, "thumbs");
                entry.Update("Extracting Frames", 0);

                var frames =_wrapper.ExtractFrames(arguments);

                if (_canceled)
                    return GeneratorResult.Failed();

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

                return GeneratorResult.Succeeded(thumbfile);
            }
            catch (Exception)
            {
                entry.DoneType = JobDoneTypes.Failure;
                return GeneratorResult.Failed();
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
            _wrapper?.Cancel();
        }

        public ThumbnailGenerator(MainViewModel viewModel) : base(viewModel)
        {
        }
    }
}