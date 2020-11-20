What?
====================
I needed to get random access (and not copy-all-at-once) to a `StreamingAssets` file. Unity doesn't provide an interface to this. 
There are a few plugins around with loads of depedencies, lots of java code, or lots of c code, which seems uncessary when we can use JNI inside c#

How?
======================
This code uses the .java class from google. The android documentation (https://developer.android.com/google/play/expansion-files) says it's in `<sdk>/extras/google/` but it's not for me (r23 installed from homebrew)
So I stole `ZipResourceFile.java` from google (I could have submoduled, but I wanted minimal assets) from here https://github.com/google/play-apk-expansion

Unity will now compile `.java` into jar/aar files automatically, from ANY folder. So you can place this submodule/package anywhere you like.

This should load from any `.jar`,`.obb`,`.apk`,`.zip` which (I think) has zero compression (an `.apk` is not compressed)


Why not?
=================
Currently for me, the bytes coming out are all zeroes, but is [a] correct length. Bit of a show stopper.
