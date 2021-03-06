open Contexto
open EntityFrameworkCore.FSharp.DbContextHelpers
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Query
open System.Linq

[<EntryPoint>]
let main argv =
    use ctx = new Context()

    [ctx.Database.EnsureDeleted()
     ctx.Database.EnsureCreated()]
    |> ignore

    let pessoas = ctx.Set<Pessoa>()
    //create person
    let pessoa = { Id=0; Nome = "Lucas"; Idade = 30; Telefone = None  }

    // save person
    addEntity ctx pessoa
    saveChanges ctx

    for p in pessoas do
        printfn "%A" p

    let newPessoa = updateEntity ctx (fun x -> x.Id) { pessoa with Nome = "Teles"; Telefone = Some "42" }
    saveChanges ctx

    pessoas |> Seq.iter (printfn "%A")

    query {
        for p in pessoas do
        where (p.Telefone.IsNone || p.Telefone.Value = "42")
        select p.Nome
    }

    |> Seq.iter (printfn "%A")

    printfn "Hello world"
    0 // return an integer exit code