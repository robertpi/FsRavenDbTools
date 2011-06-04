namespace Strangelights.FsRavenDbTools
open Strangelights.FsRavenDbTools
open System
open Newtonsoft.Json
open Raven.Client.Document

module DocumentStoreExt =
    type Raven.Client.Document.DocumentStore with
        static member OpenInitializedStore(?url)=
            let url = match url with Some x -> x |  _ -> "http://localhost:8080"
            let store = new DocumentStore (Url = url)
            store.Initialize() |> ignore
            let addConverters (converters: JsonConverterCollection) =
                converters.Add(new OptionTypeConverter())
                converters.Add(new UnionTypeConverter())
            store.Conventions.CustomizeJsonSerializer <- new Action<JsonSerializer>(fun x -> addConverters x.Converters)
            store

module SessionExt =
    type Raven.Client.IDocumentSession with
        member x.StoreImmutable(entity) =
            x.Advanced.Evict entity
            x.Store entity
