namespace ScriptPlayer.ViewModels
{
    public class PlaylistEntry
    {
        public PlaylistEntry(string filename)
        {
            Fullname = filename;
            Shortname = System.IO.Path.GetFileNameWithoutExtension(filename);
        }

        public string Shortname { get; set; }
        public string Fullname { get; set; }
    }
}