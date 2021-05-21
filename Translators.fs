module Translators

open System.Linq.Expressions
open EntityFrameworkCore.FSharp
open Microsoft.EntityFrameworkCore.Infrastructure
open Microsoft.EntityFrameworkCore.Query
open Microsoft.EntityFrameworkCore.Query
open Microsoft.EntityFrameworkCore.Storage

let memberTranslator(sqlExp: ISqlExpressionFactory ) = {
    new IMemberTranslator with
        member _.Translate(instance, member', returnType, loger) =
           if not (Extensions.isOptionType member'.DeclaringType) then
               null
           else
               sqlExp.Convert(instance, returnType) :> _

}

let methodCallTranslator(sqlExp: ISqlExpressionFactory ) = {
    new IMethodCallTranslator with
        member _.Translate(instance, method, arguments, loger) =
           if not (Extensions.isOptionType method.DeclaringType) then
                null
           else

           let expression = arguments |> Seq.head
           match method.Name with
           | "get_IsNone"-> sqlExp.IsNull(expression) :> _
           | "get_IsSome"-> sqlExp.IsNotNull(expression) :> _
           | _ -> null
}

type OptionMemberTranslatorPlugin(sqlExpressionFactory) =
    interface IMemberTranslatorPlugin with
        member _.Translators = seq {
            memberTranslator sqlExpressionFactory
        }

type OptionMethodCallTranslatorPlugin(sqlExpressionFactory) =
    interface IMethodCallTranslatorPlugin with
        member _.Translators = seq {
            methodCallTranslator sqlExpressionFactory
        }

type OptionTypeMapping =
    inherit StringTypeMapping
    new(storeType, dbType) = { inherit StringTypeMapping(storeType, dbType) }
    new(parameters) = { inherit StringTypeMapping(parameters) }

    override _.Clone(parameters: RelationalTypeMapping.RelationalTypeMappingParameters) =
        OptionTypeMapping(parameters) :> RelationalTypeMapping

//type OptionRelationalTypeMappingSourcePlugin() =
//    interface IRelationalTypeMappingSourcePlugin with
//        member this.FindMapping(mappingInfo) =
//            let typeInfo = mappingInfo.ClrType.GetTypeInfo()
//            if typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() = typedefof<Option<_>> then
//                OptionTypeMapping(mappingInfo.StoreTypeName, Nullable<_>()) :> RelationalTypeMapping
//            else null


type ExtensionInfo(extension) =
    inherit DbContextOptionsExtensionInfo(extension)
        override _.IsDatabaseProvider = false

        override _.GetServiceProviderHashCode() = 0L

        override _.PopulateDebugInfo debugInfo =
             debugInfo.["SqlServer: useFSharp"] <- "1"

        override _.LogFragment = "using FSharp"

type FsharpTypeOptionsExtension() =
     interface IDbContextOptionsExtension with
         member this.ApplyServices(services) =
             EntityFrameworkRelationalServicesBuilder(services)
                 .TryAddProviderSpecificServices(
                     fun x ->
                         x.TryAddSingletonEnumerable<IMemberTranslatorPlugin, OptionMemberTranslatorPlugin>()
                          .TryAddSingletonEnumerable<IMethodCallTranslatorPlugin, OptionMethodCallTranslatorPlugin>()
                         |> ignore)
             |> ignore

         member this.Info = ExtensionInfo(this :> IDbContextOptionsExtension) :> _
         member this.Validate(options) = ()


type SqlServerDbContextOptionsBuilder with
    member this.UseFSharpTypes() =
        let coreOptionsBuilder = (this :> IRelationalDbContextOptionsBuilderInfrastructure).OptionsBuilder

        let extension =
            let finded = coreOptionsBuilder.Options.FindExtension<FsharpTypeOptionsExtension>()
            if (box finded) <> null then finded else FsharpTypeOptionsExtension()

        (coreOptionsBuilder :> IDbContextOptionsBuilderInfrastructure).AddOrUpdateExtension(extension)
        this