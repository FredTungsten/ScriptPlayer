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

            int totalSteps = settings.OverlayScriptPositions ? 5 : 4;
            int stepsDone = 0;

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
                    Framerate = settings.Framerate,
                    ClipLeft = settings.ClipLeft,
                    DeLense = settings.ClipLeft
                };

                VideoInfo clipInfo = null;

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
                    
                    entry.Update($"Generating GIF ({stepsDone + 1}/{totalSteps}): Clipping Video Section {i + 1}/{settings.TimeFrames.Count}",
                        (stepsDone / (double)totalSteps) + ((i / (double) settings.TimeFrames.Count)) / totalSteps);

                    if(!_wrapper.Execute(clipArguments))
                        return GeneratorResult.Failed();

                    if(clipInfo == null)
                        clipInfo = _wrapper.GetVideoInfo(sectionFileName);

                    if (_canceled)
                        return GeneratorResult.Failed();
                }

                stepsDone++;

                if (settings.OverlayScriptPositions)
                {

                    string script = ViewModel.GetScriptFile(settings.VideoFile);
                    var actions = ViewModel.LoadScriptActions(script, null)?.OfType<FunScriptAction>().ToList();

                    Size barSize = new Size(clipInfo.Resolution.Horizontal, 20);

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

                            entry.Update($"Generating GIF ({stepsDone + 1}/{totalSteps}): Overlaying Positions {i + 1}/{settings.TimeFrames.Count}",
                                (stepsDone / (double)totalSteps) + ((i / (double)settings.TimeFrames.Count)) / totalSteps);

                            double expectedFrames = 1 + (settings.TimeFrames[i].Duration.TotalSeconds * clipInfo.FrameRate);

                            while (progress <= max)
                            {
                                progress = start + TimeSpan.FromSeconds(frame / clipInfo.FrameRate);

                                bar.Progress = progress;

                                var bitmap = RenderToBitmap(bar, barSize);

                                PngBitmapEncoder encoder = new PngBitmapEncoder();

                                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                                using (FileStream f = new FileStream(Path.Combine(tempDir, $"frame{frame:0000}.png"),
                                    FileMode.CreateNew))
                                    encoder.Save(f);

                                frame++;
                            }

                            FrameMergeArguments frameMergeArguments = new FrameMergeArguments
                            {
                                Framerate = clipInfo.FrameRate,
                                InputFile = Path.Combine(tempDir, "frame%04d.png"),
                                OutputFile = overlayFileName
                            };

                            if (!_wrapper.Execute(frameMergeArguments))
                                return GeneratorResult.Failed();

                            var clipInfo2 = _wrapper.GetVideoInfo(overlayFileName);

                            Directory.Delete(tempDir, true);

                            string mergeFileName = Path.Combine(Path.GetTempPath(),
                                Path.GetFileName(settings.VideoFile) + $"-merge_{i}.mkv");

                            tempFiles.Add(mergeFileName);

                            VideoOverlayArguments overlayArguments = new VideoOverlayArguments
                            {
                                InputFile = sectionFileNames[i],
                                Overlay = overlayFileName,
                                OutputFile = mergeFileName,
                                PosX = 0,
                                PosY = clipInfo.Resolution.Vertical - 20
                            };

                            if (!_wrapper.Execute(overlayArguments))
                                return GeneratorResult.Failed();

                            sectionFileNames[i] = mergeFileName;
                        }
                    }

                    stepsDone++;
                }

                entry.Update($"Generating GIF ({stepsDone + 1}/{totalSteps}): Merging Clips", stepsDone / (double)totalSteps);

                string clipFileName;

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

                stepsDone++;

                entry.Update($"Generating GIF ({stepsDone + 1}/{totalSteps}): Extracting Palette", stepsDone / (double)totalSteps);

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

                stepsDone++;

                entry.Update($"Generating GIF ({stepsDone + 1}/{totalSteps}): Creating GIF", stepsDone / (double)totalSteps);

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

                stepsDone++;

                entry.Update("Done", 1.0);
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
        public int PosX { get; set; }
        public int PosY { get; set; }

        public override string BuildArguments()
        {
            return
                $"-i \"{InputFile}\" -i \"{Overlay}\" " +
                //$"-filter_complex \"[0:v]pad=iw:ih+20:0:0[main];[main][1:v]overlay=y=H-h[out]\" " +
                $"-filter_complex \"[0:v][1:v]vstack[out]\" " +
                $"-map \"[out]\" \"{OutputFile}\"";
        }
    }

    public class FrameMergeArguments : FfmpegArguments
    {
        public override string BuildArguments()
        {
            string framerate = Framerate.ToString("F2", CultureInfo.InvariantCulture);

            return
                "-y " +                      //Yes to override existing files
                $"-framerate {framerate} " + // Frmaerate in
                $"-i \"{InputFile}\" " +     // Input File
                $"-r {framerate} " +         // Framerate out
                "-vcodec libx264 -crf 0 " +
                $"\"{OutputFile}\"";
        }

        public double Framerate { get; set; }
        public string OutputFile { get; set; }
    }
}