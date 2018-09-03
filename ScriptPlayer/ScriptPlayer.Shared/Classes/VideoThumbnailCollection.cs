using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ScriptPlayer.Shared.Classes
{
    public class VideoThumbnailCollection
    {
        private readonly List<VideoThumbnail> _thumbnails = new List<VideoThumbnail>();

        public void Add(TimeSpan timestamp, BitmapSource thumbnail)
        {
            Add(new VideoThumbnail
            {
                Thumbnail = thumbnail,
                Timestamp = timestamp
            });
        }

        public void Add(VideoThumbnail videoThumbnail)
        {
            for (int i = 0; i < _thumbnails.Count; i++)
            {
                if (_thumbnails[i].Timestamp > videoThumbnail.Timestamp)
                {
                    _thumbnails.Insert(i, videoThumbnail);
                    return;
                }
            }

            _thumbnails.Add(videoThumbnail);
        }

        public VideoThumbnail Get(TimeSpan timestamp)
        {
            if (_thumbnails.Count == 0)
                return null;

            for (int index = _thumbnails.Count - 1; index >= 0; index--)
            {
                VideoThumbnail thumbnail = _thumbnails[index];
                if (thumbnail.Timestamp <= timestamp)
                    return thumbnail;
            }

            return _thumbnails.First();
        }

        public void Save(Stream stream)
        {
            JObject jRoot = new JObject();
            JArray jThumbs = new JArray();
            jRoot.Add("thumbnails", jThumbs);

            foreach (VideoThumbnail thumbnail in _thumbnails)
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(thumbnail.Thumbnail));
                string base64Image;
                using (MemoryStream m = new MemoryStream())
                {
                    encoder.Save(m);
                    base64Image = Convert.ToBase64String(m.ToArray());
                }

                jThumbs.Add(new JObject(new JProperty("time", thumbnail.Timestamp), new JProperty("image", base64Image)));
            }

            using (StreamWriter writer = new StreamWriter(stream))
            {
                jRoot.WriteTo(new JsonTextWriter(writer));
            }
        }

        public void Load(Stream stream)
        {
            _thumbnails.Clear();

            JsonSerializer serializer = new JsonSerializer();
            JObject jRoot;

            using (StreamReader reader = new StreamReader(stream))
                jRoot = JObject.Load(new JsonTextReader(reader));

            JArray jThumbs = jRoot["thumbnails"] as JArray;

            foreach (JObject thumb in jThumbs)
            {
                byte[] image = Convert.FromBase64String(thumb["image"].Value<string>());
                JpegBitmapDecoder decoder = new JpegBitmapDecoder(new MemoryStream(image), BitmapCreateOptions.None, BitmapCacheOption.None);
                TimeSpan timestamp = TimeSpan.Parse(thumb["time"].Value<string>());

                VideoThumbnail thumbnail = new VideoThumbnail
                {
                    Thumbnail = decoder.Frames[0],
                    Timestamp = timestamp
                };

                Add(thumbnail);
            }
        }
    }

    public class VideoThumbnail
    {
        public TimeSpan Timestamp { get; set; }
        public BitmapSource Thumbnail { get; set; }
    }
}
