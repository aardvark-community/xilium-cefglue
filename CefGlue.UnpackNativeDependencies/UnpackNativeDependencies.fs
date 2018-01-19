namespace Xilium.CefGlue

open System
open System.IO
open System.Diagnostics
open System.Net
open System.Collections.Generic

open Xilium.CefGlue.Wrapper
open Xilium.CefGlue


open ICSharpCode.SharpZipLib.Core;
open ICSharpCode.SharpZipLib;
open ICSharpCode.SharpZipLib.Tar;
open ICSharpCode.SharpZipLib.BZip2

module ChromiumUtilities =

    let getCurrentArch () =
        let arch = if IntPtr.Size = 8 then 64 else 32
        let plat = 
            match Environment.OSVersion.Platform with
                | PlatformID.MacOSX -> "mac"
                | PlatformID.Unix -> "linux"
                | _ -> "windows"
        plat, arch
        
    let unbz2 (tarbz2 : string)  = 
        let tar = Path.Combine [| Path.GetDirectoryName tarbz2; Path.GetFileNameWithoutExtension tarbz2 |]
        printf " 0:   unbz2"
        use fs = new FileStream(tarbz2, FileMode.Open, FileAccess.Read)
        use fsOut = File.Create(tar)
        BZip2.Decompress(fs,fsOut,true)
        printfn " done"
        tar

    let untar (sourceFile : string) (destFolder : string) = 
        printf " 0:   untar"
        use s = File.OpenRead(sourceFile)
        use tar = TarArchive.CreateInputTarArchive(s)
        tar.ExtractContents(destFolder)
        printfn " done"
        tar.Close()


    let rec copyDirInfo (srcInfo : DirectoryInfo) (dstInfo : DirectoryInfo) =
        for src in srcInfo.GetDirectories("*", SearchOption.TopDirectoryOnly) do
            let dst = dstInfo.CreateSubdirectory(src.Name)
            copyDirInfo src dst

        for src in srcInfo.GetFiles("*", SearchOption.TopDirectoryOnly) do
            let dst = Path.Combine(dstInfo.FullName, src.Name)
            src.CopyTo(dst) |> ignore

    let copyDir (src : string) (dst : string) =
        let src = DirectoryInfo src
        let dst = DirectoryInfo dst

        if not src.Exists then
            failwith "cannot copy non-existant directory"
        else
            if not dst.Exists then dst.Create()
            copyDirInfo src dst



    let downloadCefTo (url : string) (unpackDir : string) = 
        let tarbz = Path.GetFileName(url)
        let tar = Path.GetFileNameWithoutExtension tarbz
        let plainDirName = Path.GetFileNameWithoutExtension tar
        let tempDir = Path.GetTempPath()

        let downloadFile = Path.Combine [| tempDir; tarbz |]

        let download () =
            use wc = new WebClient()
            Console.Write(" 0:   download: 0%")
            let uri = Uri(url)
            wc.DownloadProgressChanged.Add(fun a -> 
                Console.Write("\r 0:   download: {0}%", a.ProgressPercentage);
            )
            wc.DownloadFileTaskAsync(uri,downloadFile).Wait()
            Console.WriteLine("\r 0:   downloaded          ");

        let downloadExists = 
            if File.Exists downloadFile then 
                let info = FileInfo(downloadFile)

                let wc = HttpWebRequest.Create url
                let fileSize = wc.GetResponse().ContentLength

                if info.Length <> fileSize then
                    info.Delete()
                    false
                else
                    true
            else
                false

        if downloadExists then
            printfn " 0:   skipping download"
        else 
            download()
        
        
        let maxTrials = 2
        let rec doIt (remainingTrials : int) =
            if remainingTrials <= 0 then 
                failwith "CEF out of trials for download. go to https://www.spotify.com/at/opensource/, download the correct version and report failure of CefGlue.UnpackNativeDependencies."
            else
                try
                    let trial = maxTrials - remainingTrials
                    if trial > 0 then
                        printfn " 0:   installing cef (trial: %d)" trial
                    let targetDir = Path.Combine [| tempDir; plainDirName |]
                    let tarFile = unbz2 downloadFile
                    untar tarFile tempDir
                    let release   = Path.Combine [| targetDir;  "Release" |]
                    let resources = Path.Combine [| targetDir;  "Resources" |]

                    if not (Directory.Exists unpackDir) then
                        Directory.CreateDirectory unpackDir |> ignore

                    printfn " 0:   copy binaries"
                    copyDir release unpackDir
                    copyDir resources unpackDir

                    File.Delete tarFile
                    File.Delete downloadFile
                    Directory.Delete(targetDir,true)
                with e -> 
                    let c = Console.ForegroundColor
                    Console.ForegroundColor <- ConsoleColor.Red
                    printfn " 0:   install failed with %A in %A" e.Message e.StackTrace
                    Console.ForegroundColor <- c
                    download()
                    doIt (remainingTrials - 1)
        doIt 2

    let unpackDependencies (id,version) (deps : seq<KeyValuePair<string*int,string>>) =
        let name = sprintf "%s_%s" id version
        let workingDir = Path.Combine(Path.GetTempPath(), name)

        CefRuntime.LoadPath <- workingDir
        // need more version cache files for 
        let id =
            match System.Environment.OSVersion.Platform with
                | System.PlatformID.Unix -> sprintf "%s_unix" id
                | System.PlatformID.MacOSX -> sprintf "%s_mac" id
                | _ -> id 


        let c = Console.ForegroundColor

        if Directory.Exists workingDir then
            printfn " 0: skipping CEF installation of version %s (found in $TEMP\\%s)" version name
        else
            //Directory.CreateDirectory workingDir |> ignore
            Console.ForegroundColor <- ConsoleColor.Green
            printfn " 0: installing CEF version %s in $TEMP\\%s" version name

            let currentArch = getCurrentArch ()
            match deps |> Seq.tryFind (fun (KeyValue(arch, url)) -> arch = currentArch) with
                | Some (KeyValue((os,arch),url)) ->
                    printfn " 0:   os:   %s" os
                    printfn " 0:   arch: %s" (if arch = 32 then "x86" else "x64")
                    printfn " 0:   url:  %s" url

                    downloadCefTo url workingDir
                | None -> 
                    failwithf "no native dependency for current platform: %A, candidates are: %A" currentArch (deps |> Seq.toList |> List.map (fun (KeyValue(k,v)) -> k,v))
        

        Console.ForegroundColor <- c

    [<CompiledName("UnpackCef")>]
    let unpackCef () =
        unpackDependencies ("cef", "3.2883.1539") Xilium.CefGlue.NativeDependencies.NativeDependencyPaths

