namespace configManager

open System
open System.Text.RegularExpressions

open tokens

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