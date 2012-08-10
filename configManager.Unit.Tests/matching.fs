namespace configManager

open NUnit.Framework
open FsUnit

open configManager.matching
open configManager.tokens

module matching =

    [<TestFixture>] 
    module ``substituting tokens`` =
        let tokens = read("./testFiles/config.yaml") |>  toTokens

        [<Test>]
        let ``finding a token should replace it from the sunshineMapping`` ()=
            let masterConfig = "<add name=\"Sunshine\" connectionString=\"$$token2$$\"/>" 
            let swappedConfig = swapTokens masterConfig tokens "env3" 
            swappedConfig |> should equal "<add name=\"Sunshine\" connectionString=\"value6\"/>"

        [<Test>]
        let ``if there are any tokens left over there should be an error`` ()=
            let masterConfig = "<add name=\"Sunshine\" connectionString=\"$$token5$$\"/>" 
            (fun () -> swapTokens masterConfig tokens "env3" |> ignore) |> should throw typeof<MissingTokensException>
