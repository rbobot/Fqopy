using System.Collections.Generic;
using System.Management.Automation;

namespace fqopy
{
    public class SyncFiles : Cmdlet
    {
        [Parameter( Mandatory = true, Position = 0 )]
        public string Source
        {
            get { return source; }
            set { source = value.TrimEnd( new char[] { '\\', '/' } ); }
        }
        string source;

        [Parameter( Mandatory = true, Position = 1 )]
        public string Destination
        {
            get { return destination; }
            set { destination = value.TrimEnd( new char[] { '\\', '/' } ); }
        }
        string destination;

        [Parameter( Mandatory = false, Position = 2 )]
        public string Filter
        {
            get { return filter; }
            set { filter = value; }
        }
        string filter = "*";

        [Parameter( Mandatory = false )]
        public SwitchParameter Recurse { get; set; }

        [Parameter( Mandatory = false )]
        public SwitchParameter Fast { get; set; }

        [Parameter( Mandatory = false )]
        public SwitchParameter ShowProgress { get; set; }

        [Parameter( Mandatory = false )]
        public SwitchParameter PassThru { get; set; }

        List<string> listOfFiles    = new List<string>();
        List<string> listOfDestDirs = new List<string>();
        int countOfFiles = 0;
        Crc32 crc32 = new Crc32();

        protected override void BeginProcessing()
        {
        }

        protected override void EndProcessing()
        {
        }
    }
}
