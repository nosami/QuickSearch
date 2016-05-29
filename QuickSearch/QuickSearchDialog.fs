namespace QuickSearch

open System
open System.Diagnostics
open System.Text.RegularExpressions
open System.Threading
open Gtk
open MonoDevelop.Components
open MonoDevelop.Components.Commands
open MonoDevelop.Ide
open MonoDevelop.Ide.FindInFiles

module QuickSearch =
    let isNotNull x = not (isNull x)
    let mutable token = new CancellationTokenSource()

    let addResult (monitor:SearchProgressMonitor, text) =
        if isNotNull text then
            //MonoDevelop.Core.LoggingService.LogDebug text
            let m = Regex.Match(text, "(?<filename>[^:]*):(?<offset>\d+):(?<text>[^:]*)", RegexOptions.Compiled)
            if m.Success then
                let offset = m.Groups.["offset"].Value |> int
                let result = SearchResult(FileProvider(m.Groups.["filename"].Value), offset, text.Length)

                monitor.ReportResult result
        //()//monitor.
    let search term =
        token.Cancel()
        token <- new CancellationTokenSource()

        
        let computation =
            async {
                //monitor.PathMode <- FindInFiles.PathMode.
                let fsiProcess =
                    let startInfo =
                        new ProcessStartInfo
                            //(FileName = "/bin/bash", UseShellExecute = false, Arguments = "--login -i",
                            //(FileName = "/usr/bin/ssh", UseShellExecute = false, Arguments = "-t -t localhost bash --login -i",
                            (FileName = "/bin/bash", UseShellExecute = false, Arguments = sprintf "-c 'mdfind -onlyin /Users/jason/src/monodevelop %s | head -1000 | xargs grep -i --byte-offset %s'" term term,
                            //(FileName = "/usr/bin/script", UseShellExecute = false, Arguments = "bash",
                            RedirectStandardError = true, CreateNoWindow = true, RedirectStandardOutput = true,
                            RedirectStandardInput = true, StandardErrorEncoding = Text.Encoding.UTF8, StandardOutputEncoding = Text.Encoding.UTF8)

                    try
                        Process.Start(startInfo)
                    with e ->
                        MonoDevelop.Core.LoggingService.LogDebug (sprintf "Interactive: Error %s" (e.ToString()))
                        reraise()
                    //ssh.Connect()
                    //stream <- ssh.CreateShellStream("xterm", uint32 display.Size.X, uint32 display.Size.Y, 0u, 0u, 0)
                Async.OnCancel (fun() -> fsiProcess.Kill()) |> ignore                
                fsiProcess.EnableRaisingEvents <- true
                use monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true, token)

                fsiProcess.OutputDataReceived
                |> FSharp.Control.Reactive.Observable.take 1000
                |> Observable.subscribe(fun x -> if isNotNull x then addResult(monitor, x.Data)) |> ignore
                //fsiProcess.ErrorDataReceived.Subscribe(fun x -> if not (isNull x) then MonoDevelop.Core.LoggingService.LogDebug ("err" + x.Data)) |> ignore
                fsiProcess.BeginOutputReadLine()
                //fsiProcess.BeginErrorReadLine()
                fsiProcess.WaitForExit()
                monitor.EndTask()
            }
        Async.Start(computation, token.Token)
        //let outstream = fsiProcess.StandardOutput.BaseStream
    //let output = Observable.merge fsiProcess.OutputDataReceived fsiProcess.ErrorDataReceived
        
    //let outputFromServer : IObservable<byte[]> =
    //    Observable. (fun (args: DataReceivedEventArgs) -> 
    //    let x = System.Text.Encoding.UTF8.GetBytes (args.Data + "\r\n")
    //    MonoDevelop.Core.LoggingService.LogDebug (args.Data)
    //    x
    //    ) output

type QuickSearchDialog() as this =
    inherit Gtk.Dialog()
    let entry = new Gtk.Entry()

    do
        let box = new Gtk.VBox()
        this.ActionArea.PackStart entry
        //this.Decorated <- false
        this.WidthRequest <- 300
        //this.Add box

        entry.Changed
        |> FSharp.Control.Reactive.Observable.throttle (TimeSpan.FromMilliseconds 300.)
        |> Observable.subscribe(fun _ -> QuickSearch.search entry.Text) |> ignore

        this.ShowAll()

    static let dialog = Lazy.Create(fun() -> new QuickSearchDialog()).Value

    static member ShowSearch() =
        MonoDevelop.Core.LoggingService.LogDebug("************************")
        let window = (Window.op_Implicit dialog).GetNativeWidget<Gtk.Window>()
        MessageService.PlaceDialog(Window.op_Implicit window, null)
        dialog.Present()

type QuickSearchHandler() =
    inherit CommandHandler()

    [<CommandHandler("QuickSearch.QuickSearch")>]
    override x.Run() = QuickSearchDialog.ShowSearch()