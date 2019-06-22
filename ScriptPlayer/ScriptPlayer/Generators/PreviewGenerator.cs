using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Classes.Wrappers;

namespace ScriptPlayer.Generators
{
    public class PreviewGenerator : FfmpegGenerator<PreviewGeneratorSettings>
    {
        public event EventHandler<Tuple<bool, string>> Done;

        private FfmpegWrapper _wrapper;

        private Thread _thread;
        private bool _canceled;

        public void ProcessInThread(PreviewGeneratorSettings settings, GeneratorEntry entry)
        {
            _thread = new Thread(() =>
            {
                Process(settings, entry);
            });

            _thread.Start();
        }

        protected override string ProcessingType => "Preview GIF";

        protected override GeneratorResult ProcessInternal(PreviewGeneratorSettings settings, GeneratorEntry entry)
        {
            entry.State = JobStates.Processing;

            List<string> tempFiles = new List<string>();

            try
            {
                _wrapper = new FfmpegWrapper(FfmpegExePath);

                List<string> sectionFileNames = new List<string>();

                if (settings.TimeFrames.Any(tf => tf.IsFactor))
                {
                    VideoInfo info = _wrapper.GetVideoInfo(settings.VideoFile);
                   
                    if (!info.IsGoodEnough())
                    {
                        entry.State = JobStates.Done;
                        entry.DoneType = JobDoneTypes.Failure;
                        entry.Update("Failed", 1);
                        return GeneratorResult.Failed();
                    }

                    TimeSpan duration = info.Duration;

                    settings.TimeFrames.ForEach(tf => tf.CalculateStart(duration));
                }

                ClipExtractorArguments clipArguments = new ClipExtractorArguments
                {
                    InputFile = settings.VideoFile,
                    Width = settings.Width,
                    Height = settings.Height,
                    Framerate = settings.Framerate
                };
                
                for (int i = 0; i < settings.TimeFrames.Count; i++)
                {
                    string sectionFileName = Path.Combine(Path.GetTempPath(),
                        Path.GetFileName(settings.VideoFile) + $"-clip_{i}.mkv");
                    sectionFileNames.Add(sectionFileName);
                    tempFiles.Add(sectionFileName);

                    var timeFrame = settings.TimeFrames[i];

                    clipArguments.Duration = timeFrame.Duration;
                    clipArguments.StartTimeSpan = timeFrame.StartTimeSpan;
                    clipArguments.OutputFile = sectionFileName;

                    entry.Update($"Generating GIF (1/4): Clipping Video Section {i + 1}/{settings.TimeFrames.Count}",
                        ((i / (double) settings.TimeFrames.Count)) / 4.0);

                    _wrapper.Execute(clipArguments);

                    if (_canceled)
                        return GeneratorResult.Failed();
                }

                entry.Update("Generating GIF (2/4): Merging Clips", 1 / 4.0);

                string clipFileName = "";

                if (sectionFileNames.Count == 1)
                {
                    clipFileName = sectionFileNames[0];
                }
                else
                {
                    ClipMergeArguments mergeArguments = new ClipMergeArguments
                    {
                        InputFile = settings.VideoFile,
                        ClipFiles = sectionFileNames.ToArray(),
                        OutputFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(settings.VideoFile) + $"-clip.mkv")
                    };

                    clipFileName = mergeArguments.OutputFile;
                    tempFiles.Add(clipFileName);

                    _wrapper.Execute(mergeArguments);

                    if (_canceled)
                        return GeneratorResult.Failed();
                }

                entry.Update("Generating GIF (3/4): Extracting Palette", 2 / 4.0);

                string paletteFile = clipFileName + "-palette.png";

                PaletteExtractorArguments paletteArguments = new PaletteExtractorArguments
                {
                    InputFile = clipFileName,
                    OutputFile = paletteFile
                };

                tempFiles.Add(paletteFile);

                _wrapper.Execute(paletteArguments);

                if (_canceled)
                    return GeneratorResult.Failed();

                entry.Update("Generating GIF (3/4): Creating GIF", 3 / 4.0);

                string gifFileName = settings.OutputFile;

                GifCreatorArguments gifArguments = new GifCreatorArguments();
                gifArguments.InputFile = clipFileName;
                gifArguments.PaletteFile = paletteFile;
                gifArguments.OutputFile = gifFileName;
                gifArguments.Framerate = settings.Framerate;

                _wrapper.Execute(gifArguments);

                if (_canceled)
                    return GeneratorResult.Failed();

                entry.Update("Done!", 4 / 4.0);
                bool success = File.Exists(gifFileName);

                if(success)
                    entry.DoneType = JobDoneTypes.Success;
                else
                    entry.DoneType = JobDoneTypes.Failure;

                OnDone(new Tuple<bool, string>(success, gifFileName));

                if(success)
                    return GeneratorResult.Succeeded(gifFileName);

                return GeneratorResult.Failed();
            }
            catch
            {
                entry.DoneType = JobDoneTypes.Failure;
                return GeneratorResult.Failed();
            }
            finally
            {
                entry.State = JobStates.Done;

                if (_canceled)
                {
                    entry.DoneType = JobDoneTypes.Cancelled;
                }

                foreach (string tempFile in tempFiles)
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
            }
        }

        public override void Cancel()
        {
            _canceled = true;
            _wrapper?.Cancel();

            if (_thread == null)
                return;

            if (_thread.Join(TimeSpan.FromSeconds(5)))
                _thread.Abort();
            _thread = null;
        }

        protected virtual void OnDone(Tuple<bool, string> e)
        {
            Done?.Invoke(this, e);
        }

        public PreviewGenerator(string ffmpegExePath) : base(ffmpegExePath)
        {
        }
    }
}