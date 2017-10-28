using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;

namespace ScriptPlayer.Shared
{
    public class FrameCaptureCollection : ICollection<FrameCapture>
    {
        private readonly List<FrameCapture> _list;

        public FrameCaptureCollection()
        {
            _list = new List<FrameCapture>();
        }

        public FrameCaptureCollection(IEnumerable<FrameCapture> captures)
        {
            _list = new List<FrameCapture>(captures);
        }

        public FrameCapture this[int index] => _list[index];

        public IEnumerator<FrameCapture> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(FrameCapture item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(FrameCapture item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(FrameCapture[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public bool Remove(FrameCapture item)
        {
            return _list.Remove(item);
        }

        public int Count => _list.Count;
        public bool IsReadOnly => false;
        public string VideoFile { get; set; }
        public Int32Rect CaptureRect { get; set; }

        public Int32 TotalFramesInVideo { get; set; }
        
        public Int64 DurationNumerator { get; set; }

        public Int64 DurationDenominator { get; set; }

        public void SaveToFile(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                SaveToStream(stream);
        }

        public static FrameCaptureCollection FromFile(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                return FromStream(stream);
        }

        public static FrameCaptureCollection FromStream(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                FrameCaptureCollection result = new FrameCaptureCollection();

                byte[] header = reader.ReadBytes(3); //70,83,70
                Debug.Assert(header[0] == 70);
                Debug.Assert(header[1] == 83);
                Debug.Assert(header[2] == 70);

                byte[] version = reader.ReadBytes(2);
                Debug.Assert(version[0] == 1);
                Debug.Assert(version[1] == 0);

                byte endianess = reader.ReadByte();

                int payloadCount = reader.ReadInt32();

                while (payloadCount-- > 0)
                {
                    int payloadType = reader.ReadInt32();
                    int payloadSize = reader.ReadInt32();

                    switch (payloadType)
                    {
                        case 1:
                        {
                            byte[] fileNameBytes = reader.ReadBytes(payloadSize);
                            result.VideoFile = Encoding.UTF8.GetString(fileNameBytes);
                            break;
                        }
                        case 2:
                        {
                            Int32Rect captureRect = new Int32Rect();
                            captureRect.X = reader.ReadInt32();
                            captureRect.Y = reader.ReadInt32();
                            captureRect.Width = reader.ReadInt32();
                            captureRect.Height = reader.ReadInt32();
                            result.CaptureRect = captureRect;
                            break;
                        }
                        case 3:
                        {
                            int frames = reader.ReadInt32();
                            int pixelSize = reader.ReadInt32();
                            int expectedPixelSize = result.CaptureRect.Width * result.CaptureRect.Height;
                            int expectedPixelSize2 = ((payloadSize - sizeof(int) - sizeof(int)) / frames) - sizeof(long);

                            while (frames-- > 0)
                            {
                                FrameCapture capture = new FrameCapture();
                                capture.FrameIndex = reader.ReadInt64();
                                capture.Capture = reader.ReadBytes(pixelSize);
                                
                                result.Add(capture);
                            }
                            break;
                        }
                        case 4:
                        {
                            result.TotalFramesInVideo = reader.ReadInt32();
                            break;
                        }
                        case 5:
                        {
                            result.DurationNumerator = reader.ReadInt64();
                            result.DurationDenominator = reader.ReadInt64();
                                break;
                        }
                    }
                }

                if (result.TotalFramesInVideo == 0)
                    result.TotalFramesInVideo = result.Count;

                return result;
            }
        }

        public void SaveToStream(FileStream stream)
        {
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                //Header "FSF" Frame Sample File
                writer.Write(new byte[]{70,83,70});
                //Version 1.0
                writer.Write(new byte[]{1,0});
                //Endianess
                writer.Write((byte)(BitConverter.IsLittleEndian?0:1));

                writer.Write((int)5); //Number of Payloads = 5

                //Payload type 1 = VideoFile
                writer.Write((int)1);
                byte[] fileName = Encoding.UTF8.GetBytes(VideoFile);
                writer.Write((int)fileName.Length);
                writer.Write(fileName);

                //Payload type 2 = CaptureRect
                writer.Write((int)2);
                writer.Write((int)(4 * sizeof(Int32))); // Length = 4 x Int32 = 16
                writer.Write((Int32)CaptureRect.X);
                writer.Write((Int32)CaptureRect.Y);
                writer.Write((Int32)CaptureRect.Width);
                writer.Write((Int32)CaptureRect.Height);

                //Payload type 3 = Samples
                writer.Write((int)3);
                int numberOfFrames = (int)Count;
                int pixelsLength = CaptureRect.Width * CaptureRect.Height * 3;
                int framelength = (int)(pixelsLength + sizeof(long));
                int totalLength = numberOfFrames * framelength + sizeof(int) + sizeof(int);
                writer.Write(totalLength);

                writer.Write(numberOfFrames);
                writer.Write(pixelsLength);

                foreach (FrameCapture capture in _list)
                {
                    writer.Write(capture.FrameIndex);
                    writer.Write(capture.Capture);
                }

                //Payload type 4 = TotalVideoFrames
                writer.Write((int)4);
                writer.Write(sizeof(int));
                writer.Write(TotalFramesInVideo);

                //Payload type 5 = Duration;
                writer.Write((int)5);
                writer.Write((int)(2*sizeof(Int64)));
                writer.Write(DurationNumerator);
                writer.Write(DurationDenominator);
            }
        }

        public TimeSpan FrameIndexToTimeSpan(long frameIndex)
        {
            // Relative Progress / Total Duration
            // (frameIndex / TotalFramesInVideo) * (DurationNumerator / DurationDenominator)
            long actualPosition = (long) ((frameIndex * TimeSpan.TicksPerSecond / (double) TotalFramesInVideo) * (DurationNumerator / (double) DurationDenominator));
            TimeSpan roundedTimeSpan = TimeSpan.FromTicks(actualPosition);

            return roundedTimeSpan;

            //Even Int64 isn't enought ....
            /*long timeSpanTicksMs = (frameIndex * DurationNumerator * (TimeSpan.TicksPerSecond / TimeSpan.TicksPerMillisecond) ) / (TotalFramesInVideo * DurationDenominator);
            TimeSpan timestamp = TimeSpan.FromTicks(timeSpanTicksMs * TimeSpan.TicksPerMillisecond);

            if (Math.Abs((timestamp - roundedTimeSpan).TotalSeconds) > 0.1)
            {
                Debug.Write("oO!");
            }

            return timestamp;*/
        }
    }
}