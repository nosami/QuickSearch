namespace QuickSearch

open System
open Mono.Addins
open Mono.Addins.Description

[<assembly:Addin("QuickSearch", Namespace = "QuickSearch", Version = "1.0.3")>]
[<assembly:AddinName("QuickSearch")>]
[<assembly:AddinCategory("IDE extensions")>]
[<assembly:AddinDescription("QuickSearch harnesses the indexing power of OSX's Spotlight to guarantee lightning fast text search in your solution.\nHighlight some text and press alt-f to get started.")>]
[<assembly:AddinUrl("https://github.com/nosami/QuickSearch")>]
[<assembly:AddinAuthor("jason")>]
()

 