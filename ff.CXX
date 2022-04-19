#define UNICODE

#include <stdio.h>
#include <process.h>
#include <direct.h>
#include <stdlib.h>
#include <ctype.h>
#include <ppl.h>

#include <mutex>
#include <chrono>

#include <windows.h>

using namespace concurrency;
using namespace std;

#include <djlsav.hxx>

std::mutex mtxGlobal;
bool singleThreaded = FALSE;
bool showAttributes = FALSE;
bool loudMode = FALSE;

void PrintAttrib( DWORD a )
{
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_ARCHIVE ) ) ? 'a' : ' ' );
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_COMPRESSED ) ) ? 'c' : ' ' );
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_DIRECTORY ) ) ? 'd' : ' ' );
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_DEVICE ) ) ? 'D' : ' ' );
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_ENCRYPTED ) ) ? 'e' : ' ' );
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_HIDDEN ) ) ? 'h' : ' ' );
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_NOT_CONTENT_INDEXED ) ) ? 'i' : ' ' );
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_INTEGRITY_STREAM ) ) ? 'I' : ' ' );
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_NORMAL ) ) ? 'n' : ' ' );
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_NO_SCRUB_DATA ) ) ? 'N' : ' ' );
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_OFFLINE ) ) ? 'o' : ' ' );
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_RECALL_ON_OPEN ) ) ? 'O' : ' ' );
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_REPARSE_POINT ) ) ? 'p' : ' ' );
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_PINNED ) ) ? 'P' : ' ' );
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_READONLY ) ) ? 'r' : ' ' );
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS ) ) ? 'R' : ' ' );
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_SYSTEM ) ) ? 's' : ' ' );
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_SPARSE_FILE ) ) ? 'S' : ' ' );
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_TEMPORARY ) ) ? 't' : ' ' );
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_UNPINNED ) ) ? 'u' : ' ' );
    putchar( ( 0 != ( a & FILE_ATTRIBUTE_VIRTUAL ) ) ? 'v' : ' ' );
} //PrintAttrib

void DoScope( WCHAR *pwcPath, WCHAR *pwcSpec )
{
    int pathLen = wcslen( pwcPath );
    int specLen = wcslen( pwcSpec );

    WCHAR awcSpec[ MAX_PATH ];
    if ( ( pathLen + specLen + 1 ) >= _countof( awcSpec ) )
        return;

    WCHAR awcCurDir[ MAX_PATH ];
    wcscpy( awcCurDir, pwcPath );
    _wcslwr( awcCurDir );
    if ( awcCurDir[ pathLen - 1 ] != L'\\' )
    {
        awcCurDir[ pathLen++ ] = L'\\';
        awcCurDir[ pathLen ] = 0;
    }

    wcscpy( awcSpec, awcCurDir);
    wcscpy( awcSpec + pathLen, pwcSpec );

    if ( loudMode )
    {
        lock_guard<mutex> lock( mtxGlobal );
        wprintf( L"DoScope FFF file lookup %ws\n", awcSpec );
    }

    WIN32_FIND_DATA fd;
    HANDLE hFile = FindFirstFileEx( awcSpec, FindExInfoBasic, &fd, FindExSearchNameMatch, 0, FIND_FIRST_EX_LARGE_FETCH );

    if ( INVALID_HANDLE_VALUE != hFile )
    {
        do
        {
            if ( wcscmp( fd.cFileName, L"." ) &&
                 wcscmp( fd.cFileName, L".." ) )
            {
                lock_guard<mutex> lock( mtxGlobal );

                if ( showAttributes )
                {
                    PrintAttrib( fd.dwFileAttributes );

                    long long size = ( (long long) fd.nFileSizeHigh * (long long) ( MAXDWORD + 1 ) ) + (long long) fd.nFileSizeLow;
                    printf( "%12I64d  ", size );
        
                    FILETIME ft;
                    FileTimeToLocalFileTime( &fd.ftLastWriteTime, &ft );
        
                    SYSTEMTIME st;
                    FileTimeToSystemTime( &ft, &st );
        
                    BOOL pm = st.wHour >= 12;
        
                    if ( st.wHour > 12 )
                        st.wHour -= 12;
                    else if ( 0 == st.wHour )
                        st.wHour = 12;
                       
                    printf( "%2d-%02d-%04d %2d:%02d%c  ",
                            (DWORD) st.wMonth,
                            (DWORD) st.wDay,
                            (DWORD) st.wYear,
                            (DWORD) st.wHour,
                            (DWORD) st.wMinute,
                            pm ? 'p' : 'a');
                }
    
                _wcslwr( fd.cFileName );
                printf( "%ws%ws\n", awcCurDir, fd.cFileName );
            }
        } while ( FindNextFile( hFile, &fd ) );

        FindClose( hFile );
    }

    // Now look for directories

    wcscpy( awcSpec, awcCurDir );
    wcscpy( awcSpec + pathLen, L"*" );

    if ( loudMode )
    {
        lock_guard<mutex> lock( mtxGlobal );
        wprintf( L"DoScope FFF folder lookup %ws\n", awcSpec );
    }

    CStringArray aDirs;
                                               
    hFile = FindFirstFileEx( awcSpec, FindExInfoBasic, &fd, FindExSearchLimitToDirectories, 0, FIND_FIRST_EX_LARGE_FETCH );

    if ( INVALID_HANDLE_VALUE != hFile )
    {
        do
        {
            if ( ( fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY ) &&
                 ( 0 != wcscmp( fd.cFileName, L".") ) &&
                 ( 0 != wcscmp( fd.cFileName, L"..") ) )           //( 0 == ( fd.dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT ) ) )
            {
                int fileLen = wcslen( fd.cFileName );

                if ( ( pathLen + fileLen + 1 ) >= _countof( awcSpec ) )
                    continue;

                wcscpy( awcSpec + pathLen, fd.cFileName );
                aDirs.Add( awcSpec );
            }
        } while ( FindNextFile( hFile, &fd ) );

        FindClose( hFile );
    }

    if ( 0 != aDirs.Count() )
    {
        if ( singleThreaded )
        {
            for ( int i = 0; i < aDirs.Count(); i++ )
            {
                try
                {
                    DoScope( aDirs[ i ], pwcSpec );
                }
                catch( ... )
                {
                    wprintf( L"caught exception in serial processing subfolders\n" );
                }
            }
        }
        else
        {
            parallel_for ( size_t(0), size_t( aDirs.Count() ), [&]( size_t i )
            {
                try
                {
                    DoScope( aDirs[ i ], pwcSpec );
                }
                catch( ... )
                {
                    wprintf( L"caught exception in parallel_for processing subfolders\n" );
                }
            });
        }
    }
} //DoScope

