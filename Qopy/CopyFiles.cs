using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace fqopy
{
	public class FileCopyResultsItem
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

	[Cmdlet( VerbsCommon.Copy, "Files" )]
	[CmdletBinding]
	public class CopyFiles : Cmdlet
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
		public SwitchParameter Overwrite { get; set; }

		[Parameter( Mandatory = false )]
		public SwitchParameter Fast { get; set; }

		[Parameter( Mandatory = false )]
		public SwitchParameter ShowProgress { get; set; }

		[Parameter( Mandatory = false )]
		public string List { get; set; }

		[Parameter( Mandatory = false )]
		public SwitchParameter PassThru { get; set; }

		List<string> listOfFiles    = new List<string>();
		List<string> listOfDestDirs = new List<string>();
		int countOfFiles = 0;
		Crc32 crc32 = new Crc32();


		protected override void BeginProcessing()
		{
			try
			{
				if ( string.IsNullOrEmpty( List ) )
				{
					var searchOption = Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
					listOfFiles = Directory.GetFiles( Source, Filter, searchOption ).ToList();
				}
				else
				{
					listOfFiles = ( File.ReadAllLines( List ) ).Select( path => Path.GetFullPath( Source + path ) ).ToList();
				}
			}
			catch ( ArgumentException ex )
			{
				WriteError( new ErrorRecord( ex, "1", ErrorCategory.InvalidArgument, Source ) );
			}
			catch ( DirectoryNotFoundException ex )
			{
				WriteError( new ErrorRecord( ex, "2", ErrorCategory.ObjectNotFound, Source ) );
			}
			catch ( IOException ex )
			{
				WriteError( new ErrorRecord( ex, "3", ErrorCategory.ReadError, Source ) );
			}
			catch ( UnauthorizedAccessException ex )
			{
				WriteError( new ErrorRecord( ex, "4", ErrorCategory.PermissionDenied, Source ) );
			}

			if ( listOfFiles != null )
			{
				listOfDestDirs = listOfFiles.Select( path => Path.GetDirectoryName( path.Replace( Source, Destination ) ) ).Distinct().ToList();
				countOfFiles = listOfFiles.Count();
			}
		}

		protected override void EndProcessing()
		{
			if ( listOfFiles != null )
			{
				int i = 0;
				var progress = new ProgressRecord( 0, string.Format( "Copy from {0} to {1}", Source, Destination ), "Copying" );
				var startTime = DateTime.Now;

				foreach ( string dir in listOfDestDirs )
				{
					try
					{
						if ( !Directory.Exists( dir ) )
						{
							Directory.CreateDirectory( dir );
						}
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

				foreach ( string file in listOfFiles )
				{
					string fullDestination = file.Replace( Source, Destination );

					var item = new FileCopyResultsItem() { Source = file, Destination = fullDestination };

					var start = DateTime.Now;

					if ( !File.Exists( file ) )
					{
						var er = new ErrorRecord( new Exception( String.Format( "Could not find file: {0}", file ) ), "6", ErrorCategory.SecurityError, fullDestination );
						item.ErrorMessage = er.Exception.Message;
					}
					else
					{
						using ( FileStream sourceFs = File.Open( file, FileMode.Open, FileAccess.Read, FileShare.Read ) )
						{
							foreach ( byte b in crc32.ComputeHash( sourceFs ) )
							{
								item.SourceCRC += b.ToString( "x2" ).ToLower();
							}

							try
							{
								using ( FileStream dstFs = File.Open( fullDestination, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None ) )
								{
									bool copyTheFile = false;

									if ( sourceFs.Length > 0 && ( dstFs.Length == 0 || Overwrite ) )
									{
										copyTheFile = true;
									}

									if ( dstFs.Length > 0 && Overwrite )
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

								if ( !Fast )
								{
									File.SetCreationTimeUtc( fullDestination, File.GetCreationTimeUtc( file ) );
									File.SetLastWriteTimeUtc( fullDestination, File.GetLastWriteTimeUtc( file ) );
									File.SetLastAccessTimeUtc( fullDestination, File.GetLastAccessTimeUtc( file ) );
								}
							}
							catch ( UnauthorizedAccessException ex )
							{
								var er = new ErrorRecord( ex, "6", ErrorCategory.SecurityError, fullDestination );
								item.ErrorMessage = er.Exception.Message;
							}
							catch ( NotSupportedException ex )
							{
								var er = new ErrorRecord( ex, "7", ErrorCategory.InvalidOperation, sourceFs );
								item.ErrorMessage = er.Exception.Message;
							}
							catch ( ObjectDisposedException ex )
							{
								var er = new ErrorRecord( ex, "8", ErrorCategory.ResourceUnavailable, sourceFs );
								item.ErrorMessage = er.Exception.Message;
							}
							catch ( IOException ex )
							{
								var er = new ErrorRecord( ex, "9", ErrorCategory.WriteError, fullDestination );
								item.ErrorMessage = er.Exception.Message;
							}

						}

						item.Time = DateTime.Now - start;
						item.Match = item.SourceCRC == item.DestinationCRC;
					}

					if ( ShowProgress )
					{
						int percentage = (int) ( (double) ++i / listOfFiles.Count() * 100 );
						progress.PercentComplete = percentage <= 100 ? percentage : 100;
						progress.SecondsRemaining = (int) ( ( ( DateTime.Now - startTime ).TotalSeconds / i ) * ( countOfFiles - i ) );
						WriteProgress( progress );
					}

					if ( !string.IsNullOrEmpty( item.ErrorMessage ) )
					{
						WriteVerbose( item.ErrorMessage );
					}

					if ( PassThru )
					{
						WriteObject( item );
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
	}
}
