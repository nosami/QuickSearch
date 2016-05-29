namespace QuickSearch

open Gtk
open MonoDevelop.Components
open MonoDevelop.Core
open MonoDevelop.Ide.FindInFiles
open MonoDevelop.Ide.Gui.Components

type QuickSearchWidget() as this =
    inherit HBox()

    let mutable store:ListStore = null
    
    let buttonStop:ToolButton = null

    let buttonPin:ToggleToolButton = null
    
    let SearchResultColumn = 0;
    let DidReadColumn      = 1;
    
    //ColorScheme highlightStyle;
    
    let mutable scrolledwindowLogView:ScrolledWindow = null
    let mutable treeviewSearchResults:PadTreeView = null
    let mutable labelStatus:Label = null
    //TextView textviewLog;
    //TreeViewColumn patrchResult>
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
        labelStatus <- new Label (Xalign = 0.0f, Justify = Justification.Left)
        let hpaned = new HPaned ()
        vbox.PackStart (hpaned, true, true, 0u)
        vbox.PackStart (labelStatus, false, false, 0u)
        let resultsScroll = new CompactScrolledWindow ()
        hpaned.Pack1 (resultsScroll, true, true)
        //scrolledwindowLogView <- new CompactScrolledWindow ()
        //hpaned.Pack2 (scrolledwindowLogView, true, true)
        //textviewLog <- new TextView (Editable <- false)
        //scrolledwindowLogVi        //let x = typedefof <SearchResult>ew.Add (t//extviewLog)

        let store = new ListStore(typedefof<SearchResult>, typedefof<bool>) // didRe//ad)
        
        treeviewSearchResults <- new PadTreeView (Model = store, HeadersClickable = true)// Rule//sHint <- true)

        treeviewSearchResults.Selection.Mode <- Gtk.SelectionMode.Multiple
        resultsScroll.Add (treeviewSearchResults)

        let projectColumn = new TreeViewColumn (
                                Resizable = true,
                                SortColumnId = 1,
                                Title = GettextCatalog.GetString ("Project"),
                                Sizing = TreeViewColumnSizing.Fixed,
                                FixedWidth = 100
                             )

        let projectPixbufRenderer = new CellRendererImage ()
        projectColumn.PackStart (projectPixbufRenderer, false)
        projectColumn.SetCellDataFunc (projectPixbufRenderer, ResultProjectIconDataFunc)

        let renderer = treeviewSearchResults.TextRenderer
        renderer.Ellipsize <- Pango.EllipsizeMode.End
        projectColumn.PackStart (renderer, true)
        projectColumn.SetCellDataFunc (renderer, ResultProjectDataFunc)
        treeviewSearchResults.AppendColumn (projectColumn)

        let fileNameColumn = new TreeViewColumn 
                                (
                                    Resizable = true,
                                    SortColumnId = 2,
                                    Title = GettextCatalog.GetString ("File"),
                                    Sizing = TreeViewColumnSizing.Fixed,
                                    FixedWidth = 200
                                )

        let fileNamePixbufRenderer = new CellRendererImage ()
        fileNameColumn.PackStart (fileNamePixbufRenderer, false)
        fileNameColumn.SetCellDataFunc (fileNamePixbufRenderer, FileIconDataFunc)
        
        fileNameColumn.PackStart (renderer, true)
        fileNameColumn.SetCellDataFunc (renderer, FileNameDataFunc)
        treeviewSearchResults.AppendColumn (fileNameColumn)


        let textColumn = treeviewSearchResults.AppendColumn (GettextCatalog.GetString ("Text"), renderer, ResultTextDataFunc)
        textColumn.Resizable <- true
        textColumn.Sizing <- TreeViewColumnSizing.Fixed
        textColumn.FixedWidth <- 300

        pathColumn <- treeviewSearchResults.AppendColumn (GettextCatalog.GetString ("Path"), renderer, ResultPathDataFunc)
        pathColumn.SortColumnId <- 3
        pathColumn.Resizable <- true
        pathColumn.Sizing <- TreeViewColumnSizing.Fixed
        pathColumn.FixedWidth <- 500

        ////store.DefaultSortFunc <- DefaultSortFunc
        //store.//SetSortFunc (1, CompareProjectFileNames)
        ////store.SetSortFunc (2, CompareFileNames)
        ////store.SetSortFunc (3, CompareFilePaths)

        //treeviewSearchResults.RowActivated +<- T//reeviewSearchResultsRowActivated
        
        //buttonStop <- new ToolButton (new ImageView (Gui.Stock.Stop, Gtk.IconSize.Menu), null, S//ensitive <- false)// { Sensitive <- false }
      //  //buttonStop.Clicked +<- ButtonStopClicked
        //buttonStop.Too//ltipText <- GettextCatalog.GetString ("St//op")
        //toolbar.Insert (buttonStop, -1)

        //let buttonClear <- new ToolButton (new ImageVi//ew (Gui.Stock.Clear, Gtk.IconSize.Menu), null)
     //   //buttonClear.Clicked +<- ButtonClearClicked
        //buttonClear.TooltipTex//t <- GettextCatalog.GetString ("Clear results")
  //      toolbar.Insert (buttonClear, -1)
        
   //     let buttonOutput <- new ToggleToolButton ()
        //buttonOutput.IconWidget <- new Ima//geView (Gui.Stock.OutputIcon, Gtk.IconSize.Menu)
     //   //buttonOutput.Clicked +<- ButtonOutputClicked
        //buttonOutput.TooltipText <- GettextCatalog.GetString ("Show output")
 //       toolbar.Insert (buttonOutput, -1)
      //  
        //buttonPin <- new ToggleToolButton ()
        //buttonPin.IconWidget <- n//ew ImageView (Gui.Stock.PinUp, Gtk.IconSize.Menu//)
        //buttonPin.Clicked +<- ButtonPinClicked
        //buttonPin.TooltipT//ext <- GettextCatalog.GetString ("Pin resu//lts pad")
        //toolbar.Insert (buttonPin, -1)

     //   // store.SetSortColumnId (3, S//ortType.Ascending)
        this.ShowAll ()
        
        scrolledwindowLogView.Hide ()
   //     treeviewSearchResults//.FixedHeightMode <- true

        //UpdateStyles ()
        //IdeApp.Preferences.ColorScheme.Changed +<- UpdateStyles