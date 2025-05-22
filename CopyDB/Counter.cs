using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyDB
{
    class Counter
    {
        private int _value = 0;
        public Counter() { }
        
        public void Increment() {
            Interlocked.Increment(ref _value);
        }

        public void Decrement()
        {
            Interlocked.Decrement(ref _value);
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _value, 0);
        }

        public int Value 
        { 
            get 
            {
                return (int)Interlocked.Read(_value);
                //return Interlocked.CompareExchange(ref _value, 0, 0); 
            } 
        }

    }
}