void Usage( WCHAR *pwcApp )
{
    wprintf( L"usage: %ws [-l] [-a] [-s] [starting path] filespec\n", pwcApp );
    wprintf( L"    arguments:   [-a]  show file attributes\n" );
    wprintf( L"                 [-s]  single threaded; default is parallel\n" );
    wprintf( L"                 [-l]  loud (Debug) mode; print status information\n" );
    wprintf( L"                 [starting path] Optional root folder. default is root of current drive\n" );
    wprintf( L"                 [filespec]      File(s) to look for\n" );
    wprintf( L"    examples:    ff -a c:\\pictures *.jpg\n" );
    wprintf( L"                 ff -s -a c:\\pictures *.jpg\n" );
    wprintf( L"                 ff slothrust.jpg\n" );
    wprintf( L"    attributes:\n" );
    wprintf( L"        a: archive\n" );
    wprintf( L"        c: compressed\n" );
    wprintf( L"        d: directory\n" );
    wprintf( L"        D: device\n" );
    wprintf( L"        e: encrypted\n" );
    wprintf( L"        h: hidden\n" );
    wprintf( L"        i: not content indexed\n" );
    wprintf( L"        I: integrity stream\n" );
    wprintf( L"        n: normal\n" );
    wprintf( L"        N: no scrub data\n" );
    wprintf( L"        o: offline\n" );
    wprintf( L"        O: recall on open\n" );
    wprintf( L"        p: reparse point\n" );
    wprintf( L"        P: pinned\n" );
    wprintf( L"        r: read only\n" );
    wprintf( L"        R: recall on data access (OneDrive placeholder)\n" );
    wprintf( L"        s: system\n" );
    wprintf( L"        S: sparse\n" ); 
    wprintf( L"        t: temporary\n" ); 
    wprintf( L"        u: unpinned\n" );
    wprintf( L"        v: virtual\n" );
    exit( 1 );
} //Usage

extern "C" int __cdecl wmain( int argc, WCHAR * argv[] )
{
    _set_se_translator([](unsigned int u, EXCEPTION_POINTERS *pExp)
    {
        wprintf( L"translating exception %x\n", u );
        std::string error = "SE Exception: ";
        switch (u)
        {
            case 0xC0000005:
                error += "Access Violation";
                break;
            default:
                char result[11];
                sprintf_s(result, 11, "0x%08X", u);
                error += result;
        };

        wprintf( L"throwing std::exception\n" );
    
        throw std::exception(error.c_str());
    });

    if ( argc < 2 || argc > 5 )
        Usage( argv[0] );

    try
    {
        WCHAR awcPath[MAX_PATH];
        WCHAR awcSpec[MAX_PATH];
        awcPath[ 0 ] = 0;
        awcSpec[ 0 ] = 0;
    
        int iArg = 1;
        while ( iArg < argc )
        {
            //wprintf( L"ff arg %d: '%ws'\n", iArg, argv[iArg] );
            const WCHAR * pwcArg = argv[iArg];
            WCHAR a0 = pwcArg[0];
    
            if ( ( L'-' == a0 ) ||
                 ( L'/' == a0 ) )
            {
               WCHAR a1 = towlower( pwcArg[1] );
    
               if ( L'a' == a1 )
                   showAttributes = TRUE;
               else if ( L's' == a1 )
                   singleThreaded = TRUE;
               else if ( L'l' == a1 )
                   loudMode = TRUE;
               else
                   Usage( argv[ 0 ] );
            }
            else
            {
                if ( 0 == awcSpec[0] )
                    wcscpy( awcSpec, argv[ iArg ] );
                else if ( 0 == awcPath[0] )
                {
                    wcscpy( awcPath, awcSpec );
                    wcscpy( awcSpec, argv[ iArg ] );
                }
                else
                    Usage( argv[ 0 ] );
            }
    
            iArg++;
        }
    
        WCHAR awcFullPath[ MAX_PATH ];
        awcFullPath[0] = 0;

        //wprintf( L"before: awcPath '%ws', awcSpec '%ws'\n", awcPath, awcSpec );
    
        if ( 0 == awcPath[ 0 ] )
            _wfullpath( awcFullPath, L"\\", sizeof awcPath / sizeof WCHAR );
        else
            _wfullpath( awcFullPath, awcPath, sizeof awcPath / sizeof WCHAR );

        //wprintf( L"after: awcFullPath '%ws'\n", awcFullPath );
    
        if ( wcschr( awcSpec, L':' ) ) // forgot a space between args?
            Usage( argv[0] );
    
        DoScope( awcFullPath, awcSpec );
    }
    catch( ... )
    {
        wprintf( L"caught an exception in ff.exe, exiting\n" );
    }

    return 0;
} //main
