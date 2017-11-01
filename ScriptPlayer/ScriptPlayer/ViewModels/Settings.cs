using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Scripts;

namespace ScriptPlayer.ViewModels
{
    public class Settings
    {
        public List<string> AdditionalPaths { get; set; }
        public VlcConnectionSettings Vlc { get; set; }
        public WhirligigConnectionSettings Whirligig { get; set; }
        public ButtplugConnectionSettings Buttplug { get; set; }
        public Byte MinPosition { get; set; }
        public Byte MaxPosition { get; set; }
        public byte MinSpeed { get; set; }
        public byte MaxSpeed { get; set; }
        public double ScriptDelay { get; set; }
        public double SpeedMultiplier { get; set; }
        public double CommandDelay { get; set; }
        public bool AutoSkip { get; set; }
        public ConversionMode ConversionMode { get; set; }
        public bool ShowHeatMap { get; set; }
        public bool LogMarkers { get; set; }
        public PositionFilterMode FilterMode { get; set; }
        public double FilterRange { get; set; }
        public bool ShowScriptPositions { get; set; }
        public bool DisplayEventNotifications { get; set; }

        public Settings()
        {
            DisplayEventNotifications = true;
            AdditionalPaths = new List<string>();
        }

        public static Settings FromFile(string filename)
        {
            try
            {
                using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                    return serializer.Deserialize(stream) as Settings;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return null;
            }
        }

        public void Save(string filename)
        {
            try
            {
                string dir = Path.GetDirectoryName(filename);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using (FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                    serializer.Serialize(stream, this);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
    }
}