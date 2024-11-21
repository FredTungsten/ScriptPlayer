namespace ScriptPlayer.HandyApi.Messages
{
    public class AuthTokenIssueRequest
    {
        [GetParameter("ck")]
        public string ConnectionKey { get; set; }

        [GetParameter("ip")]
        public string ClientIp { get; set; }

        [GetParameter("origin")]
        public string ClientOrigin { get; set; }

        [GetParameter("ttl")]
        public int TimeToLive { get; set; }
    }
}
