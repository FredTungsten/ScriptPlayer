using System;

namespace ScriptPlayer.Shared.TheHandy
{
    internal class Handy2Response
    {
        public int result { get; set; }
        public Handy2Error error { get; set; }
    }

    internal class Handy2Error
    {
        public int code { get; set; }
        public string name { get; set; }
        public string message { get; set; }
        public bool connected { get; set; }
    }

    internal class Handy2ModeUpdateRequest
    {
        public int mode { get; set; }
    }

    internal class Handy2ModeUpdateResponse : Handy2Response
    {
        private int state { get; set; }
    }
    
    internal enum Handy2ModeUpdateResult
    {
        Error = -1,
        SuccessNewMode = 0,
        SuccessSameMode = 1
    }

    internal enum Handy2Mode
    {
        Hamp = 0,
        Hssp = 1,
        Hdsp = 2,
        Maintenance =3
    }

    internal enum Handy2HsspSetupResult
    {
        UsingCached = 0,
        Downloaded = 1,
        DownloadError = -1,
        SyncRequired = -20,
    }

    internal class Handy2HsspPlayRequest
    {
        public long estimatedServerTime { get; set; }
        public long startTime { get; set; }
    }

    internal enum Handy2HsspPlayResult
    {
        Success = 0,
        Failure = -1,
    }

    internal enum Handy2SyncResult
    {
        Success = 0,
        Error = 1
    }

    internal class Handy2SyncResponse : Handy2Response
    {
        private long dtserver { get; set; }
    }

    internal class Handy2HsspSetup
    {
        public string url { get; set;}
        //public string sha256 { get; set; }
    }

    internal class Handy2ConnectedResult
    {
        public bool connected { get; set; }
    }

    internal class Handy2ServerTimeResponse : Handy2Response
    {
        public long serverTime { get; set; }
    }

    internal class Handy2SetSlideRequest
    {
        public double min { get; set; }
        public double max { get; set; }
    }

    internal class Handy2SetOffsetRequest
    {
        public int offset { get; set; }
    }
}
