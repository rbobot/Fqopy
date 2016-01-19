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
        public FileCopyResultsItem InputObject
        {
            get { return inputObject; }
            set { inputObject = value; }
        }
        FileCopyResultsItem inputObject;

        FileCopyResultsReport report = new FileCopyResultsReport();

        protected override void ProcessRecord()
        {
            report.TotalTime += inputObject.Time;
            report.FileCount++;

            if ( !inputObject.Match )
            {
                report.FailedItemList.Add( inputObject );
            }
            else
            {
                report.Bytes += inputObject.Size;
            }
        }

        protected override void EndProcessing()
        {
            WriteObject( report );
        }

    }
}
