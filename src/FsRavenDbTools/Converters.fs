namespace Strangelights.FsRavenDbTools

open System
open System.Reflection
open Newtonsoft.Json
open Microsoft.FSharp.Reflection

type OptionTypeConverter() =
    inherit JsonConverter()
    override x.CanConvert(typ:Type) =
        typ.IsGenericType && 
            typ.GetGenericTypeDefinition() = typedefof<option<OptionTypeConverter>>

    override x.WriteJson(writer: JsonWriter, value: obj, serializer: JsonSerializer) =
        if value <> null then
            let t = value.GetType()
            let fieldInfo = t.GetField("value", System.Reflection.BindingFlags.NonPublic ||| System.Reflection.BindingFlags.Instance)
            let value = fieldInfo.GetValue(value)
            serializer.Serialize(writer, value)

    override x.ReadJson(reader: JsonReader, objectType: Type, existingValue: obj, serializer: JsonSerializer) = 
        let cases = Microsoft.FSharp.Reflection.FSharpType.GetUnionCases(objectType)
        let value = serializer.Deserialize(reader, objectType.GetGenericArguments().[0]);
        if value <> null then
            FSharpValue.MakeUnion(cases.[1], [|value|])
        else
            FSharpValue.MakeUnion(cases.[0], [||])


type UnionTypeConverter() =
    inherit JsonConverter()
    override x.CanConvert(typ:Type) =
        FSharpType.IsUnion typ 

    override x.WriteJson(writer: JsonWriter, value: obj, serializer: JsonSerializer) =
        let t = value.GetType()
        let fieldInfo = t.GetField("_tag", System.Reflection.BindingFlags.NonPublic ||| System.Reflection.BindingFlags.Instance) 
        let tag = fieldInfo.GetValue(value) :?> int
        writer.WriteStartObject()
        writer.WritePropertyName("_tag")
        writer.WriteValue(tag)
        let cases = FSharpType.GetUnionCases(t)
        let case = cases.[tag]
        //printfn "case: %s" case.Name 
        let fields = case.GetFields()
        for field in fields do
            //printfn "%s %s" field.Name field.PropertyType.Name
            //printfn "%s %s" (value.GetType().FullName) field.DeclaringType.Name
            writer.WritePropertyName(field.Name)
            serializer.Serialize(writer, field.GetValue(value, [||]))
        writer.WriteEndObject()

    override x.ReadJson(reader: JsonReader, objectType: Type, existingValue: obj, serializer: JsonSerializer) =
        //printfn "%A" reader.Value
        reader.Read() |> ignore
        //printfn "%A" reader.Value
        reader.Read() |> ignore
        //printfn "%A" reader.Value
        let tag = reader.Value
        //printfn "tag: %A tag type: %s" tag (tag.GetType().FullName)
        let tag = int (tag :?> int64)
        let cases = FSharpType.GetUnionCases(objectType)
        let case = cases.[tag]
        let fieldValues =
            [| for field in case.GetFields() do
                // not sure why this is needed, should it say anything that isn't int/string/float
                if FSharpType.IsUnion field.PropertyType || FSharpType.IsRecord field.PropertyType then 
                    reader.Read() |> ignore
                    reader.Read() |> ignore
                yield serializer.Deserialize(reader, field.PropertyType) |]
        FSharpValue.MakeUnion(cases.[tag], fieldValues)


