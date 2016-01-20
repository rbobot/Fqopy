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

        List<string> filesToCopy     = new List<string>();
        List<string> filesToRemove   = new List<string>();
        List<string> dirsToCreate = new List<string>();
        List<string> dirsToRemove = new List<string>();

        int countOfFiles = 0;
        Crc32 crc32 = new Crc32();

        protected override void BeginProcessing()
        {
            DirectoryInfo sourceDir = new DirectoryInfo( Source );
            DirectoryInfo destinDir = new DirectoryInfo( Destination );

            WriteVerbose( string.Format( "{0}{1}", "Source dir name: ", sourceDir.Name ));
            WriteVerbose( string.Format( "{0}{1}", "Destination dir name: ", destinDir.Name ));

            if (string.Compare( sourceDir.Name, destinDir.Name, true) != 0)
            {
                Destination = Path.Combine( Destination, sourceDir.Name );
                WriteVerbose( string.Format( "{0}{1}", "New Destination dir name: ", Destination ) );
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
                                                .Select( file => file.FullName).ToList();
                // The following files are in destination but not source.
                filesToRemove = ( from file in destinList
                                     select file ).Except( sourceList, myFileCompare )
                                                  .Select( file => file.FullName ).ToList();

                IEnumerable<string> listOfSourceDirs = sourceList.Select( path => Path.GetDirectoryName( path.FullName ) )
                                                                 .Distinct();

                IEnumerable<string> listOfDestDirs = destinList.Select( path => Path.GetDirectoryName( path.FullName ) )
                                                               .Distinct();

                dirsToCreate = listOfSourceDirs.Select( path =>  path.Replace( Source, Destination ) )
                                               .Except( listOfDestDirs ).ToList();

                dirsToRemove = listOfDestDirs.Select( path => path.Replace( Destination, Source ) )
                                             .Except( listOfSourceDirs ).ToList();

                if ( dirsToCreate.Count != 0 )
                {
                    WriteVerbose( "dirs to create: " );
                    foreach ( var dir in dirsToCreate )
                    {
                        WriteVerbose( dir );
                    }
                }
                if ( dirsToRemove.Count != 0 )
                {
                    WriteVerbose( "dirs to remove: " );
                    foreach ( var dir in dirsToRemove )
                    {
                        WriteVerbose( dir );
                    }
                }
                if ( filesToCopy.Count != 0 )
                {
                    WriteVerbose( "files to copy: " );
                    foreach ( var file in filesToCopy )
                    {
                        WriteVerbose( file );
                    }
                }
                if ( filesToRemove.Count != 0 )
                {
                    WriteVerbose( "files to remove: " );
                    foreach ( var file in filesToRemove )
                    {
                        WriteVerbose( file );
                    }
                }
            }
        }

        protected override void EndProcessing()
        {
            if ( dirsToCreate.Count != 0 )
            {
                foreach ( string dir in dirsToCreate )
                {
                    if ( !Directory.Exists( dir ) )
                    {
                        Directory.CreateDirectory( dir );
                    }
                }
            }

            if ( dirsToRemove.Count != 0 )
            {
                foreach ( string dir in dirsToRemove )
                {
                    if ( Directory.Exists( dir ) )
                    {
                        Directory.Delete( dir, true );
                    }
                }
            }

            if ( filesToRemove.Count != 0 )
            {
                foreach ( string file in filesToRemove )
                {
                    if ( File.Exists( file ) )
                    {
                        File.Delete( file );
                    }
                }
            }

            foreach ( string file in filesToCopy )
            {
                string fullDestination = file.Replace( Source, Destination );
                var item = new FileCopyResultsItem() { Source = file, Destination = fullDestination };

                using ( FileStream sourceFs = File.Open( file, FileMode.Open, FileAccess.Read, FileShare.Read ) )
                {
                    foreach ( byte b in crc32.ComputeHash( sourceFs ) )
                    {
                        item.SourceCRC += b.ToString( "x2" ).ToLower();
                    }

                    using ( FileStream dstFs = File.Open( fullDestination, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None ) )
                    {
                        bool copyTheFile = false;

                        if ( sourceFs.Length > 0 && dstFs.Length == 0 )
                        {
                            copyTheFile = true;
                        }

                        if ( dstFs.Length > 0 )
                        {
                            dstFs.SetLength( 0 );
                            dstFs.Flush();
                            copyTheFile = true;
                        }

                        if ( copyTheFile )
                        {
                            sourceFs.Position = 0;
                            dstFs.Position = 0;
                            sourceFs.CopyTo( dstFs );
                        }

                        dstFs.Position = 0;
                        foreach ( byte b in crc32.ComputeHash( dstFs ) )
                            item.DestinationCRC += b.ToString( "x2" ).ToLower();
                        item.Size = dstFs.Length;
                    }
                }
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
