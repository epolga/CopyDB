using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyDB
{
    // Provides interface for parsers which parse logs from different CT machines
    public interface IParser
    {
        void GetMachineID();
        void GetMachineName();

        void GetMachineVersion();

      
        void Parse();

        abstract int MachineID { set; }
        abstract string MachineName { set; }
        abstract string MachineVersion { set; }  

    }

    public class ParsersComposition()
    {
        List<IParser> _parsers = new List<IParser>();
        public void AddParser(IParser parser) { 
            _parsers.Add(parser);
        }
        public void ParseLogs() {
            foreach (IParser parser in _parsers) { 
                parser.GetMachineID();
                parser.GetMachineName();
                parser.GetMachineVersion();
                parser.Parse();
            }
        }
    }

    public class ParserMX : IParser
    {
        int _machineID;
        string _machineName;
        string _machineVersion;

        public int MachineID
        {
            set
            {
                _machineID = value;
            }
        }
        public string MachineName
        {
            set
            {
                _machineName = value;
            }
        }
        public string MachineVersion
        {
            set
            {
                _machineVersion = value;
            }
        }

        public void GetMachineID()
        {
            //read machineID from file ID.txt
        }

        public void GetMachineName()
        {
            //read machineID from file Name.txt
        }

        public void GetMachineVersion()
        {
            //read machineID from file Version.txt
        }

        public void Parse()
        {
           // Parse all files in subfolder logs
        }
    }

    public class ParserII : IParser
    {
        int _machineID;
        string _machineName;
        string _machineVersion;

        public int MachineID
        {
            set
            {
                _machineID = value;
            }
        }
        public string MachineName
        {
            set
            {
                _machineName = value;
            }
        }
        public string MachineVersion
        {
            set
            {
                _machineVersion = value;
            }
        }

        public void GetMachineID()
        {
            //read machineID from file SystemInfo.xml
        }

        public void GetMachineName()
        {
            //read machineID from file SystemInfo.xml
        }

        public void GetMachineVersion()
        {
            //read machineID from file   file SystemInfo.xml
        }

        public void Parse()
        {
            // Parse all files with extension *.logs
        }
    }

    public class Test
    {
        public void TestParse()
        {
            ParsersComposition parsersComposition = new ParsersComposition();
            parsersComposition.AddParser(new ParserII());
            parsersComposition.AddParser(new ParserMX());
            parsersComposition.ParseLogs();
        }
    }
}
