using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ScriptPlayer.Shared.Scripts;

namespace ScriptPlayer.LoaderTester
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] files = Directory.GetFiles("D:\\Videos\\CH\\~Haptic Files", "*.*", SearchOption.AllDirectories);
            bool[] everloaded = new bool[files.Length];

            List<ScriptLoader> loaders =
                new List<ScriptLoader>
                {
                    new FeelMeBruteForceJsonLoader(),
                    new FeelMeBruteForceLoader(),
                    new FeelMeRegexLoader(),
                    new FeelVrScriptLoader(),
                    new WankzVrScriptLoader(),
                    new VirtualRealPornScriptLoader()
                };


            foreach (ScriptLoader loader in loaders)
            {
                DateTime start = DateTime.Now;

                int success = 0;

                for(int i = 0; i < files.Length; i++)
                {
                    string filename = files[i];

                    try
                    {
                        var result = loader.Load(filename);
                        if (result == null) continue;
                        if (result.Count == 0) continue;

                        everloaded[i] = true;
                        success++;
                    }
                    catch (Exception)
                    {
                    }
                }

                DateTime end = DateTime.Now;

                Console.WriteLine("{0} successfully loaded {1}/{2} ({3:P1}) scripts in {4:f1}s", loader.GetType().Name, success, files.Length, success/ (double)files.Length, (end-start).TotalSeconds);
            }

            Console.WriteLine("The following files {0} were never loaded successfully:", everloaded.Count(b => !b));

            for (int i = 0; i < files.Length; i++)
            {
                if (everloaded[i]) continue;
                string filename = files[i];
                Console.WriteLine(filename);
            }

            Console.ReadLine();
        }
    }
}
