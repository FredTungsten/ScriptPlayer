using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ScriptPlayer.Shared.Scripts
{
    public class RepeatablePattern
    {
        public class PatternPosition
        {
            public byte Position { get; set; }
            public int Duration { get; set; }

            public PatternPosition()
            { }

            public PatternPosition(byte position, int duration = 1)
            {
                Position = position;
                Duration = duration;
            }
        }

        public string Name { get; set; }

        private readonly List<PatternPosition> _positions = new List<PatternPosition>();

        public int Count => _positions.Count;

        public PatternPosition this[int index]
        {
            get => _positions[index % _positions.Count];
        }

        public RepeatablePattern(params int[] positions)
        {
            foreach (int position in positions)
            {
                if (position < 0)
                {
                    if (_positions.Count > 0)
                        _positions.Last().Duration++;
                }
                else
                    _positions.Add(new PatternPosition((byte)position));
            }
        }
    }
}
