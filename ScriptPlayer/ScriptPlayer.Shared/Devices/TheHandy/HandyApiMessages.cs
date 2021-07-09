using JetBrains.Annotations;

// ReSharper disable InconsistentNaming

namespace ScriptPlayer.Shared.TheHandy
{
    [UsedImplicitly]
    internal class HandyResponse
    {
        public bool success { get; set; }
        public bool connected { get; set; }
        public string cmd { get; set; }
        public string error { get; set; }
        public int setOffset { get; set; }
        public int adjustment { get; set; }
        public string version { get; set; }
        public string latest { get; set; }
        public int mode { get; set; }
        public float position { get; set; }
        public float stroke { get; set; }
        public float strokePercent { get; set; }
        public float speed { get; set; }
    }

    [UsedImplicitly]
    internal class HandyUploadResponse
    {
        public bool success { get; set; }
        public bool? converted { get; set; }
        public string filename { get; set; }
        public string info { get; set; }
        public string orginalfile { get; set; }
        public int size { get; set; }
        public string url { get; set; }
        public string error { get; set; }
    }

    [UsedImplicitly]
    internal class HandyPlay
    {
        // required
        public bool play { get; set; }
        // optional
        public long? serverTime { get; set; }
        public int? time { get; set; }
        public int? timeout { get; set; }
    }

    [UsedImplicitly]
    internal class HandyPrepare
    {
        // required
        public string url { get; set; } // url to funscript converted to csv can be local ip
        // optional
        public string name { get; set; } // name of scipt
        public int? size { get; set; } // max size 1MB in bytes
        public int? timeout { get; set; }
    }

    [UsedImplicitly]
    internal class HandySetStroke
    {
        public int value { get; set; }
        public string type { get; set; }
        public int? timeout { get; set; }
    }

    [UsedImplicitly]
    internal class HandySetStrokeZone
    {
        public int min { get; set; }
        public int max { get; set; }
        public int? timeout { get; set; }
    }

    [UsedImplicitly]
    internal class HandyOffset
    {
        // required
        public int offset { get; set; }
        // optional
        public int? timeout { get; set; }
    }

    [UsedImplicitly]
    internal class HandyAdjust
    {
        // required
        public int currentTime { get; set; }
        public long serverTime { get; set; }
        // optional
        public float? filter { get; set; }
        public int? timeout { get; set; }
    }

    [UsedImplicitly]
    internal class HandyTimeResponse
    {
        public long serverTime { get; set; }
    }
}
