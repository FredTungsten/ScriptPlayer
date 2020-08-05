using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ScriptPlayer.Shared.TheHandy
{
    public class BlockingTaskQueue
    {
        private readonly BlockingCollection<Task> _jobs = new BlockingCollection<Task>();

        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private readonly Thread _worker;

        public BlockingTaskQueue()
        {
            _worker = new Thread(OnStart);
            _worker.IsBackground = true;
            _worker.Start();
        }

        public void Enqueue(Task job)
        {
            _jobs.Add(job);
        }

        private void OnStart()
        {
            foreach (var job in _jobs.GetConsumingEnumerable(_tokenSource.Token))
            {
                job.RunSynchronously();
            }
        }

        public void Cancel()
        {
            _tokenSource.Cancel();
            _worker.Abort();
        }
    }
}