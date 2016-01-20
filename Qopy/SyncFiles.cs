using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace fqopy
{
    [Cmdlet( "Sync", "Files" )]
    [CmdletBinding]
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

        IEnumerable<string> filesToCopy     = new List<string>();
        IEnumerable<string> filesToRemove   = new List<string>();
        IEnumerable<string> dirsToCreate = new List<string>();
        IEnumerable<string> dirsToRemove = new List<string>();

        int countOfFiles = 0;

        protected override void BeginProcessing()
        {
            DirectoryInfo sourceDir = new DirectoryInfo( Source );
            DirectoryInfo destinDir = new DirectoryInfo( Destination );

            if (string.Compare( sourceDir.Name, destinDir.Name, true) != 0)
            {
                Destination = Path.Combine( Destination, sourceDir.Name );
                if ( !Directory.Exists( Destination ) )
                {
                    Directory.CreateDirectory( Destination );
                }
                destinDir = new DirectoryInfo( Destination );
            }

            // Take a snapshot of the file system.
            IEnumerable<FileInfo> sourceList = sourceDir.GetFiles( Filter, SearchOption.AllDirectories );
            IEnumerable<FileInfo> destinList = destinDir.GetFiles( Filter, SearchOption.AllDirectories );

            //A custom file comparer defined below
            var myFileCompare = new FileCompare();

            // This query determines whether the two folders contain
            // identical file lists, based on the custom file comparer
            if ( sourceList.SequenceEqual( destinList, myFileCompare ) != true )
            {
                // The following files are in source but not destination.
                filesToCopy = ( from file in sourceList
                                   select file ).Except( destinList, myFileCompare )
                                                .Select( file => file.FullName);
                // The following files are in destination but not source.
                filesToRemove = ( from file in destinList
                                     select file ).Except( sourceList, myFileCompare )
                                                  .Select( file => file.FullName );

                IEnumerable<string> listOfSourceDirs = sourceList.Select( path => Path.GetDirectoryName( path.FullName ) )
                                                                 .Distinct();

                IEnumerable<string> listOfDestDirs = destinList.Select( path => Path.GetDirectoryName( path.FullName ) )
                                                               .Distinct();

                dirsToCreate = listOfSourceDirs.Select( path =>  path.Replace( Source, Destination ) )
                                               .Except( listOfDestDirs );

                dirsToRemove = listOfDestDirs.Select( path => path.Replace( Destination, Source ) )
                                             .Except( listOfSourceDirs );
            }
        }

        protected override void EndProcessing()
        {
            if ( dirsToCreate.Count() != 0 )
            {
                foreach ( string dir in dirsToCreate )
                {
                    if ( !Directory.Exists( dir ) )
                    {
                        try
                        {
                            Directory.CreateDirectory( dir );
                        }
                        catch ( UnauthorizedAccessException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                        catch ( PathTooLongException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                        catch ( ArgumentNullException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                        catch ( ArgumentException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                        catch ( DirectoryNotFoundException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                        catch ( NotSupportedException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                        catch ( IOException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                    }
                }
            }

            if ( dirsToRemove.Count() != 0 )
            {
                foreach ( string dir in dirsToRemove )
                {
                    if ( Directory.Exists( dir ) )
                    {
                        try
                        {
                            Directory.Delete( dir, true );
                        }
                        catch ( UnauthorizedAccessException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                        catch ( PathTooLongException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                        catch ( ArgumentNullException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                        catch ( ArgumentException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                        catch ( DirectoryNotFoundException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                        catch ( NotSupportedException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                        catch ( IOException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                    }
                }
            }

            if ( filesToRemove.Count() != 0 )
            {
                foreach ( string file in filesToRemove )
                {
                    if ( File.Exists( file ) )
                    {
                        try
                        { 
                            File.Delete( file );
                        }
                        catch ( UnauthorizedAccessException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                        catch ( PathTooLongException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                        catch ( ArgumentNullException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                        catch ( ArgumentException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                        catch ( DirectoryNotFoundException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                        catch ( NotSupportedException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                        catch ( IOException ex )
                        {
                            WriteVerbose( ex.Message );
                        }
                    }
                }
            }

            int i = 0;
            var progress = new ProgressRecord( 0, string.Format( "Sync {0} with {1}", Source, Destination ), "Syncing" );
            var startTime = DateTime.Now;

            foreach ( var item in CopyFilesUtility.CopyFiles( Source, Destination, filesToCopy ) )
            {
                if ( !string.IsNullOrEmpty( item.ErrorMessage ) )
                {
                    WriteVerbose( item.ErrorMessage );
                }

                if ( PassThru )
                {
                    WriteObject( item );
                }

                if ( ShowProgress )
                {
                    int percentage = (int) ( (double) ++i / filesToCopy.Count() * 100 );
                    progress.PercentComplete = percentage <= 100 ? percentage : 100;
                    progress.SecondsRemaining = (int) ( ( ( DateTime.Now - startTime ).TotalSeconds / i ) * ( countOfFiles - i ) );
                    WriteProgress( progress );
                }
            }

            if ( ShowProgress )
            {
                progress.RecordType = ProgressRecordType.Completed;
                progress.PercentComplete = 100;
                WriteProgress( progress );
            }

        }
    }

    class FileCompare : IEqualityComparer<FileInfo>
    {
        public FileCompare() { }

        public bool Equals( FileInfo f1, FileInfo f2 )
        {
            return ( f1.Name == f2.Name && f1.Length == f2.Length );
        }

        // Return a hash that reflects the comparison criteria. According to the rules for IEqualityComparer<T>,
        // if Equals is true, then the hash codes must also be equal.
        public int GetHashCode( FileInfo fi )
        {
            Crc32 crc32 = new Crc32();
            using ( FileStream sourceFs = File.Open( fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Read ) )
            {
                string SourceCRC = string.Empty;
                foreach ( byte b in crc32.ComputeHash( sourceFs ) )
                {
                    SourceCRC += b.ToString( "x2" ).ToLower();
                }
                return string.Format( "{0}{1}", fi.Directory.Name, SourceCRC ).GetHashCode();
            }
        }
    }
}
