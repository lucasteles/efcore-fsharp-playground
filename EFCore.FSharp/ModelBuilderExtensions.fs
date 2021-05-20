namespace EntityFrameworkCore.FSharp

open System
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Storage.ValueConversion
open System.Reflection
open System.Text

module Extensions =

    let private genericOptionConverterType = typedefof<OptionConverter<_>>

    let isOptionType (t:Type) =
        let typeInfo = t.GetTypeInfo()
        typeInfo.IsGenericType
        && typeInfo.GetGenericTypeDefinition() = typedefof<Option<_>>

    let unwrapOptionType (t:Type) =
        if isOptionType t then t.GenericTypeArguments.[0] else t

    type ModelBuilder with

        member this.UseValueConverterForType<'a>(converter : ValueConverter) =
            this.UseValueConverterForType(typeof<'a>, converter)

        member this.UseValueConverterForType(``type`` : Type, converter : ValueConverter) =

            this.Model.GetEntityTypes()
            |> Seq.iter(fun e ->
                e.ClrType.GetProperties()
                |> Seq.filter(fun p -> p.PropertyType = ``type``)
                |> Seq.iter(fun p ->
                    this.Entity(e.Name).Property(p.Name).HasConversion(converter) |> ignore
                )
            )

            this

        member this.RegisterOptionTypes() =

            let makeOptionConverter t =
                let underlyingType = unwrapOptionType t
                let converterType = genericOptionConverterType.MakeGenericType(underlyingType)
                let converter = converterType.GetConstructor([||]).Invoke([||]) :?> ValueConverter
                converter

            let converterDetails =
                this.Model.GetEntityTypes()
                |> Seq.filter (fun p -> not <| isOptionType p.ClrType)
                |> Seq.collect (fun e -> e.ClrType.GetProperties())
                |> Seq.filter (fun p -> isOptionType p.PropertyType)
                |> Seq.map(fun p -> (p, (makeOptionConverter p.PropertyType)) )

            for (prop, converter) in converterDetails do
                    this.Entity(prop.DeclaringType)
                        .Property(prop.PropertyType,prop.Name)
                        .HasConversion(converter)
                    |> ignore

    let registerOptionTypes (modelBuilder : ModelBuilder) =
        modelBuilder.RegisterOptionTypes()

    let useValueConverter<'a> (converter : ValueConverter) (modelBuilder : ModelBuilder) =
        modelBuilder.UseValueConverterForType<'a>(converter)

    let useValueConverterForType (``type`` : Type) (converter : ValueConverter) (modelBuilder : ModelBuilder) =
        modelBuilder.UseValueConverterForType(``type``, converter)
