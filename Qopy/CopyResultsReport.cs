using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace fqopy
{
	public class FqopyResultsItem
	{
		public string Source;
		public string Destination;
		public long Size;
		public TimeSpan Time;
		public string SourceCRC;
		public string DestinationCRC;
		public bool Match = false;
		public string ErrorMessage = string.Empty;
	}

	public class FqopyResultsReport
    {
        public TimeSpan TotalTime;
        public int FileCount;
        public long Bytes;
        public List<FqopyResultsItem> FailedItemList;

        public FqopyResultsReport()
        {
            FileCount = 0;
            FailedItemList = new List<FqopyResultsItem>();
        }
    }

    [Cmdlet( VerbsCommon.Get, "FqopyResultsReport" )]
    public class FcopyResultsReport : Cmdlet
    {
        [Parameter( Mandatory = true, ValueFromPipeline = true )]
        public FqopyResultsItem InputObject { get; set; }

        FqopyResultsReport report = new FqopyResultsReport();

        protected override void ProcessRecord()
        {
            report.TotalTime += InputObject.Time;
            report.FileCount++;

            if ( !InputObject.Match )
            {
                report.FailedItemList.Add( InputObject );
            }
            else
            {
                report.Bytes += InputObject.Size;
            }
        }

        protected override void EndProcessing()
        {
            WriteObject( report );
        }
    }
}
