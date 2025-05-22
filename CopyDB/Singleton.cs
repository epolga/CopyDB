using CopyDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyDB
{
    sealed public class Singleton1
    {
         // Static Lazy<T> ensures thread-safe lazy initialization
        private static readonly Lazy<Singleton1> instance =
            new Lazy<Singleton1>(() => new Singleton1());
        private Singleton1() { } // private constructor
        static public Singleton1 Instance => instance.Value;
    }


    sealed public class Singleton
    {
        static Singleton instance = null;
        private Singleton() { }

        static object lockObject = new object();

        static public Singleton Instance
        {
            get
            {

                if (instance == null)
                {
                    lock (lockObject) // there may me a situation when while 
                                      // checking for null the state was changed
                    {
                        if (instance == null)
                        {
                            instance = new Singleton();
                        }
                    }
                }

                return instance;
            }
            
        }
        public void DoWork()
        {

        }

    }
}
