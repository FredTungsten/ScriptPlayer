using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Classes.Wrappers;
using ScriptPlayer.Shared.Scripts;
using ScriptPlayer.ViewModels;

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
            _thread.SetApartmentState(ApartmentState.STA);
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
                List<string> overlayFileNames = new List<string>();

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
                    Framerate = settings.Framerate,
                    ClipLeft = settings.ClipLeft,
                    DeLense = settings.ClipLeft
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

                    if(!_wrapper.Execute(clipArguments))
                        return GeneratorResult.Failed();

                    if (_canceled)
                        return GeneratorResult.Failed();
                }

                string script = ViewModel.GetScriptFile(settings.VideoFile);
                var actions = ViewModel.LoadScriptActions(script, null)?.OfType<FunScriptAction>().ToList();

                Size barSize = new Size(200, 20);

                if (actions != null && actions.Count > 0)
                {
                    PositionBar bar = new PositionBar
                    {
                        Width = barSize.Width,
                        Height = barSize.Height,
                        TotalDisplayedDuration = TimeSpan.FromSeconds(5),
                        Background = Brushes.Black,
                        Positions = new PositionCollection(actions.Select(a => new TimedPosition()
                        {
                            Position = a.Position,
                            TimeStamp = a.TimeStamp
                        })),
                        DrawCircles = false,
                        DrawLines = false,
                    };

                    for (int i = 0; i < settings.TimeFrames.Count; i++)
                    {
                        TimeSpan start = settings.TimeFrames[i].StartTimeSpan;
                        TimeSpan max = start + settings.TimeFrames[i].Duration;

                        TimeSpan progress = start;

                        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                        Directory.CreateDirectory(tempDir);

                        string overlayFileName = Path.Combine(Path.GetTempPath(),
                            Path.GetFileName(settings.VideoFile) + $"-overlay_{i}.mkv");

                        tempFiles.Add(overlayFileName);

                        int frame = 0;

                        while (progress <= max)
                        {
                            bar.Progress = progress;

                            var bitmap = RenderToBitmap(bar, barSize);

                            PngBitmapEncoder encoder = new PngBitmapEncoder();

                            encoder.Frames.Add(BitmapFrame.Create(bitmap));

                            using(FileStream f = new FileStream(Path.Combine(tempDir, $"frame{frame:0000}.png"), FileMode.CreateNew))
                                encoder.Save(f);

                            frame++;

                            progress += TimeSpan.FromSeconds(1.0 / settings.Framerate);
                        }

                        _wrapper.Execute(new FrameMergeArguments
                        {
                            Framerate = settings.Framerate,
                            InputFile = Path.Combine(tempDir, "frame%04d.png"),
                            OutputFile = overlayFileName
                        });

                        Directory.Delete(tempDir, true);

                        string mergeFileName = Path.Combine(Path.GetTempPath(),
                            Path.GetFileName(settings.VideoFile) + $"-merge_{i}.mkv");

                        tempFiles.Add(mergeFileName);

                        _wrapper.Execute(new VideoOverlayArguments
                        {
                            InputFile = sectionFileNames[i],
                            Overlay = overlayFileName,
                            OutputFile = mergeFileName
                        });

                        sectionFileNames[i] = mergeFileName;
                    }
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

                    if(!_wrapper.Execute(mergeArguments))
                        return GeneratorResult.Failed();

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

                if(!_wrapper.Execute(paletteArguments))
                    return GeneratorResult.Failed();

                if (_canceled)
                    return GeneratorResult.Failed();

                entry.Update("Generating GIF (3/4): Creating GIF", 3 / 4.0);

                string gifFileName = settings.OutputFile;

                GifCreatorArguments gifArguments = new GifCreatorArguments();
                gifArguments.InputFile = clipFileName;
                gifArguments.PaletteFile = paletteFile;
                gifArguments.OutputFile = gifFileName;
                gifArguments.Framerate = settings.Framerate;

                if(!_wrapper.Execute(gifArguments))
                    return GeneratorResult.Failed();

                if (_canceled)
                    return GeneratorResult.Failed();

                entry.Update("Done", 4 / 4.0);
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
            catch(Exception e)
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

        public BitmapSource RenderToBitmap(UIElement element, Size size)
        {
            element.Measure(size);
            element.Arrange(new Rect(size));
            element.UpdateLayout();

            var bitmap = new RenderTargetBitmap(
                (int)size.Width, (int)size.Height, 96, 96, PixelFormats.Default);

            bitmap.Render(element);
            return bitmap;
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

        public PreviewGenerator(MainViewModel viewModel) : base(viewModel)
        {
        }
    }

    public class VideoOverlayArguments : FfmpegArguments
    {
        public string Overlay { get; set; }
        public string OutputFile { get; set; }

        public override string BuildArguments()
        {
            return
                $"-i \"{InputFile}\" -i \"{Overlay}\" " +
                $"-filter_complex overlay " +
                $"\"{OutputFile}\"";
        }
    }

    public class FrameMergeArguments : FfmpegArguments
    {
        public override string BuildArguments()
        {
            string framerate = Framerate.ToString("F2", CultureInfo.InvariantCulture);

            return
                "-y " + //Yes to override existing files
                $"-i \"{InputFile}\" " + // Input File
                $"-r {framerate} " +
                "-vcodec libx264 -crf 0 " +
                $"\"{OutputFile}\"";
        }

        public double Framerate { get; set; }
        public string OutputFile { get; set; }
    }
}