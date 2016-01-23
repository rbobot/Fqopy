using System;
using System.Management.Automation.Host;
using System.Text;

namespace fqopy
{
    public static class PSCmdletExtender
    {
        class HostBuffer
        {
            public string       WindowTitle;
            public BufferCell[,] Buffer;
            public Size         BufferSize;
            public int          CursorSize;
            public Coordinates  CursorPosition;
            public ConsoleColor Background;
            public ConsoleColor Foreground;
            public Coordinates  WindowPosition;
            public Size         WindowSize;

        }

        static StringBuilder SB
        {
            get
            {
                return ( _sb == null ) ? _sb = new StringBuilder() : _sb;
            }
        }
        static StringBuilder _sb;

        static HostBuffer HostBufferImage { get; set; }

        public static void ShowInformation( this PSHostRawUserInterface host, string caption, string message, int offsetX = 0, int offsetY = 3 )
        {
            var pos = host.WindowPosition;
            var con = host.WindowSize;

            pos.X += offsetX;
            pos.Y += offsetY;

            string spacer = "  ";
            string[] text = new string[]
                {
                    SB.Remove( 0, SB.Length ).Insert( 0, ( (char) 32 ).ToString(), con.Width).ToString(),
                    SB.Remove( 0, SB.Length ).Append( spacer ).Append( caption )
                                             .Insert( caption.Length + spacer.Length, ( (char) 32 ).ToString(), ( con.Width - caption.Length - spacer.Length) ).ToString(),
                    SB.Remove( 0, SB.Length ).Insert( 0, ( (char) 32 ).ToString(), con.Width).ToString(),
                    SB.Remove( 0, SB.Length ).Append( spacer ).Append( message )
                                             .Insert( message.Length + spacer.Length, ( (char) 32 ).ToString(), ( con.Width - message.Length - spacer.Length) ).ToString(),
                    SB.Remove( 0, SB.Length ).Insert( 0, ( (char) 32 ).ToString(), con.Width).ToString()
                };

            var row = host.NewBufferCellArray( text, ConsoleColor.Black, ConsoleColor.DarkGray );
            host.SetBufferContents( pos, row );
        }

        public static void PushHostUI( this PSHostRawUserInterface host )
        {
            var buffer = new Rectangle( 0, 0, host.BufferSize.Width, host.BufferSize.Height );
            HostBufferImage = new HostBuffer
            {
                BufferSize = host.BufferSize,
                Buffer = host.GetBufferContents( buffer ),

                CursorSize = host.CursorSize,
                CursorPosition = host.CursorPosition,

                Background = host.BackgroundColor,
                Foreground = host.ForegroundColor,

                WindowPosition = host.WindowPosition,
                WindowSize = host.WindowSize,
                WindowTitle = host.WindowTitle
            };
        }
        public static void PopHostUI( this PSHostRawUserInterface host )
        {
            host.BufferSize = HostBufferImage.BufferSize;
            host.SetBufferContents( HostBufferImage.WindowPosition, HostBufferImage.Buffer );

            host.CursorSize = HostBufferImage.CursorSize;
            host.CursorPosition = HostBufferImage.CursorPosition;

            host.BackgroundColor = HostBufferImage.Background;
            host.ForegroundColor = HostBufferImage.Foreground;

            host.WindowTitle = HostBufferImage.WindowTitle;
            host.WindowPosition = HostBufferImage.WindowPosition;
            host.WindowSize = HostBufferImage.WindowSize;
        }
    }
}