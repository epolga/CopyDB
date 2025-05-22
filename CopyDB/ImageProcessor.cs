using MySqlX.XDevAPI.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CopyDB
{
    public interface IFilter
    {
        void DoFilter(char[] chars);
    }

    public class BlurFilter : IFilter
    {
        public BlurFilter()
        {
        }

        public void DoFilter(char[] chars)
        {
            //do some blurring
        }
    }

    public class GreyFilter : IFilter
    {
        public GreyFilter()
        {
        }

        public void DoFilter(char[] chars)
        {
            //convert image to grey scale
        }
    }

    public class ImageProcessor()
    {
        IFilter? _filter = null;

        public void DoFilter(char[] chars)
        {
            if (_filter == null)
            {
                // write to console
            }
            else
            { 
                _filter.DoFilter(chars);
            }
        }
       
        public void SetFilter(IFilter filter) 
        {
            _filter = filter;
        }

       static private void Kardan(char[] arr, out int max, out int start, out int end)
        {
            if(arr == null || arr.Length == 0)
            {
                max = -1;
                start = -1;
                end = -1;
                return;
            }
            
            int len = arr.Length;
            int maxCur = arr[0];
            max = arr[0];
            int startTmp = 0;
            
            start = 0;
            end = 0;
            for (int i = 1; i < len; i++) {
                
                maxCur += arr[i];
                if (arr[i] > maxCur) //start new sequence
                {
                    maxCur = arr[i];
                    
                    startTmp = i;
                }
               
                if (maxCur > max) 
                { 
                    max = maxCur;
                    start = startTmp;
                    end = i; 
                }
            }
        }
    }

    public class Test1
    {
        ImageProcessor processor = new ImageProcessor();
        public void RunTest()
        {
            char[] chars1 = { '1', '4', '5' /*etc*/};
            char[] chars2 = { '5', '3', '6' /*etc*/};
            processor.SetFilter(new BlurFilter());
            processor.DoFilter(chars1);
            processor.SetFilter(new GreyFilter());
            processor.DoFilter(chars1);
        }
    }
}
