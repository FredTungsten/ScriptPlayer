using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScriptPlayer.Shared.Scripts
{
    public static class ScriptLoaderManager
    {
        private static readonly HashSet<Type> KnownLoaders = new HashSet<Type>();
        private static readonly List<ScriptLoader> Loaders = new List<ScriptLoader>();
        private static readonly Dictionary<ScriptFileFormat, ScriptLoader> LoaderByFileFormat = new Dictionary<ScriptFileFormat, ScriptLoader>();

        static ScriptLoaderManager()
        {
            //RegisterLoader<VorzeScriptLoader>();
            //RegisterLoader<RawScriptLoader>();

            RegisterLoader<FunScriptLoader>();
            RegisterLoader<BeatFileLoader>();
            RegisterLoader<VirtualRealPornScriptLoader>();
            RegisterLoader<VorzeScriptToFunscriptLoader>();
            RegisterLoader<FeelVrScriptLoader>();
            RegisterLoader<WankzVrScriptLoader>();
            RegisterLoader<RealTouchScriptLoader>();

            RegisterLoader<FeelMeBruteForceLoader>();
            RegisterLoader<FeelMeBruteForceJsonLoader>();
            RegisterLoader<FeelMeLikeBruteForceJsonLoader>();
            RegisterLoader<FeelMeRegexLoader>();
        }

        public static void RegisterLoader<T>() where T : ScriptLoader, new()
        {
            if (KnownLoaders.Contains(typeof(T)))
                return;

            ScriptLoader loader = new T();
            Loaders.Add(loader);
            KnownLoaders.Add(typeof(T));

            List<ScriptFileFormat> formats = loader.GetSupportedFormats();

            foreach (ScriptFileFormat format in formats)
            {
                LoaderByFileFormat.Add(format, loader);
            }
        }

        public static ScriptFileFormatCollection GetFormats()
        {
            return new ScriptFileFormatCollection(LoaderByFileFormat.Keys);
        }

        public static ScriptLoader[] GetLoaders(ScriptFileFormat[] formats)
        {
            return formats.Select(format => LoaderByFileFormat[format])
                          .OrderBy(loader => Loaders.IndexOf(loader))
                          .ToArray();
        }

        public static string[] GetSupportedExtensions()
        {
            return Loaders.SelectMany(l => l.GetSupportedFormats().SelectMany(f => f.Extensions)).Distinct().ToArray();
        }

        public static ScriptLoader[] GetLoaders(string filename)
        {
            string extension = (Path.GetExtension(filename) ?? "").TrimStart('.').ToLower();
            return Loaders.Where(loader => loader.GetSupportedFormats().Any(f => f.Extensions.Contains(extension)))
                .OrderBy(loader => Loaders.IndexOf(loader))
                .ToArray();
        }

        public static ScriptLoader[] GetAllLoaders()
        {
            return Loaders.ToArray();
        }
    }

    public class BeatFileLoader : ScriptLoader
    {
        public override List<ScriptAction> Load(Stream stream)
        {
            BeatCollection beats = BeatCollection.Load(stream);

            return beats.Select(beat => new BeatScriptAction
            {
                TimeStamp = beat
            }).Cast<ScriptAction>().ToList();
        }

        public override List<ScriptFileFormat> GetSupportedFormats()
        {
            return new List<ScriptFileFormat>
            {
                new ScriptFileFormat("Beat File", "txt", "beats")
            };
        }
    }

    public class BeatScriptAction : ScriptAction
    {
        public override bool IsSameAction(ScriptAction action)
        {
            return false;
        }
    }
}
