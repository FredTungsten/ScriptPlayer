using System;
using System.Diagnostics;
using HtmlAgilityPack;

namespace ScriptPlayer.Shared
{
    public class MpcStatus
    {
        public bool IsValid { get; set; }

        public string FilePath { get; set; }

        public string File { get; set; }

        public string FileDir { get; set; }

        public MpcPlaybackState State { get; set; }

        public long Position { get; set; }

        public long Duration { get; set; }

        public int VolumeLevel { get; set; }

        public MpcStatus()
        {
            IsValid = false;
        }

        public MpcStatus(string statusHtml)
        {
            try
            {
                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(statusHtml);

                File = document.GetElementbyId("file").InnerText;
                FilePath = document.GetElementbyId("filepath").InnerText;
                FileDir = document.GetElementbyId("filedir").InnerText;
                State = (MpcPlaybackState)int.Parse(document.GetElementbyId("State").InnerText);
                Position = long.Parse(document.GetElementbyId("position").InnerText);
                Duration = long.Parse(document.GetElementbyId("duration").InnerText);
                VolumeLevel = int.Parse(document.GetElementbyId("volumelevel").InnerText);
                IsValid = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Couldn't load MPC-HC status page! " + e.Message);
                IsValid = false;
            }
        }
    }
}