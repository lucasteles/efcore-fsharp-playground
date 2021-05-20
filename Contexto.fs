module Contexto 

open System.ComponentModel.DataAnnotations
open EntityFrameworkCore.FSharp
open Microsoft.EntityFrameworkCore
open EntityFrameworkCore.FSharp.Extensions
open Microsoft.EntityFrameworkCore.Storage.ValueConversion

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
    
    [<DefaultValue>] val mutable pessoas : DbSet<Pessoa>
    member this.Pessoas with get() = this.pessoas and set v = this.pessoas <- v

    override _.OnConfiguring options =
        options.UseSqlServer(connectionString) |> ignore

    override _.OnModelCreating builder =
        builder.RegisterOptionTypes()

        ()