using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyDB
{
    public interface IObserver
    {
        public void DoWork(string message);
    }
    class ConcreteObserver : IObserver
    {
        public void DoWork(string message)
        {
          //do some work
        }
    }

    public class Subject
    {
        object lockObject = new object();
        List<IObserver> listObservers = new List<IObserver>();
        public void Attach(IObserver observer) {
            lock (lockObject)
            {
                listObservers.Add(observer);
            }
        }
        public void Detach(IObserver observer)
        {
            lock (lockObject)
            {
                listObservers.Remove(observer);
            }
        }

        public void Broadcast(string message)
        {
            List<IObserver> snapshot;
            lock (lockObject) //avoid holding the lock during broadcasting.
            {
                snapshot = new List<IObserver>(listObservers);
            }

            foreach (IObserver observer in snapshot) {
                observer.DoWork(message);
            }
        }
    }

    public class Subject1
    {
        
        ConcurrentDictionary<IObserver, bool> listObservers = new ConcurrentDictionary<IObserver, bool>();
        public void Attach(IObserver observer)
        {
            listObservers.TryAdd(observer, true);
        }
        public void Detach(IObserver observer)
        {
            bool v = listObservers.TryRemove(observer, out _);
        }

        public void Broadcast(string message)
        {    

            foreach (IObserver observer in listObservers.Keys)
            {
                observer.DoWork(message);
            }
        }
    }
}
