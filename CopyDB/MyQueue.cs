using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyDB
{
    class MyTasksQueue
    {
        private ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        int _nTasks = 0;
        public MyTasksQueue(int nTasks) 
        {
            _nTasks = nTasks;
        }

        public void Enqueue(string taskName)
            { _queue.Enqueue(taskName); }

        public void Start()
        {
            for (int i = 0; i < _nTasks; i++) {
                Task.Run(() =>
                {
                    ProcessTask(_cancellationTokenSource.Token);
                });
            }
        }
        public void Stop()
        {
            _cancellationTokenSource.Cancel();  
        }
        public void ProcessTask(CancellationToken cancellationToken) { 
            while(!cancellationToken.IsCancellationRequested)
            {
                if(_queue.TryDequeue(out string taskName))
                {
                    Console.WriteLine(taskName);
                   //Do Work
                }
            }
        }
        
    }
}
