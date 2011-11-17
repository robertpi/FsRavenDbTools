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
        let t = objectType.GetGenericArguments().[0]
        let t = 
            if t.IsValueType then 
                let nullable = typedefof<Nullable<int>> 
                nullable.MakeGenericType [|t|]
            else 
                t
        let value = serializer.Deserialize(reader, t)
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
        let (info, fields) = FSharpValue.GetUnionFields(value, t)
        writer.WriteStartObject()
        writer.WritePropertyName("_tag")
        writer.WriteValue(info.Tag)
        let cases = FSharpType.GetUnionCases(t)
        let case = cases.[info.Tag]
        let fields = case.GetFields()
        for field in fields do
            writer.WritePropertyName(field.Name)
            serializer.Serialize(writer, field.GetValue(value, [||]))
        writer.WriteEndObject()

    override x.ReadJson(reader: JsonReader, objectType: Type, existingValue: obj, serializer: JsonSerializer) =
          reader.Read() |> ignore //pop start obj type label
          reader.Read() |> ignore //pop tag prop name
          let union = FSharpType.GetUnionCases(objectType)
          let case = union.[int(reader.Value :?> int64)]
          let fieldValues =  [| 
                 for field in case.GetFields() do
                     reader.Read() |> ignore //pop item name
                     reader.Read() |> ignore
                     yield serializer.Deserialize(reader, field.PropertyType)
           |] 

          reader.Read() |> ignore
          FSharpValue.MakeUnion(case, fieldValues)


