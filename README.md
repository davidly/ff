# ff
Find File. Windows command line app to find files on a drive.

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
                     ff slothrust.jpgattributes:
            a: archive
            c: compressed
            d: directory
            e: encrypted
            h: hidden
            i: not content indexed
            I: integrity stream
            n: normal
            o: offline
            O: recall on open
            p: reparse point
            r: read only
            R: recall on data access (OneDrive placeholder)
            s: system
            S: sparse
            v: virtual
