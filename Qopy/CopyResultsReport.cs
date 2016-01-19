using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace fqopy
{
    public class FileCopyResultsReport
    {
        public TimeSpan TotalTime;
        public int FileCount;
        public long Bytes;
        public List<FileCopyResultsItem> FailedItemList;

        public FileCopyResultsReport()
        {
            FileCount = 0;
            FailedItemList = new List<FileCopyResultsItem>();
        }
    }

    [Cmdlet( VerbsCommon.Get, "CopyResultsReport" )]
    public class CopyResultsReport : Cmdlet
    {
        [Parameter( Mandatory = true, ValueFromPipeline = true )]
        public FileCopyResultsItem InputObject { get; set; }

        FileCopyResultsReport report = new FileCopyResultsReport();

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
