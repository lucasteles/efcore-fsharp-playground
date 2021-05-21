module Contexto 

open System.ComponentModel.DataAnnotations
open EntityFrameworkCore.FSharp
open Translators
open Microsoft.EntityFrameworkCore
open EntityFrameworkCore.FSharp.Extensions

[<Literal>]
let connectionString = "Server=localhost; Database=FooDb; User=sa; Password=Senha@123"

[<CLIMutable>]
type Pessoa = {
    [<Key>] Id: int
    Nome: string
    Idade: int
    Telefone: string option
}

type Context() =
    inherit DbContext()
    override _.OnConfiguring options =
        options
//            .LogTo(fun s -> printfn "%s" s)
            .UseSqlServer(connectionString, (fun x -> x.UseFSharpTypes() |> ignore)) |> ignore

    override _.OnModelCreating builder =
        builder.Entity<Pessoa>() |> ignore
        builder.RegisterOptionTypes()