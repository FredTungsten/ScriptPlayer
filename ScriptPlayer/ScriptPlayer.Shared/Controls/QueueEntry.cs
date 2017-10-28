using System;

namespace ScriptPlayer.Shared
{
    public class QueueEntry
    {
        public QueueEntry()
        {
            Submitted = DateTime.Now;
        }

        public DateTime Submitted { get; set; }
    }

    public class QueueEntry<T> : QueueEntry
    {
        public T Values { get; }

        public QueueEntry(T values)
        {
            Values = values;
        }
    }
}