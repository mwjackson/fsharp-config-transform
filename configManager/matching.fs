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

    let matchEvaluatorEnvvar (tokenMatch : Match) =
        let token = tokenMatch.Groups.[1].Value
        let potentialValue = System.Environment.GetEnvironmentVariable(token)
        match potentialValue with
        | null -> "%%" + token + "%%"
        | value -> value

    let throwMissingTokens (tokensRemaining : seq<Match>) (env : string) =
        let missingTokens = tokensRemaining |> Seq.cast |> Seq.map (fun (mtch : Match) -> mtch.Groups.[0].Value) |> Array.ofSeq
        let message = String.Format("Cannot generate config file - no tokens found for environment! {0} Environment: {1}{0} Remaining Tokens: {0}{2}", 
                        Environment.NewLine, 
                        env, 
                        String.Join(Environment.NewLine, missingTokens))
        raise (new MissingTokensException(message))

    let swapTokens master tokens env =
        let regex1 = new Regex(@"\$\$(\w+)\$\$")
        let swappedConfig = regex1.Replace (master, new MatchEvaluator(fun (tokenMatch : Match) -> matchEvaluator tokenMatch tokens env))
        let regex2 = new Regex(@"\%\%(\w+)\%\%")
        let swappedConfig = regex2.Replace (swappedConfig, new MatchEvaluator(fun (tokenMatch : Match) -> matchEvaluatorEnvvar tokenMatch))
        let tokensRemaining = Seq.append (regex1.Matches swappedConfig |> Seq.cast<Match> ) (regex2.Matches swappedConfig |> Seq.cast<Match>) |> List.ofSeq
        if (tokensRemaining.Length > 0) then
            throwMissingTokens tokensRemaining env
        swappedConfig