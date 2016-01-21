using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace fqopy
{
	class CustomFileInfo
	{
		public string FullPath;
		public string BasePath;
	}

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


		IEnumerable<string> dirsToCreate;
		IEnumerable<string> dirsToRemove;

		IEnumerable<CustomFileInfo> sourceList;
		IEnumerable<CustomFileInfo> destinList;

		IEnumerable<CustomFileInfo> filesToCopy;
		IEnumerable<CustomFileInfo> filesToRemove;

		int countOfFiles = 0;

        protected override void BeginProcessing()
        {

			var sourceIndex = new DirectoryInfo( Source );
			var destinIndex = new DirectoryInfo( Destination );

			// Take a snapshot of the file system.
			sourceList = sourceIndex.GetFiles( "*.*", SearchOption.AllDirectories )
							.Select( file => new CustomFileInfo
							{
								FullPath = file.FullName,
								BasePath = Source
							} );

			destinList = destinIndex.GetFiles( "*.*", SearchOption.AllDirectories )
							.Select( file => new CustomFileInfo
							{
								FullPath = file.FullName,
								BasePath = Destination
							} );

			//A custom file comparer defined below
			FileCompare complexCompare = new FileCompare();
			SimpleCompare simpleCompare = new SimpleCompare();

			filesToCopy = sourceList.Except( destinList, complexCompare );

			filesToRemove = destinList.Except( sourceList, complexCompare )
										  .Except( filesToCopy, simpleCompare );

			IEnumerable<string> listOfSourceDirs = sourceList.Select( path => path.FullPath ).Distinct();

			IEnumerable<string> listOfDestDirs = destinList.Select( path => path.FullPath ).Distinct();

			dirsToCreate = listOfSourceDirs.Select( path => path.Replace( Source, Destination ) )
										   .Except( listOfDestDirs );

			dirsToRemove = listOfDestDirs.Select( path => path.Replace( Destination, Source ) )
										 .Except( listOfSourceDirs );
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
                foreach ( string file in filesToRemove.Select( f => f.FullPath ) )
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

            foreach ( var item in CopyFilesUtility.CopyFiles( Source, Destination, filesToCopy.Select( f => f.FullPath ), Fast ) )
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

	class FileCompare : IEqualityComparer<CustomFileInfo>
	{
		public bool Equals( CustomFileInfo f1, CustomFileInfo f2 )
		{
			return string.Equals( f1.FullPath.Replace( f1.BasePath, "" ),
								  f2.FullPath.Replace( f2.BasePath, "" ),
								  StringComparison.OrdinalIgnoreCase );
		}

		public int GetHashCode( CustomFileInfo fi )
		{
			return GetHashCode( fi.FullPath );
		}

		private static int GetHashCode( string path )
		{
			Crc32 crc32 = new Crc32();
			using ( FileStream sourceFs = File.Open( path, FileMode.Open, FileAccess.Read, FileShare.Read ) )
			{
				string SourceCRC = string.Empty;
				foreach ( byte b in crc32.ComputeHash( sourceFs ) )
				{
					SourceCRC += b.ToString( "x2" ).ToLower();
				}
				return SourceCRC.GetHashCode();
			}
		}
	}

	class SimpleCompare : IEqualityComparer<CustomFileInfo>
	{
		public bool Equals( CustomFileInfo f1, CustomFileInfo f2 )
		{
			return string.Equals( f1.FullPath.Replace( f1.BasePath, "" ),
								  f2.FullPath.Replace( f2.BasePath, "" ),
								  StringComparison.OrdinalIgnoreCase );
		}

		public int GetHashCode( CustomFileInfo fi )
		{
			return fi.FullPath.Replace( fi.BasePath, "" ).GetHashCode();
		}
	}
}
