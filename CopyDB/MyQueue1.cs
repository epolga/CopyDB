using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyDB
{
    public class MyQueue1
    {
        ConcurrentQueue<string> _taskQueue = new ConcurrentQueue<string>();
        CancellationTokenSource _cts = new CancellationTokenSource();
        int _count = 0;
        
        public void Enqueue(string item)
        {
            _taskQueue.Enqueue(item);
        }

        public string Dequeue()
        {
            string result = string.Empty;
            if (_taskQueue.TryDequeue(out result))
            {
                return result;
            }
            return string.Empty;
        }

        public void Start()
        {
            foreach (var item in _taskQueue) 
            {
                Task.Run(() => { ProcessTask(_cts.Token); });
                Thread.Sleep(10);
            }
        }
        public void ProcessTask(CancellationToken token)
        {
            if (!_cts.IsCancellationRequested)
            {
                _taskQueue.TryDequeue(out _);
                Thread.Sleep(1000);
            }
        }
    }
}
