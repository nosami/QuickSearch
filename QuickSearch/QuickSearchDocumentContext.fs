namespace QuickSearch
open System.Threading.Tasks
open MonoDevelop.Ide.Editor
open MonoDevelop.Ide.TypeSystem

type QuickSearchDocumentContext(filename) =
    inherit DocumentContext()

    override x.ParsedDocument = null
    override x.AttachToProject(_) = ()
    override x.ReparseDocument() = ()
    override x.GetOptionSet() = TypeSystemService.Workspace.Options
    override x.Project = null
    override x.Name = filename
    override x.AnalysisDocument with get() = null
    override x.UpdateParseDocument() = Task.FromResult null

















