namespace configManager

open System
open System.Text.RegularExpressions

open NUnit.Framework
open FsUnit

open configManager.tokens

module matching =

    let matchEvaluator (tokenMatch : Match) (tokens : token list) (env : string) =
        let token = tokenMatch.Groups.[1].Value
        let potentialValue = lookup tokens token env
        match potentialValue with
        | None -> "$$" + token + "$$"
        | Some value -> value

    let throwMissingTokens (tokensRemaining : MatchCollection) (env : string) =
        let missingTokens = tokensRemaining |> Seq.cast |> Seq.map (fun (mtch : Match) -> mtch.Groups.[0].Value) |> Array.ofSeq
        let message = String.Format("Cannot generate config file - tokens missing! {0} Environment: {1} {0} Tokens: {0} {2}", 
                        Environment.NewLine, 
                        env, 
                        String.Join(Environment.NewLine, missingTokens))
        raise (new MissingTokensException(message))

    let swapTokens master tokens env =
        let regex = new Regex(@"\$\$(\w+)\$\$")
        let swappedConfig = regex.Replace (master, new MatchEvaluator(fun (tokenMatch : Match) -> matchEvaluator tokenMatch tokens env))
        let tokensRemaining = (regex.Matches swappedConfig)
        if (tokensRemaining.Count > 0) then
            throwMissingTokens tokensRemaining env
        swappedConfig

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
