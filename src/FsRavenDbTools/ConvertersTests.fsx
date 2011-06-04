#I @"..\Packages\Newtonsoft.Json.4.0.2\lib\net40"
#r "Newtonsoft.Json.dll"
#load "Converters.fs"

open System.IO
open Newtonsoft.Json
open Strangelights.FsRavenDbTools


let serializer = new JsonSerializer()
serializer.Converters.Add(new OptionTypeConverter())
serializer.Converters.Add(new UnionTypeConverter())

let serializeAndDisplay x =
    let writer = new StringWriter()
    serializer.Serialize(writer, x)
    writer.ToString()

let roundTrip x =
    let memStream = new MemoryStream()
    use writer = new StreamWriter(memStream)
    serializer.Serialize(writer, x)
    writer.Flush()
    memStream.Position <- 0L
    serializer.Deserialize(new StreamReader(memStream), x.GetType())

let displayThenRoundTrip x =
    printfn "%s" (serializeAndDisplay x)
    roundTrip x

type F = { Field: string }

type ABCD =
    | A
    | B of int
    | C of string
    | D of int* string
    | E of ABCD
    | F of F



displayThenRoundTrip (Some 1)

displayThenRoundTrip (A)
displayThenRoundTrip (B 1)
displayThenRoundTrip (C "hello")
displayThenRoundTrip (D(1, "hello"))
displayThenRoundTrip (E(A))
displayThenRoundTrip (F {Field = "hello"})

// mainly for debug
//serializeAndDisplay (1, 1)

