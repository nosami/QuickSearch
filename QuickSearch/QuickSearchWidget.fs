namespace QuickSearch

open System
open System.Threading
open Gtk
open MonoDevelop.Components
open MonoDevelop.Components.Commands
open MonoDevelop.Ide
open MonoDevelop.Core
open MonoDevelop.Ide.FindInFiles
open MonoDevelop.Ide.Gui.Components
open MonoDevelop.Ide.Editor

[<AutoOpen>]
module Nullables =
    type NullBuilder() =
        member x.Return(v) = v
        member x.ReturnFrom(v) = v
        member x.Bind(v, f) = if (box v) = null then null else f v
    let nullable = NullBuilder()

type QuickSearchWidget() as this =
    inherit HPaned()

    let buttonStop:ToolButton = null

    let buttonPin:ToggleToolButton = null

    let SearchResultColumn = 0;

    let mutable resultCount = 0

    let searchEntry = new Entry()
    let buttonPreview = new ToggleButton ()
    let store = new ListStore(typedefof<obj>)
    let labelStatus = new Label (Xalign = 0.0f, Justify = Justification.Left)
    let treeviewSearchResults = new PadTreeView (Model = store, HeadersClickable = true)
    let preview = TextEditorFactory.CreateNewEditor(TextEditorType.Default)

    do
        let hbox = new HBox ()
        let vbox = new VBox ()
        this.Pack1 (vbox, true, true)

        let previewControl = preview :> Control

        let hpaned = new HPaned ()

        searchEntry.GrabFocus()
        let entryRow = new HBox()
        entryRow.PackStart(searchEntry, true, true, 0u)
        let mutable previewLoaded = false
        buttonPreview.Label <- "Preview"
        buttonPreview.Active <- false
        buttonPreview.Image <- new ImageView (MonoDevelop.Ide.Gui.Stock.SearchboxSearch.ToString(), IconSize.Menu)
        buttonPreview.Image.Show ()
        buttonPreview.Toggled.Add(fun _e -> 
            if not previewLoaded then
                previewLoaded <- true
                this.Pack2 (Control.op_Implicit preview, true, true)
            this.Child2.Visible <- buttonPreview.Active)
        buttonPreview.TooltipText <- "Show results in preview window"
        entryRow.PackEnd(buttonPreview, false, false, 2u)
        vbox.PackStart (entryRow, false, false, 0u)
        vbox.PackStart (hpaned, true, true, 0u)
        vbox.PackStart (labelStatus, false, false, 0u)
        let resultsScroll = new CompactScrolledWindow ()
        hpaned.Pack1 (resultsScroll, true, true)

        treeviewSearchResults.Selection.Mode <- Gtk.SelectionMode.Multiple
        resultsScroll.Add (treeviewSearchResults)

        let isNotNull x = not (isNull x)

        let renderer (column: TreeViewColumn) (cell: CellRenderer) (model: TreeModel)  (iter: TreeIter) (f:QuickSearchResult -> string) =
            let column, cell, model, iter, store = column, cell, model, iter, store
            let renderer = cell :?> CellRendererText;
           
            let storeValue = model.GetValue (iter, SearchResultColumn)
            if isNotNull storeValue then
                let res = storeValue :?> QuickSearchResult 
                renderer.Text <- f res

        let renderFileName (column: TreeViewColumn) (cell: CellRenderer) (model: TreeModel)  (iter: TreeIter) =
            renderer column cell model iter
                (fun res -> sprintf "%s:%d" res.filePath.FileName res.line)

        let renderText (column: TreeViewColumn) (cell: CellRenderer) (model: TreeModel)  (iter: TreeIter) =
            renderer column cell model iter (fun res -> res.text)

        let renderPath (column: TreeViewColumn) (cell: CellRenderer) (model: TreeModel)  (iter: TreeIter) =
            renderer column cell model iter (fun res -> res.filePath.ParentDirectory |> string)

        let fileNameColumn = 
            new TreeViewColumn (Resizable = true, Title = GettextCatalog.GetString ("File"), Sizing = TreeViewColumnSizing.Fixed, FixedWidth = 200)

        let renderer = treeviewSearchResults.TextRenderer
        renderer.Ellipsize <- Pango.EllipsizeMode.End
        fileNameColumn.PackStart (renderer, true)
        fileNameColumn.SetCellDataFunc (renderer, renderFileName)
        treeviewSearchResults.AppendColumn (fileNameColumn) |> ignore

        let textColumn = treeviewSearchResults.AppendColumn (GettextCatalog.GetString ("Text"), renderer, renderText)
        textColumn.Resizable <- true
        textColumn.Sizing <- TreeViewColumnSizing.Fixed
        textColumn.FixedWidth <- 300

        let pathColumn = treeviewSearchResults.AppendColumn (GettextCatalog.GetString ("Path"), renderer, renderPath)
        pathColumn.SortColumnId <- 3
        pathColumn.Resizable <- true
        pathColumn.Sizing <- TreeViewColumnSizing.Fixed
        pathColumn.FixedWidth <- 300
        
        let openSelectedMatches _e =
            for path in treeviewSearchResults.Selection.GetSelectedRows() do
                let iter : TreeIter ref = ref Unchecked.defaultof<_>
                let res = store.GetIter(iter, path)
                if res then
                    let result = store.GetValue (!iter, SearchResultColumn) :?> QuickSearchResult
                    IdeApp.Workbench.OpenDocument (result.filePath, null, result.line, 0) |> ignore

        treeviewSearchResults.RowActivated.Add openSelectedMatches
        this.ShowAll ()

    let createEditor filename line =
        let doc = TextEditorFactory.LoadDocument(filename, DesktopService.GetMimeTypeForUri filename)
        let ctx = new QuickSearchDocumentContext(filename)
        let editor = TextEditorFactory.CreateNewEditor(ctx, doc, TextEditorType.Default)
        editor.IsReadOnly <- true
        editor

    let runInMainThread (f:unit -> unit) =
        Runtime.RunInMainThread f |> Async.AwaitTask |> Async.Start

    let search _e =
        Runtime.RunInMainThread(fun() -> store.Clear()) |> ignore
        resultCount <- 0
        labelStatus.Text <- "0 results"
        QuickSearch.search searchEntry.Text

    let reportResult (result:QuickSearchResult) =
        store.AppendValues(result) |> ignore
        resultCount <- resultCount + 1
        labelStatus.Text <- if resultCount = 1 then
                                "1 result"
                            else
                                sprintf "%d results" resultCount
    let previewChanged _args =
        if buttonPreview.Active then
            for path in treeviewSearchResults.Selection.GetSelectedRows() do
                let iter : TreeIter ref = ref Unchecked.defaultof<_>
                let res = store.GetIter(iter, path)
                if res then
                    let result = store.GetValue (!iter, SearchResultColumn) :?> QuickSearchResult

                    let editor = 
                        if preview.FileName = result.filePath then
                            preview
                        else
                            let previewContainer = this.Children |> Array.last
                            this.Remove previewContainer
                            let editor = createEditor (result.filePath.FullPath |> string) result.line
                            this.Pack2 (Control.op_Implicit editor, true, true)
                            editor
                    let loc = DocumentLocation(result.line, 1)
                    editor.SetCaretLocation(loc, true, true)

    let searchEntryDisposable =
        searchEntry.Changed
        |> FSharp.Control.Reactive.Observable.throttle (TimeSpan.FromMilliseconds 350.)
        |> Observable.subscribe search

    let resultReceivedDisposable =
        QuickSearch.resultReceived.Subscribe(
            fun res -> 
                if res.term = searchEntry.Text then 
                    runInMainThread(fun() -> reportResult res))


    let treeviewKeyDisposable = 
        treeviewSearchResults.KeyPressEvent.Subscribe
            (fun args -> LoggingService.LogDebug(sprintf "treeViewport %A" args.Event.Key)
                         if args.Event.Key = Gdk.Key.Up then
                             searchEntry.GrabFocus())

    let searchEntryKeyDisposable = 
        searchEntry.KeyPressEvent.Subscribe
            (fun args -> LoggingService.LogDebug(sprintf "entry %A" args.Event.Key)
                         if args.Event.Key = Gdk.Key.Up then
                             let editor =
                                 nullable {
                                     let! doc = IdeApp.Workbench.ActiveDocument
                                     return doc.Editor
                                 }
                             editor.GrabFocus())// |> Option.iter(fun ed -> ed.GrabFocus()))
                             //MonoDevelop.Ide.IdeApp.CommandService.DispatchCommand(Win);
                             //searchEntry.GrabFocus())

    let selectionChangedDisposable =
        treeviewSearchResults.Selection.Changed
        |> Observable.merge buttonPreview.Toggled
        |> Observable.filter(fun _args -> searchEntry.Text.Length > 1)
        |> FSharp.Control.Reactive.Observable.throttle (TimeSpan.FromMilliseconds 350.)
        |> Observable.subscribe(fun(_args) -> runInMainThread previewChanged)

    let disposables =
        FSharp.Control.Reactive.Disposables.compose [searchEntryDisposable; resultReceivedDisposable; selectionChangedDisposable; treeviewKeyDisposable] 

    member x.SearchEntry = searchEntry
    override x.Dispose() = disposables.Dispose()

type QuickSearchPad() =
    inherit MonoDevelop.Ide.Gui.PadContent()
    let view = new QuickSearchWidget()
    member x.SearchEntry = view.SearchEntry
    override x.Control = Control.op_Implicit view
    override x.Dispose() =
        view.Dispose()
        base.Dispose()

type QuickSearchHandler() =
    inherit CommandHandler()

    [<CommandHandler("QuickSearch.QuickSearch")>]
    override x.Run() =
        let guipad = IdeApp.Workbench.GetPad<QuickSearchPad>()
        guipad.BringToFront()
        let quicksearchPad = guipad.Content :?> QuickSearchPad
        quicksearchPad.SearchEntry.GrabFocus()

        let selectedText =
            nullable {
                let! doc = IdeApp.Workbench.ActiveDocument
                let! editor = doc.Editor
                return! editor.SelectedText
            }
        selectedText 
        |> Option.ofObj
        |> Option.iter (fun text -> quicksearchPad.SearchEntry.Text <- text)
