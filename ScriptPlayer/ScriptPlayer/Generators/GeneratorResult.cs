namespace ScriptPlayer.Generators
{
    public class GeneratorResult
    {
        public string GeneratedFile { get; set; }

        public bool Success { get; set; }

        public static GeneratorResult Failed()
        {
            return new GeneratorResult();
        }

        public static GeneratorResult Succeeded(string file)
        {
            return new GeneratorResult
            {
                GeneratedFile = file,
                Success = true
            };
        }
    }
}