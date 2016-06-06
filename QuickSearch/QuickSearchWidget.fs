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

module Option =
    let inline tryCast<'T> (o: obj): 'T option =
        match o with
        | null -> None
        | :? 'T as a -> Some a
        | _ -> None

type QuickSearchWidget() as this =
    inherit HBox()

    let buttonStop:ToolButton = null

    let buttonPin:ToggleToolButton = null

    let SearchResultColumn = 0;

    let mutable resultCount = 0

    let searchEntry = new Entry()
    do
        let vbox = new VBox ()

        let toolbar = new Toolbar 
                          (
                            Orientation = Orientation.Vertical,
                            IconSize = IconSize.Menu,
                            ToolbarStyle = ToolbarStyle.Icons
                          )

        this.PackStart (vbox, true, true, 0u)
        this.PackStart (toolbar, false, false, 0u)
        let labelStatus = new Label (Xalign = 0.0f, Justify = Justification.Left)
        let hpaned = new HPaned ()

        let store = new ListStore(typedefof<obj>)

        let search _e =
            Runtime.RunInMainThread(fun() -> store.Clear()) |> ignore
            resultCount <- 0
            labelStatus.Text <- "0 results"
            QuickSearch.search searchEntry.Text

        let reportResult (result:QuickSearchResult) =
            store.AppendValues([|result|]) |> ignore
            resultCount <- resultCount + 1
            labelStatus.Text <- if resultCount = 1 then
                                    "1 result"
                                else
                                    sprintf "%d results" resultCount

        searchEntry.Changed
        |> FSharp.Control.Reactive.Observable.throttle (TimeSpan.FromMilliseconds 350.)
        |> Observable.subscribe search |> ignore

        QuickSearch.resultReceived.Subscribe(
            fun res -> 
                if res.term = searchEntry.Text then 
                    Runtime.RunInMainThread(fun() -> reportResult res) |> ignore) |> ignore

        searchEntry.GrabFocus()
        vbox.PackStart (searchEntry, false, false, 0u)
        vbox.PackStart (hpaned, true, true, 0u)
        vbox.PackStart (labelStatus, false, false, 0u)
        let resultsScroll = new CompactScrolledWindow ()
        hpaned.Pack1 (resultsScroll, true, true)

        let treeviewSearchResults = new PadTreeView (Model = store, HeadersClickable = true)

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
        textColumn.FixedWidth <- 500

        let pathColumn = treeviewSearchResults.AppendColumn (GettextCatalog.GetString ("Path"), renderer, renderPath)
        pathColumn.SortColumnId <- 3
        pathColumn.Resizable <- true
        pathColumn.Sizing <- TreeViewColumnSizing.Fixed
        pathColumn.FixedWidth <- 500

        this.ShowAll ()
        
        treeviewSearchResults.FixedHeightMode <- true

        let openSelectedMatches _e =
            for path in treeviewSearchResults.Selection.GetSelectedRows() do
                let iter : TreeIter ref = ref Unchecked.defaultof<_>
                let res = store.GetIter(iter, path)
                if res then
                    let result = store.GetValue (!iter, SearchResultColumn) :?> QuickSearchResult
                    IdeApp.Workbench.OpenDocument (result.filePath, null, result.line, 0) |> ignore

        treeviewSearchResults.RowActivated.Add openSelectedMatches
    member x.SearchEntry = searchEntry

type QuickSearchPad() =
    inherit MonoDevelop.Ide.Gui.PadContent()
    let view = new QuickSearchWidget()
    member x.SearchEntry = view.SearchEntry
    override x.Control = Control.op_Implicit view

type QuickSearchHandler() =
    inherit CommandHandler()

    [<CommandHandler("QuickSearch.QuickSearch")>]
    override x.Run() = 
        let guipad = IdeApp.Workbench.GetPad<QuickSearchPad>()
        guipad.BringToFront()
        let quicksearchPad = guipad.Content :?> QuickSearchPad
        quicksearchPad.SearchEntry.GrabFocus()
