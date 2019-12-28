using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Xml.Serialization;

namespace ScriptPlayer.ViewModels
{
    public class WindowStateModel
    {
        public bool IsMaximized { get; set; }

        public bool IsFullscreen { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public double SettingsWidth { get; set; }

        public double PlaylistWidth { get; set; }

        public bool HideSettings { get; set; }

        public bool HidePlaylist { get; set; }

        public WindowStateModel()
        {
            SettingsWidth = 200;
            PlaylistWidth = 200;
        }

        [XmlIgnore]
        public Rect WindowPosition
        {
            get => GetPosition();
            set => SetPosition(value);
        }

        public void SetPosition(Rect value)
        {
            X = value.X;
            Y = value.Y;
            Width = value.Width;
            Height = value.Height;
        }

        public Rect GetPosition()
        {
            return new Rect(X, Y, Width, Height);
        }
    }

    public class PlayerStateModel
    {
        public double? Volume { get; set; }
        public PlaybackMode? PlaybackMode { get; set; }

        public WindowStateModel WindowState { get; set; }

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
                if (!File.Exists(filename))
                    return null;

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
