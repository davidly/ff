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
        Console.WriteLine( @"  attributes: r    readonly" );
        Console.WriteLine( @"              h    hidden" );
        Console.WriteLine( @"              e    encrypted" );
        Console.WriteLine( @"              s    system" );
        Console.WriteLine( @"              o    offline" );
        Console.WriteLine( @"              p    sparse" );
        Console.WriteLine( @"              d    directory" );
        Console.WriteLine( @"              c    compressed" );
        Console.WriteLine( @"              i    not content indexed" );
        Console.WriteLine( @"              a    reparse point" );

        Environment.Exit( 1 );
    } //Usage

    static object lockObj = new object();

    static void DisplayAttributes( FileInfo fi )
    {
        FileAttributes a = fi.Attributes;
    
        Console.Write( ( 0 != ( a & FileAttributes.ReadOnly ) )          ? "r" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.Hidden ) )            ? "h" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.Encrypted ) )         ? "e" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.System ) )            ? "s" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.Offline ) )           ? "o" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.SparseFile ) )        ? "p" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.Directory ) )         ? "d" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.Compressed ) )        ? "c" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.NotContentIndexed ) ) ? "i" : " " );
        Console.Write( ( 0 != ( a & FileAttributes.ReparsePoint ) )      ? "a" : " " );
    
        Console.Write( " {0,14:N0}  ", fi.Length );
        Console.Write( " {0:MM/dd/yy HH:mm:ss}  ", fi.LastWriteTime );
    } //DisplayAttributes

    static void Find( DirectoryInfo diRoot, string spec, bool attributes, ParallelOptions po )
    {
        // On Windows NTFS, ReparsePoints are skipped by EnumerateDirectories.
        // On MacOS, they aren't. This check is redundant on Windows

        if ( 0 != ( diRoot.Attributes & FileAttributes.ReparsePoint ) )
            return;
 
        try
        {
            Parallel.ForEach( diRoot.EnumerateDirectories(), po, ( subDir ) =>
            {
                Find( subDir, spec, attributes, po );
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
                lock ( lockObj )
                {
                    if ( attributes )
                        DisplayAttributes( fileInfo );
    
                    Console.WriteLine( "{0}",
#if _WINDOWS
                                       fileInfo.FullName.ToLower() );
#else
                                       fileInfo.FullName );
#endif
                }
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
        bool attributes = false;
        bool singlethreaded = false;

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
                    attributes = true;
                else if ( 'S' == c )
                    singlethreaded = true;
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
        ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = singlethreaded ? 1 : -1 };

        Find( diRoot, filespec, attributes, po );
    } //Main
} //FindFiles


