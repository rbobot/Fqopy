using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;

namespace fqopy
{
    class CopyFilesUtility
    {
        public static IEnumerable<FileCopyResultsItem> CopyFiles( string source, string destination, IEnumerable<string> files ) 
        {
            var crc32 = new Crc32();
            var start = DateTime.Now;

            foreach ( string file in files )
            {
                var dest = file.Replace( source, destination );
                var item = new FileCopyResultsItem() { Source = file, Destination = dest };

                using ( FileStream sourceStream = File.Open( file, FileMode.Open, FileAccess.Read, FileShare.Read ) )
                {
                    try
                    {
                        foreach ( byte b in crc32.ComputeHash( sourceStream ) )
                        {
                            item.SourceCRC += b.ToString( "x2" ).ToLower();
                        }

                        using ( FileStream destinStream = File.Open( dest, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None ) )
                        {
                            bool copyFlag = false;

                            if ( sourceStream.Length > 0 && destinStream.Length == 0 )
                            {
                                copyFlag = true;
                            }

                            if ( destinStream.Length > 0 )
                            {
                                destinStream.SetLength( 0 );
                                destinStream.Flush();
                                copyFlag = true;
                            }

                            if ( copyFlag )
                            {
                                sourceStream.Position = 0;
                                destinStream.Position = 0;
                                sourceStream.CopyTo( destinStream );
                            }

                            destinStream.Position = 0;
                            foreach ( byte b in crc32.ComputeHash( destinStream ) )
                            {
                                item.DestinationCRC += b.ToString( "x2" ).ToLower();
                            }
                            item.Size = destinStream.Length;
                        }
                    }
                    catch ( UnauthorizedAccessException ex )
                    {
                        var er = new ErrorRecord( ex, "6", ErrorCategory.SecurityError, dest );
                        item.ErrorMessage = er.Exception.Message;
                    }
                    catch ( NotSupportedException ex )
                    {
                        var er = new ErrorRecord( ex, "7", ErrorCategory.InvalidOperation, sourceStream );
                        item.ErrorMessage = er.Exception.Message;
                    }
                    catch ( ObjectDisposedException ex )
                    {
                        var er = new ErrorRecord( ex, "8", ErrorCategory.ResourceUnavailable, sourceStream );
                        item.ErrorMessage = er.Exception.Message;
                    }
                    catch ( IOException ex )
                    {
                        var er = new ErrorRecord( ex, "9", ErrorCategory.WriteError, dest );
                        item.ErrorMessage = er.Exception.Message;
                    }
                }

                item.Time = DateTime.Now - start;
                item.Match = item.SourceCRC == item.DestinationCRC;
                yield return item;
            }
        }
    }
}
