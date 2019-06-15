using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Helpers;

namespace ScriptPlayer.Generators
{
    public class PreviewGenerator : FfmpegGenerator<PreviewGeneratorSettings>
    {
        public event EventHandler<Tuple<bool, string>> Done;

        private ConsoleWrapper _wrapper;

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

        protected override void ProcessInternal(PreviewGeneratorSettings settings, GeneratorEntry entry)
        {
            entry.State = JobStates.Processing;

            List<string> tempFiles = new List<string>();

            try
            {
                _wrapper = new ConsoleWrapper(FfmpegExePath);
                List<string> sectionFileNames = new List<string>();

                if (settings.TimeFrames.Any(tf => tf.IsFactor))
                {
                    TimeSpan duration = MediaHelper.GetDuration(settings.VideoFile).Value;
                    settings.TimeFrames.ForEach(tf => tf.CalculateStart(duration));
                }

                for (int i = 0; i < settings.TimeFrames.Count; i++)
                {
                    string sectionFileName = Path.Combine(Path.GetTempPath(),
                        Path.GetFileName(settings.VideoFile) + $"-clip_{i}.mkv");
                    sectionFileNames.Add(sectionFileName);
                    tempFiles.Add(sectionFileName);

                    var timeFrame = settings.TimeFrames[i];

                    string clipArguments =
                        "-y " + //Yes to override existing files
                        $"-ss {timeFrame.StartTimeSpan:hh\\:mm\\:ss\\.ff} " + // Starting Position
                        $"-i \"{settings.VideoFile}\" " + // Input File
                        $"-t {timeFrame.Duration:hh\\:mm\\:ss\\.ff} " + // Duration
                        $"-r {settings.Framerate} " +
                        "-vf " + // video filter parameters" +
                        //$"select=\"mod(n-1\\,{_settings.FramerateDivisor})\"," +    // Every 2nd Frame
                        $"\"setpts=PTS-STARTPTS, hqdn3d=10, scale = {settings.Width}:{settings.Height}\" " +
                        "-vcodec libx264 -crf 0 " +
                        $"\"{sectionFileName}\"";

                    entry.Update($"Generating GIF (1/4): Clipping Video Section {i + 1}/{settings.TimeFrames.Count}",
                        ((i / (double) settings.TimeFrames.Count)) / 4.0);

                    _wrapper.Execute(clipArguments);

                    if (_canceled)
                        return;
                }

                entry.Update("Generating GIF (2/4): Merging Clips", 1 / 4.0);

                string clipFileName = "";

                if (sectionFileNames.Count == 1)
                {
                    clipFileName = sectionFileNames[0];
                }
                else
                {
                    string playlistFileName = Path.Combine(Path.GetTempPath(),
                        Path.GetFileName(settings.VideoFile) + $"-playlist.txt");
                    clipFileName = Path.Combine(Path.GetTempPath(), Path.GetFileName(settings.VideoFile) + $"-clip.mkv");

                    File.WriteAllLines(playlistFileName, sectionFileNames.Select(se => $"file '{se}'"));

                    tempFiles.Add(playlistFileName);
                    tempFiles.Add(clipFileName);

                    string mergeArguments = $"-f concat -safe 0 -i \"{playlistFileName}\" -c copy \"{clipFileName}\"";

                    _wrapper.Execute(mergeArguments);

                    if (_canceled)
                        return;
                }

                entry.Update("Generating GIF (3/4): Extracting Palette", 2 / 4.0);

                string paletteFileName =
                    Path.Combine(Path.GetTempPath(), Path.GetFileName(settings.VideoFile) + "-palette.png");
                string paletteArguments = $"-stats -y -i \"{clipFileName}\" -vf palettegen \"{paletteFileName}\"";
                tempFiles.Add(paletteFileName);

                _wrapper.Execute(paletteArguments);

                if (_canceled)
                    return;

                entry.Update("Generating GIF (3/4): Creating GIF", 3 / 4.0);

                string gifFileName = settings.OutputFile;
                string gifArguments =
                    $"-stats -y -r {settings.Framerate} -i \"{clipFileName}\" -i \"{paletteFileName}\" -filter_complex paletteuse -plays 0 \"{gifFileName}\"";

                _wrapper.Execute(gifArguments);

                if (_canceled)
                    return;

                entry.Update("Done!", 4 / 4.0);
                bool success = File.Exists(gifFileName);

                if(success)
                    entry.DoneType = JobDoneTypes.Success;
                else
                    entry.DoneType = JobDoneTypes.Failure;

                OnDone(new Tuple<bool, string>(success, gifFileName));
            }
            catch
            {
                entry.DoneType = JobDoneTypes.Failure;
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
            _wrapper?.Input("q");

            if (_thread != null)
            {
                if (_thread.Join(TimeSpan.FromSeconds(5)))
                    _thread.Abort();
                _thread = null;
            }
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