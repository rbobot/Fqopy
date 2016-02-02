using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace fqopy
{
	[Cmdlet( "Sync", "Files" )]
	[CmdletBinding]
	public class SyncFiles : PSCmdlet
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
		public SwitchParameter Fast { get; set; }

		[Parameter( Mandatory = false )]
		public SwitchParameter PassThru { get; set; }

		List<string> FoldersToCopy = new List<string>();
		List<string> FoldersToRemove = new List<string>();
		List<string> FilesToCopy   = new List<string>();
		List<string> FilesToRemove = new List<string>();

		protected override void BeginProcessing()
		{
			if ( !PassThru )
			{
				Host.UI.RawUI.PushHostUI();
				Host.UI.RawUI.ShowInformation( "Comparing directories", "..." );
			}

			CompareFolders( Source, Destination, ref FoldersToCopy, ref FoldersToRemove, ref FilesToCopy, ref FilesToRemove );
		}

		protected override void EndProcessing()
		{
			if ( FoldersToCopy.Count() != 0 )
			{
				foreach ( string dir in FoldersToCopy )
				{
					if ( !PassThru )
					{
						Host.UI.RawUI.ShowInformation( "Copying directories", dir );
					}
					if ( Directory.Exists( dir ) )
					{
						try
						{
							var destDir = dir.Replace( Source, Destination );
							CopyDirectoriesUtility.DirectoryCopy(dir, destDir );

						}
						catch ( Exception ex )
						{
							WriteVerbose( ex.Message );
						}
					}
				}
			}

			if ( FoldersToRemove.Count() != 0 )
			{
				foreach ( string dir in FoldersToRemove )
				{
					if ( !PassThru )
					{
						Host.UI.RawUI.ShowInformation( "Removing directories", dir );
					}
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

			if ( FilesToRemove.Count() != 0 )
			{
				foreach ( string file in FilesToRemove )
				{
					if ( !PassThru )
					{
						Host.UI.RawUI.ShowInformation( "Removing files", file );
					}
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

			foreach ( var item in CopyFilesUtility.CopyFiles( Source, Destination, FilesToCopy, Fast ) )
			{
				if ( !string.IsNullOrEmpty( item.ErrorMessage ) )
				{
					WriteVerbose( item.ErrorMessage );
				}

				if ( PassThru )
				{
					WriteObject( item );
				}
				else
				{
					Host.UI.RawUI.ShowInformation( "Copying files", item.Source );
				}
			}

			if ( !PassThru )
			{
				Host.UI.RawUI.PopHostUI();
			}
		}

		static void CompareFolders( string firstpath, 
									string secondpath, 
									ref List<string> foldersToCopy,
									ref List<string> foldersToRemove,
									ref List<string> filesToCopy,
									ref List<string> filesToRemove )
		{
			// enumerate folders
			IEnumerable<string> firstFolderDirectories = Directory.GetDirectories( firstpath )
																  .Select( s => Path.GetFileName( s ) );
			IEnumerable<string> secondFolderDirectories = Directory.GetDirectories( secondpath )
																   .Select( s => Path.GetFileName( s ) );
			// compare folders and collect defferencies
			var foldersToRecurse = new List<string>();
			foreach ( string dir in firstFolderDirectories )
			{
				if ( secondFolderDirectories.Contains( dir ) )
				{
					// collect top level folders for looping
					foldersToRecurse.Add( Path.Combine( firstpath, dir ) );
				}
				else
				{
					// collect folders from source which not exist in destination
					foldersToCopy.Add( Path.Combine( firstpath, dir ) );
				}
			}

			// collect folders from destination which not exist in source
			foreach ( string dir in secondFolderDirectories.Except( firstFolderDirectories ) )
				foldersToRemove.Add( Path.Combine( secondpath, dir ) );

			// enumerate top level files
			IEnumerable<CustomFileInfo> firstFolderFiles = Directory.GetFiles( firstpath )
																	.Select( s => new CustomFileInfo
																	{
																		FileName = Path.GetFileName( s ),
																		FullPath = s
																	} );
			IEnumerable<CustomFileInfo> secondFolderFiles = Directory.GetFiles( secondpath )
																	 .Select( s => new CustomFileInfo
																	 {
																		 FileName = Path.GetFileName( s ),
																		 FullPath = s
																	 } );

			var Crc32Compare = new Crc32FileComparator();
			var DumbCompare = new DumbFileComparator();

			// collect files from source which not exist in destination
			var differentFilesFromSource = firstFolderFiles.Except( secondFolderFiles, Crc32Compare );
			// collect files from destination which not exist in source
			var differentFilesFromDestin = secondFolderFiles.Except( firstFolderFiles, Crc32Compare );

			// files to copy
			foreach ( CustomFileInfo file in differentFilesFromSource )
			{
				filesToCopy.Add( file.FullPath );
			}

			// files to remove
			foreach ( CustomFileInfo file in differentFilesFromDestin.Except( differentFilesFromSource, DumbCompare ) )
			{
				filesToRemove.Add( file.FullPath );
			}

			foreach ( string firstInnerPath in foldersToRecurse )
			{
				string secondInnerPath = firstInnerPath.Replace( firstpath, secondpath );
				CompareFolders( firstInnerPath, secondInnerPath, ref foldersToCopy, ref foldersToRemove, ref filesToCopy, ref filesToRemove );
			}
		}

	}

	class Crc32FileComparator : IEqualityComparer<CustomFileInfo>
	{
		public bool Equals( CustomFileInfo f1, CustomFileInfo f2 )
		{
			return string.Equals( f1.FileName, f2.FileName );
		}

		public int GetHashCode( CustomFileInfo fi )
		{
			return GetHashCode( fi.FullPath );
		}

		static int GetHashCode( string path )
		{
			int result;
			if ( File.Exists( path ) )
			{
				Crc32 crc32 = new Crc32();
				using ( FileStream sourceFs = File.Open( path, FileMode.Open, FileAccess.Read, FileShare.Read ) )
				{
					string SourceCRC = string.Empty;
					foreach ( byte b in crc32.ComputeHash( sourceFs ) )
					{
						SourceCRC += b.ToString( "x2" ).ToLower();
					}
					result = SourceCRC.GetHashCode();
				}
			}
			else
			{
				result = 0;
			}
			return result;
		}
	}

	class DumbFileComparator : IEqualityComparer<CustomFileInfo>
	{
		public bool Equals( CustomFileInfo f1, CustomFileInfo f2 )
		{
			return string.Equals( f1.FileName, f2.FileName );
		}

		public int GetHashCode( CustomFileInfo fi )
		{
			return fi.FileName.GetHashCode();
		}
	}

	class CustomFileInfo
	{
		public string FileName;
		public string FullPath;
	}

}
