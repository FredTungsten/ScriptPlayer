using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;

namespace ScriptPlayer.Shared.Classes.Wrappers
{
    public class FfmpegWrapper
    {
        private bool _canceled;

        private FfmpegConsoleWrapper _activeWrapper;

        protected string FfmpegExePath { get; }

        public FfmpegWrapper(string ffmpegExePath)
        {
            FfmpegExePath = ffmpegExePath;
        }

        public static string CreateRandomTempDirectory()
        {
            string outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("D"));
            if (!outputDirectory.EndsWith("\\"))
                outputDirectory += "\\";

            Directory.CreateDirectory(outputDirectory);
            return outputDirectory;
        }

        public string[] GetSubtitles(string videoFile, SubtitleStream stream)
        {
            string tempFile = Path.GetTempFileName();
            string tempfileWithextension = tempFile + ".tmp";

            try
            {
                FfmpegArguments arguments = new ExtractSubtitleArguments()
                {
                    StreamIndex = stream.StreamId,
                    OutputFile = tempfileWithextension,
                    Format = stream.Format,
                    InputFile = videoFile
                };

                if (!Execute(arguments))
                    return null;

                return File.ReadAllLines(tempfileWithextension);
            }
            finally
            {
                File.Delete(tempFile);
                File.Delete(tempfileWithextension);
            }
        }

        public VideoInfo GetVideoInfo(string videoFile)
        {
            VideoInfoArguments arguments = new VideoInfoArguments
            {
                InputFile = videoFile
            };

            VideoInfoWrapper infoWrapper = new VideoInfoWrapper(arguments, FfmpegExePath)
            {
                DebugOutput = false,
            };

            DateTime start = DateTime.Now;

            ExecuteWrapper(infoWrapper);

            TimeSpan duration = DateTime.Now - start;

            Debug.WriteLine($"GetVideoInfo took {duration.TotalMilliseconds:f2} ms");

            return infoWrapper.Result;
        }

        private int ExecuteWrapper(FfmpegConsoleWrapper wrapper)
        {
            if (_canceled)
                return -1;

            _activeWrapper = wrapper;
            int result = wrapper.Execute();
            Debug.WriteLine(wrapper.GetType().Name + " executed with ExitCode " + result);
            _activeWrapper = null;
            return result;
        }

        public bool Execute(FfmpegArguments arguments)
        {
            FfmpegConsoleWrapper wrapper = new FfmpegConsoleWrapper(arguments, FfmpegExePath);
            return ExecuteWrapper(wrapper) == 0;
        }

        public void Cancel()
        {
            if (_canceled)
                return;

            _canceled = true;

            try
            {
                _activeWrapper?.Cancel();
            }
            catch
            {

            }
        }

        public List<Tuple<TimeSpan, BitmapSource>> ExtractFrames(FrameConverterArguments arguments)
        {
            bool isTempDir = false;

            if (string.IsNullOrWhiteSpace(arguments.OutputDirectory))
            {
                isTempDir = true;
                arguments.OutputDirectory = CreateRandomTempDirectory();
            }

            FrameConverterWrapper wrapper = new FrameConverterWrapper(arguments, FfmpegExePath);

            ExecuteWrapper(wrapper);

            List<Tuple<TimeSpan, BitmapSource>> result = new List<Tuple<TimeSpan, BitmapSource>>();

            List<string> usedFiles = new List<string>();

            foreach (string file in Directory.EnumerateFiles(arguments.OutputDirectory))
            {
                string number = Path.GetFileNameWithoutExtension(file);
                int index = int.Parse(number);

                TimeSpan position = TimeSpan.FromSeconds((index - 0.5) * arguments.Intervall);

                var frame = new BitmapImage();
                frame.BeginInit();
                frame.CacheOption = BitmapCacheOption.OnLoad;
                frame.UriSource = new Uri(file, UriKind.Absolute);
                frame.EndInit();

                result.Add(new Tuple<TimeSpan, BitmapSource>(position, frame));
                usedFiles.Add(file);
            }
            
            foreach (string tempFile in usedFiles)
                File.Delete(tempFile);

            if(isTempDir)
                Directory.Delete(arguments.OutputDirectory);

            return result;  
        }
    }
}
