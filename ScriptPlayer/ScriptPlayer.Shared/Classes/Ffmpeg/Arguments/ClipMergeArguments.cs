using System.IO;
using System.Linq;

namespace ScriptPlayer.Shared
{
    public class ClipMergeArguments : FfmpegArguments
    {
        public string[] ClipFiles { get; set; }

        public string OutputFile { get; set; }

        public override string BuildArguments()
        {
            string playlistFileName = Path.Combine(Path.GetTempPath(), Path.GetFileName(InputFile) + $"-playlist.txt");
            File.WriteAllLines(playlistFileName, ClipFiles.Select(se => $"file '{se.Replace("'", "'\\''")}'")); //concat requires special escaping
            TempFiles.Add(playlistFileName);
            return $"-f concat -safe 0 -i \"{playlistFileName}\" -c copy \"{OutputFile}\"";
        }
    }
}