namespace Xilium.CefGlue

open System
open System.IO
open System.Diagnostics
open System.Net
open System.Collections.Generic

open Xilium.CefGlue


open ICSharpCode.SharpZipLib.Core;
open ICSharpCode.SharpZipLib;
open ICSharpCode.SharpZipLib.Tar;
open ICSharpCode.SharpZipLib.BZip2

module ChromiumUtilities =
    let private sha1 = System.Security.Cryptography.SHA1.Create()
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
        try
            use fs = new FileStream(tarbz2, FileMode.Open, FileAccess.Read)
            use fsOut = File.Create(tar)
            BZip2.Decompress(fs,fsOut,true)
            printfn " done"
            tar
        with _ ->
            printfn " failed"
            failwithf "CEF unbz2 of %s failed" tarbz2

    let untar (sourceFile : string) (destFolder : string) = 
        printf " 0:   untar"
        try
            use s = File.OpenRead(sourceFile)
            use tar = TarArchive.CreateInputTarArchive(s)
            tar.ExtractContents(destFolder)
            printfn " done"
            tar.Close()
        with _ ->
            printfn " failed"
            failwithf "untar of %s failed" sourceFile


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
            
    let downloadString (url : string) =
        let request = HttpWebRequest.Create(url)
        
        use response = request.GetResponse()
        use reader = new StreamReader(response.GetResponseStream())
        reader.ReadToEnd()

    let computeHash (file : string) =
        use stream = File.OpenRead(file)
        sha1.ComputeHash(stream) |> Array.map (sprintf "%02x") |> String.concat ""


    let downloadCefTo (url : string) (unpackDir : string) = 
        let urlString = System.Web.HttpUtility.UrlDecode(url)
        let tarbz = Path.GetFileName(urlString)
        let tar = Path.GetFileNameWithoutExtension tarbz
        let plainDirName = Path.GetFileNameWithoutExtension tar
        let tempDir = Path.GetTempPath()

        let downloadFile = Path.Combine [| tempDir; tarbz |]

        let downloadValid() =
            try
                let eHash = downloadString(url + ".sha1").ToLower()
                let rHash = computeHash downloadFile
                if rHash <> eHash then
                    Choice2Of2 (eHash, rHash)
                else
                    Choice1Of2 true
            with _ ->
                Choice1Of2 false

        let download () =
            use wc = new WebClient()
            Console.Write(" 0:   download: 0%")
            let uri = Uri(url)
            wc.DownloadProgressChanged.Add(fun a -> 
                Console.Write("\r 0:   download: {0}%", a.ProgressPercentage);
            )
            wc.DownloadFileTaskAsync(uri,downloadFile).Wait()

            match downloadValid() with
                | Choice1Of2 true -> 
                    Console.WriteLine("\r 0:   downloaded          ")
                | Choice1Of2 false ->
                    let c = Console.ForegroundColor
                    Console.ForegroundColor <- ConsoleColor.DarkYellow
                    Console.WriteLine("\r 0:   could not validate hash")
                    Console.ForegroundColor <- c
                | Choice2Of2(eHash, rHash) -> 
                    Console.WriteLine("\r 0:   invalid hash: {0}", rHash)
                    failwithf "CEF invalid hash: { expected: %s; real: %s }" eHash rHash

        let downloadExists = 
            let info = FileInfo(downloadFile)

            if info.Exists then 
                match downloadValid() with
                    | Choice2Of2(eHash, rHash) ->
                        info.Delete()
                        false
                    | _ ->
                        true
            else
                false



        if downloadExists then
            printfn " 0:   skipping download"
        else 
            download()

        
        let targetDir = Path.Combine [| tempDir; plainDirName |]
        let release   = Path.Combine [| targetDir;  "Release" |]
        let resources = Path.Combine [| targetDir;  "Resources" |]


        let tarFile = unbz2 downloadFile
        untar tarFile tempDir
        
        printfn " 0:   copy binaries"
        try
            if not (Directory.Exists unpackDir) then Directory.CreateDirectory unpackDir |> ignore
        with _ ->
            printfn " 0:   could not create directory"
            failwithf "CEF could not create directory: %s" unpackDir

        try
            copyDir release unpackDir
            copyDir resources unpackDir
        with _ ->
            printfn " 0:   could not copy resources"
            Directory.Delete unpackDir
            failwithf "CEF could not copy resources to: %s" unpackDir
            
        try
            File.Delete tarFile
            File.Delete downloadFile
            Directory.Delete(targetDir,true)
        with _ ->
            ()

    let unpackDependenciesTo (fixupLoadPath : bool) (name : string) (workingDir : string) (id,version) (deps : seq<KeyValuePair<string*int,string>>) =
        
        if fixupLoadPath then
            CefRuntime.LoadPath <- workingDir
        
        // need more version cache files for 
        let id =
            match System.Environment.OSVersion.Platform with
                | System.PlatformID.Unix -> sprintf "%s_unix" id
                | System.PlatformID.MacOSX -> sprintf "%s_mac" id
                | _ -> id 


        let c = Console.ForegroundColor

        // todo more sophisticated check.... (on linux this is not clear yet)
        if Directory.Exists workingDir && Directory.GetFiles(workingDir).Length > 0 then
            printfn " 0: skipping CEF installation of version %s (found in %%AppData%%\\%s)" version name
        else
            //Directory.CreateDirectory workingDir |> ignore
            Console.ForegroundColor <- ConsoleColor.Green
            printfn " 0: installing CEF version %s in %%AppData%%\\%s" version name

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

    let unpackDependencies (id,version) (deps : seq<KeyValuePair<string*int,string>>) =
        let name = sprintf "%s_%s" id version
        let appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        let workingDir = Path.Combine(appDataPath, name)
        unpackDependenciesTo true name workingDir (id,version) deps

    let id = "cef"
    let version = "87.1.1+g9a70877+chromium-87.0.4280.27"

    [<CompiledName("UnpackCef")>]
    let unpackCef () =
        unpackDependencies (id,version) Xilium.CefGlue.NativeDependencies.NativeDependencyPaths

