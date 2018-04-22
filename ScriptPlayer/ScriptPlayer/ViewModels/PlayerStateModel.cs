using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace ScriptPlayer.ViewModels
{
    public class PlayerStateModel
    {
        public double? Volume { get; set; }
        public PlaybackMode? PlaybackMode { get; set; }

        public void Save(string filename)
        {
            try
            {
                string dir = Path.GetDirectoryName(filename);
                if (string.IsNullOrWhiteSpace(dir))
                    throw new ArgumentException(@"Directory is null", nameof(filename));

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using (FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(PlayerStateModel));
                    serializer.Serialize(stream, this);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public static PlayerStateModel FromFile(string filename)
        {
            try
            {
                using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(PlayerStateModel));
                    return serializer.Deserialize(stream) as PlayerStateModel;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return null;
            }
        }
    }
}
