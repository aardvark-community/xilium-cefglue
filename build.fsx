#load @"paket-files/build/vrvis/Aardvark.Fake/DefaultSetup.fsx"
#r "./packages/build/SharpZipLib/lib/20/ICSharpCode.SharpZipLib.dll"

open Fake
open System
open System.IO
open System.Diagnostics
open System.Net

open Aardvark.Fake

open ICSharpCode.SharpZipLib.Core;
open ICSharpCode.SharpZipLib;
open ICSharpCode.SharpZipLib.Tar;
open ICSharpCode.SharpZipLib.BZip2

type CefBuild = {
    linux32 : string
    linux64 : string
    mac64 : string
    win32 : string
    win64 : string
}

let cefBuild =
    {
        linux32 = "http://opensource.spotify.com/cefbuilds/cef_binary_3.2883.1539.gd7f087e_linux32_minimal.tar.bz2"
        linux64 = "http://opensource.spotify.com/cefbuilds/cef_binary_3.2883.1539.gd7f087e_linux64_minimal.tar.bz2"
        mac64 = "http://opensource.spotify.com/cefbuilds/cef_binary_3.2883.1539.gd7f087e_macosx64_minimal.tar.bz2"
        win32 = "http://opensource.spotify.com/cefbuilds/cef_binary_3.2883.1539.gd7f087e_windows32_minimal.tar.bz2"
        win64 = "http://opensource.spotify.com/cefbuilds/cef_binary_3.2883.1539.gd7f087e_windows64_minimal.tar.bz2"
    }


do Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

DefaultSetup.install ["Xilium.CefGlue.WithoutGtk.sln"]

let unbz2 (sourceFile : string)  = 
    let targetFile = Path.ChangeExtension(Path.GetFileNameWithoutExtension sourceFile,"tar")
    logfn "unbz2: %s to %s" sourceFile targetFile
    use fs = new FileStream(sourceFile,FileMode.Open,FileAccess.Read)
    use fsOut = File.Create(targetFile)
    BZip2.Decompress(fs,fsOut,true)
    logfn "unbz2 done."
    targetFile

let untar (sourceFile : string) (destFolder : string) = 
    logfn "untar %s to %s" sourceFile destFolder
    use s = File.OpenRead(sourceFile)
    use tar = TarArchive.CreateInputTarArchive(s)
    tar.ExtractContents(destFolder)
    logfn "untar done."
    tar.Close()


let downloadCefTo (url : string) (unpackDir : string) = 
    let file = Path.GetFileName(url)
    let plainFileName = Path.GetFileNameWithoutExtension file
    let dir = Path.GetTempPath()
    let targetDir = Path.Combine [| dir; plainFileName |]
    let downloadFile = Path.Combine [|dir;file|]
    use wc = new WebClient()
    logfn "downloading: %s to %s" url downloadFile
    wc.DownloadFile(url,downloadFile)
    let tarFile = unbz2 downloadFile
    untar tarFile targetDir
    let release = Path.Combine [| targetDir; Path.GetFileNameWithoutExtension(plainFileName); "Release" |]
    let resources = Path.Combine [| targetDir; Path.GetFileNameWithoutExtension(plainFileName); "Resources" |]
    logfn "copy %s to %s" release unpackDir
    Fake.FileHelper.CopyDir unpackDir release (fun _ -> true)
    Fake.FileHelper.CopyDir unpackDir resources (fun _ -> true)
    File.Delete(downloadFile)
    Directory.Delete(targetDir,true)
    logfn "downloadCefTo %s to %s done." url downloadFile

Target "DownloadCefBuild" (fun _ -> 
    downloadCefTo cefBuild.win64 "./lib/Native/Xilium.CefGlue/windows/AMD64"
    //downloadCefTo cefBuild.linux32 "./lib/Native/Xilium.CefGlue/linux/x86"
    //downloadCefTo cefBuild.linux64 "./lib/Native/Xilium.CefGlue/linux/AMD64"
    //downloadCefTo cefBuild.mac64 "./lib/Native/Xilium.CefGlue/mac/amd64"
    //downloadCefTo cefBuild.win32 "./lib/Native/Xilium.CefGlue/windows/x86"
)

#if DEBUG
do System.Diagnostics.Debugger.Launch() |> ignore
#endif


entry()