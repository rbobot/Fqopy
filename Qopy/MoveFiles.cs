using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace fqopy
{
	public class FileMoveResultsItem
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

	[Cmdlet( VerbsCommon.Move, "Files" )]
	[CmdletBinding]
	public class MoveFiles : PSCmdlet
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
		public string Filter { get; set; } = "*";

		[Parameter( Mandatory = false )]
		public SwitchParameter Recurse { get; set; }

		[Parameter( Mandatory = false )]
		public SwitchParameter Overwrite { get; set; }

		[Parameter( Mandatory = false )]
		public SwitchParameter Fast { get; set; }

		[Parameter( Mandatory = false )]
		public string List { get; set; }

		[Parameter( Mandatory = false )]
		public SwitchParameter PassThru { get; set; }

		IEnumerable<string> filesToCopy    = new List<string>();
		IEnumerable<string> listOfDestDirs = new List<string>();

		protected override void BeginProcessing()
		{
			try
			{
				if ( string.IsNullOrEmpty( List ) )
				{
					var searchOption = Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
					filesToCopy = Directory.GetFiles( Source, Filter, searchOption );
				}
				else
				{
					filesToCopy = ( File.ReadAllLines( List ) ).Select( path => Path.GetFullPath( Source + path ) );
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

			if ( filesToCopy != null )
			{
				listOfDestDirs = filesToCopy.Select( path => Path.GetDirectoryName( path.Replace( Source, Destination ) ) ).Distinct();
			}
		}

		protected override void EndProcessing()
		{
			if ( filesToCopy != null )
			{
				if ( !PassThru )
				{
					Host.UI.RawUI.PushHostUI();
				}

				foreach ( string dir in listOfDestDirs )
				{
					if ( !PassThru )
					{
						Host.UI.RawUI.ShowInformation( "Creating directories", dir );
					}
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

				foreach ( var item in CopyFilesUtility.CopyFiles( Source, Destination, filesToCopy, Fast, Overwrite ) )
				{
					if ( string.IsNullOrEmpty( item.ErrorMessage ) )
					{
						try
						{
							File.Delete( item.Source );
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
						catch ( PathTooLongException ex )
						{
							WriteVerbose( ex.Message );
						}
						catch ( IOException ex )
						{
							WriteVerbose( ex.Message );
						}
						catch ( UnauthorizedAccessException ex )
						{
							WriteVerbose( ex.Message );
						}

					}
					else
					{
						WriteVerbose( item.ErrorMessage );
					}

					if ( PassThru )
					{
						WriteObject( item );
					}
					else
					{
						Host.UI.RawUI.ShowInformation( "Moving files", item.Source );
					}
				}

				if ( !PassThru )
				{
					Host.UI.RawUI.PopHostUI();
				}
			}
		}
	}
}
