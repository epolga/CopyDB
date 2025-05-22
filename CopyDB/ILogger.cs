using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyDB
{
    abstract public class Logger
    {
        abstract public void Log(string message);
    }

    public class ConsoleLogger : Logger
    {
        public override void Log(string message)
        {
            Console.WriteLine(message);
        }
    }

    public class FileLogger : Logger
    {
        string _filePath = string.Empty;
        FileLogger(string filePath)
        {
            if (File.Exists(filePath))
            {
                _filePath = filePath;
            }
            else
            {
                throw new FileNotFoundException();
            }
        }
        public override void Log(string message)
        {
            File.WriteAllText(_filePath, message);
        }
    }
}
