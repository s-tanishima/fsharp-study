namespace ViewModels

open System
open System.Windows
open System.IO
open FSharp.ViewModule
open FSharp.ViewModule.Validation
open FsXaml
open Microsoft.WindowsAPICodePack.Dialogs

type MainView = XAML<"MainWindow.xaml", true>

type MainViewModel() as self = 
    inherit ViewModelBase()

    // 宣言の闇 part1
    let text = self.Factory.Backing(<@ self.SourceText @>, "")
    let textBox = self.Factory.Backing(<@ self.BoxText @>, "")
    
    // 宣言の闇 part2
    member this.BoxText with get() = textBox.Value and set(value) = textBox.Value <- value
    member this.SourceText with get() = text.Value and set(value) = text.Value <- value

    // 指定パスからのサブディレクトリ、ファイルを出力させる
    member this.ExecuteButtonClicked = self.Factory.CommandSync(fun () -> 
        match String.IsNullOrEmpty text.Value with
        | true -> ()
        | _ ->
            // 初期化
            textBox.Value <- ""

            // ファイルの一覧を取得します
            let searchFile (fileSystemInfos: FileSystemInfo[]) = 
                fileSystemInfos
                |> Array.filter (fun x -> x :? FileInfo)
                |> Array.map    (fun x -> 
                // 出力の埋め込み
                let file = x :?> FileInfo
                let path = ((file.Length |> string) + "\t\t" + file.FullName + "\r\n")
                textBox.Value <- textBox.Value + path
                file)

            // ディレクトリーの一覧を取得します
            let searchDirectory (fileSystemInfos: FileSystemInfo[]) = 
                fileSystemInfos
                |> Array.filter (fun x -> x :? DirectoryInfo)
                |> Array.map    (fun x -> x :?> DirectoryInfo)
            
            // 【再帰】ディレクトリー内のサイズをサマリーする
            let rec searchAll (directory: DirectoryInfo) =
                let systemInfos = directory.GetFileSystemInfos()
                let summaryFileSize         = searchFile        systemInfos |> Array.sumBy (fun x -> x.Length)
                let summaryDirectorySize    = searchDirectory   systemInfos |> Array.sumBy (fun x -> searchAll x)
                let summarySize = summaryFileSize + summaryDirectorySize

                // 出力の埋め込み
                let sizeStr = summarySize |> string
                let path = (sizeStr + "\t\t" + directory.FullName + "\r\n")
                textBox.Value <- textBox.Value + path
                summarySize

            let directory = new DirectoryInfo(text.Value)
            let totalSize = searchAll directory |> string
            textBox.Value <- textBox.Value + totalSize
        )

    // フォルダ指定ウィンドウの表示
    member this.BrowseButtonClicked = self.Factory.CommandSync(fun () -> 
        let dlg = new CommonOpenFileDialog()
        dlg.Title <- "フォルダの選択"
        dlg.IsFolderPicker <- true
        dlg.InitialDirectory <- text.Value

        dlg.AddToMostRecentlyUsedList <- false
        dlg.AllowNonFileSystemItems <- false
        dlg.DefaultDirectory <- text.Value
        dlg.EnsureFileExists <- true
        dlg.EnsurePathExists <- true
        dlg.EnsureReadOnly <- false
        dlg.EnsureValidNames <- true

        dlg.Multiselect <- false
        dlg.ShowPlacesList <- true

        if dlg.ShowDialog() = CommonFileDialogResult.Ok then
            text.Value <- dlg.FileName
        )
        
    //member this.ExecuteButtonClicked = self.Factory.CommandSync(fun () -> )