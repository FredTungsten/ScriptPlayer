namespace ScriptPlayer.Shared.Scripts
{
    public class ScriptFileFormat
    {
        public string Name { get; set; }
        public string[] Extensions { get; set; }

        public ScriptFileFormat()
        { }

        public ScriptFileFormat(string name, params string[] extensions)
        {
            Name = name;
            Extensions = extensions;
        }
    }
}