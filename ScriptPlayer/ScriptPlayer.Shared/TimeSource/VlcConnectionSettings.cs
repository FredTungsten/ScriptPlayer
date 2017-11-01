namespace ScriptPlayer.Shared
{
    public class VlcConnectionSettings
    {
        public string Password { get; set; }
        public string IpAndPort { get; set; }

        public const string DefaultEndpoint = "127.0.0.1:8080";

        public VlcConnectionSettings()
        {
            IpAndPort = DefaultEndpoint;
        }
    }
}