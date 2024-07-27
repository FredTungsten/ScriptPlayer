namespace ScriptPlayer.Shared.Beats
{
    public class Bar
    {
        public Tact Tact { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public bool[] Rythm { get; set; }
    }
}