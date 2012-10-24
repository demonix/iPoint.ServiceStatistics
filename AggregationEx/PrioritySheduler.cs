using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AggregationEx
{
    public class PrioritySheduler: TaskScheduler
    {
        public static PrioritySheduler AboveNormal = new PrioritySheduler(ThreadPriority.AboveNormal);
        public static PrioritySheduler BelowNormal = new PrioritySheduler(ThreadPriority.BelowNormal);
        private BlockingCollection<Task> _tasks = new BlockingCollection<Task>();
        private Thread[] _threads;
        private ThreadPriority _priority;
        private int _maxConcurencyLevel = Math.Max(1, Environment.ProcessorCount);


        private PrioritySheduler(ThreadPriority priority)
        {
            _priority = priority;
            _threads = new Thread[_maxConcurencyLevel];
                for (int i = 0; i < _threads.Length; i++)
                {
                    _threads[i] = new Thread(() =>
                                                 {
                                                     foreach (var t in _tasks.GetConsumingEnumerable())
                                                     {
                                                         base.TryExecuteTask(t);
                                                     }
                                                 });
                    _threads[i].Name = "PrioritySheduler #" + i;
                    _threads[i].Priority = _priority;
                    _threads[i].IsBackground = true;
                    _threads[i].Start();
                }
        }

        public override int MaximumConcurrencyLevel
        {
            get { return _maxConcurencyLevel; }
        }

        protected override void QueueTask(Task task)
        {
            _tasks.Add(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks;
        }
    }
}