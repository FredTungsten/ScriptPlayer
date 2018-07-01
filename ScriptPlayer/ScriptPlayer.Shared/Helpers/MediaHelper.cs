using System;
using System.Diagnostics;
using MediaInfo;

namespace ScriptPlayer.Shared.Helpers
{
    public static class MediaHelper
    {
        public static TimeSpan? GetDuration(string mediaFileName)
        {
            if (string.IsNullOrWhiteSpace(mediaFileName))
                return null;

            try
            {
                MediaInfoWrapper wrapper = new MediaInfoWrapper(mediaFileName);
                return TimeSpan.FromMilliseconds(wrapper.Duration);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception in {nameof(MediaHelper)}.{nameof(GetDuration)}: {e.Message}");
                return null;
            }
        }
    }
}
