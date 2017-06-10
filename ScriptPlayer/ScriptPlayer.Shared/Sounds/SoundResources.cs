using System;
using System.Reflection;

namespace ScriptPlayer.Shared.Sounds
{
    public static class SoundResources
    {
        private static readonly string AssemblyName;

        static SoundResources()
        {
            AssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        }
        public static Uri GetResourceUri(string filename)
        {
            string uri = $"pack://application:,,,/{AssemblyName};component/Sounds/{filename}";
            return new Uri(uri, UriKind.Absolute);
        }
    }
}
