using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

class FindFiles
{
    static void Usage( string context = null )
    {
        if ( null != context )
            Console.WriteLine( @"Context: {0}", context );

        Console.WriteLine( @"Usage: ff [-s] [-a] [rootpath] <filespec>" );
        Console.WriteLine( @"  arguments:  [rootpath] Optional path to start the enumeration. Default is current drive root \" );
        Console.WriteLine( @"                         Default for MacOS is $HOME if valid else /" );
        Console.WriteLine( @"              <filespec> What to look for" );
        Console.WriteLine( @"              [-a]       Display file attribute info. Default is just path" );
        Console.WriteLine( @"              [-s]       Single-threaded. Slower, but more predictable ordering of results" );
        Console.WriteLine( @"  examples:   ff ff.cxx" );
        Console.WriteLine( @"              ff *.cs" );
        Console.WriteLine( @"              ff -a g:\home *.jpg" );
        Console.WriteLine( @"              ff g:\home *.jpg" );
        Console.WriteLine( @"  (MacOS):    ff \*.cs" );
        Console.WriteLine( @"              ff -a ~ \*.jpg" );
        Console.WriteLine( @"              ff -a ~/Documents \*.jpg" );
        Console.WriteLine( @"  attributes:" );
        Console.WriteLine( @"        a: archive" );
        Console.WriteLine( @"        c: compressed" );
        Console.WriteLine( @"        d: directory" );
        Console.WriteLine( @"        D: device" );
        Console.WriteLine( @"        e: encrypted" );
        Console.WriteLine( @"        h: hidden" );
        Console.WriteLine( @"        i: not content indexed" );
        Console.WriteLine( @"        I: integrity stream" );
        Console.WriteLine( @"        n: normal" );
        Console.WriteLine( @"        N: no scrub data" );
        Console.WriteLine( @"        o: offline" );
        Console.WriteLine( @"        O: recall on open" );
        Console.WriteLine( @"        p: reparse point" );
        Console.WriteLine( @"        P: pinned" );
        Console.WriteLine( @"        r: read only" );
        Console.WriteLine( @"        R: recall on data access (OneDrive placeholder)" );
        Console.WriteLine( @"        s: system" );
        Console.WriteLine( @"        S: sparse" ); 
        Console.WriteLine( @"        t: temporary" );
        Console.WriteLine( @"        u: unpinned" );
        Console.WriteLine( @"        v: virtual" );

        Environment.Exit( 1 );
    } //Usage

    static object lockObj = new object();

    static void DisplayAttributes( FileSystemInfo fi, Int64 length )
    {
        FileAttributes a = fi.Attributes;
        uint x = (uint) a;
    
        Console.Write( ( 0 != ( a & FileAttributes.Archive ) )           ? "a" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.Compressed ) )        ? "c" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.Directory ) )         ? "d" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.Device ) )            ? "D" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.Encrypted ) )         ? "e" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.Hidden ) )            ? "h" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.NotContentIndexed ) ) ? "i" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.IntegrityStream ) )   ? "I" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.Normal ) )            ? "n" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.NoScrubData ) )       ? "N" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.Offline ) )           ? "o" : " " );
        Console.Write( ( 0 != ( x & 0x00040000 ) )                       ? "O" : " " ); // FILE_ATTRIBUTE_RECALL_ON_OPEN
        Console.Write( ( 0 != ( a & FileAttributes.ReparsePoint ) )      ? "p" : " " );
        Console.Write( ( 0 != ( x & 0x00080000 ) )                       ? "P" : " " ); // FILE_ATTRIBUTE_PINNED
        Console.Write( ( 0 != ( a & FileAttributes.ReadOnly ) )          ? "r" : " " );
        Console.Write( ( 0 != ( x & 0x00400000 ) )                       ? "R" : " " ); // FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS
        Console.Write( ( 0 != ( a & FileAttributes.System ) )            ? "s" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.SparseFile ) )        ? "S" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.Temporary ) )         ? "t" : " " );
        Console.Write( ( 0 != ( x & 0x00100000 ) )                       ? "u" : " " ); // FILE_ATTRIBUTE_UNPINNED
        Console.Write( ( 0 != ( x & 0x00010000 ) )                       ? "v" : " " ); // FILE_ATTRIBUTE_VIRTUAL

        Console.Write( " {0,14:N0}  ", length );
        Console.Write( " {0:MM/dd/yy HH:mm:ss}  ", fi.LastWriteTime );
    } //DisplayAttributes

    static void DisplayInfo( FileSystemInfo fi, Int64 length, bool showAttributes )
    {
        lock ( lockObj )
        {
            if ( showAttributes )
                DisplayAttributes( fi, length );
        
            Console.WriteLine( "{0}",
                               #if _WINDOWS
                                   fi.FullName.ToLower() );
                               #else
                                   fi.FullName );
                               #endif
        }
    } //DisplayInfo

    static void Find( DirectoryInfo diRoot, string spec, bool showAttributes, ParallelOptions po )
    {
        // On Windows NTFS, ReparsePoints are skipped by EnumerateDirectories.
        // On MacOS and Linux, they aren't.

#if !_WINDOWS
        if ( 0 != ( diRoot.Attributes & FileAttributes.ReparsePoint ) )
            return;
#endif
 
        try
        {
            Parallel.ForEach( diRoot.EnumerateDirectories(), po, ( subDir ) =>
            {
                Find( subDir, spec, showAttributes, po );
            });
        }
        catch (Exception ex)
        {
            // Some folders are locked-down, and can't be enumerated
            // Console.Error.WriteLine(ex);
        }

        try
        {
            Parallel.ForEach( diRoot.EnumerateDirectories( spec ), po, ( subDir ) =>
            {
                DisplayInfo( subDir, 0, showAttributes );
            });
        }
        catch (Exception ex)
        {
            // Some folders are locked-down, and can't be enumerated
            // Console.Error.WriteLine(ex);
        }

        try
        {
            Parallel.ForEach( diRoot.EnumerateFiles( spec ), po, ( fileInfo ) =>
            {
                DisplayInfo( fileInfo, fileInfo.Length, showAttributes );
            });
        }
        catch (Exception ex)
        {
            // Some folders are locked-down, and can't be enumerated
            // Console.Error.WriteLine(ex);
        }
    } //Find

    static void Main( string[] args )
    {
        if ( args.Count() < 1 || args.Count() > 4 )
            Usage( "argument count must be 1..4" );

        string root = null;
        string filespec = null;
        bool showAttributes = false;
        bool singleThreaded = false;

        for ( int i = 0; i < args.Length; i++ )
        {
            if ( ( '-' == args[i][0] )
#if _WINDOWS
                 || ( '/' == args[i][0] )
#endif
               )
            {
                string arg = args[i];
                string argUpper = arg.ToUpper();
                char c = argUpper[1];

                if ( 'A' == c )
                    showAttributes = true;
                else if ( 'S' == c )
                    singleThreaded = true;
                else
                    Usage( "argument isn't -a or -s, so it's unrecognized" );
            }
            else
            {
                if ( null == filespec )
                    filespec = args[ i ];
                else if ( null == root )
                {
                    root = filespec;
                    filespec = args[ i ];
                }
                else
                    Usage( "filespec and root both defined yet there is another argument: " + args[ i ] );
            }
        }

        if ( null == filespec )
            Usage( "filespec is null" );

        if ( null == root )
        {
#if _WINDOWS
            root = @"\";
#else
            root = Environment.GetEnvironmentVariable( @"HOME" );
            if ( null == root )
                root = @"/";
#endif
        }

        root = Path.GetFullPath( root );

        DirectoryInfo diRoot = new DirectoryInfo( root );
        ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = singleThreaded ? 1 : -1 };

        Find( diRoot, filespec, showAttributes, po );
    } //Main
} //FindFiles


