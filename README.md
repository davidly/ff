# ff
Find File. Windows command line app to find files on a drive. (MacOS-compatible C# version below)

Build using a Visual Studio 64 bit command prompt using m.bat

Usage:

    usage: ff [-l] [-a] [-s] [starting path] filespec
        arguments:   [-a]  show file attributes
                     [-s]  single threaded; default is parallel
                     [-l]  loud (Debug) mode; print status information
                     [starting path] Optional root folder. default is root of current drive
                     [filespec]      File(s) to look for
        examples:    ff -a c:\pictures *.jpg
                     ff -s -a c:\pictures *.jpg
                     ff slothrust.jpg
        attributes:
            a: archive
            c: compressed
            d: directory
            D: device
            e: encrypted
            h: hidden
            i: not content indexed
            I: integrity stream
            n: normal
            N: no scrub data
            o: offline
            O: recall on open
            p: reparse point
            P: pinned
            r: read only
            R: recall on data access (OneDrive placeholder)
            s: system
            S: sparse
            t: temporary
            u: unpinned
            v: virtual
        
The csharp folder has an implementation in C# that runs on MacOS, Linux and Windows. It's about 25% slower than the C++ version on Windows.
To build on Windows using .net 6, use m.bat
To build on MacOS using .net 6, use m.sh
To build on Linux using .net 6, use m-linux.sh

Note that the m.sh script copies the binary ff to ~/bin then code-signs it using a blank signature so it can run locally in that folder. That's required on M1 macs.

    dotnet publish ff.csproj --configuration Release -r osx.12-arm64 -f net6.0 -p:UseAppHost=true --self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:PublishTrimmed=false -o ./ -nologo
    cp ff ~/bin/ff
    # have to re-sign in the target folder or it won't be trusted by MacOS
    codesign -f -s - ~/bin/ff
    
On Linux on WSL, many file attributes are stripped away, whereas on MacOS many attributes are available.

