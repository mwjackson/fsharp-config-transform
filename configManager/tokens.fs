namespace configManager

open System
open System.IO
open System.Yaml
open System.Collections.Generic

module tokens =

    type MissingTokensException (message)=
        inherit Exception(message)

    type DuplicateTokensException (message) =
        inherit Exception(message)

    type token = {
        name : string
        envs : Map<string, string>
        }

    let read file =
        List.ofArray<YamlNode>(YamlNode.FromYamlFile(file)) 
        |> List.head<YamlNode> :?> YamlMapping

    let toTokens (yamlDoc : YamlMapping) =
        [
            for configSetting in yamlDoc.Keys do
            yield { 
                name = (configSetting :?> YamlScalar).Value; 
                envs = yamlDoc.[configSetting] :?> YamlMapping 
                        |> Seq.map (fun token -> ((token.Key :?> YamlScalar).Value, (token.Value :?> YamlScalar).Value)) 
                        |> Map.ofSeq
            } 
        ]

    let lookupValue env envs token =
        if (Map.containsKey env envs) then
            Map.tryFind env envs
        else if (Map.containsKey "default" envs) then
            Map.tryFind "default" envs
        else 
            None

    let lookup (tokens:token list) (token:string) (env:string) =
        let tokenValues = tokens |> List.tryFind(fun (t : token) -> t.name = token)
        match tokenValues with
        | None -> None
        | Some tok -> lookupValue env tok.envs token

    let combine (globalTokens : token list) (projectTokens : token list) =
        let allTokens = List.concat [ globalTokens; projectTokens]
        let duplicateTokens = allTokens |> Seq.ofList |> Seq.countBy (fun token -> token.name) |> Seq.filter (fun count -> (snd count) > 1) |> Array.ofSeq
        if (duplicateTokens.Length > 0) then
            let message = String.Format("Duplicate tokens found in both global.tokens and application tokens! {0}{1}", 
                            Environment.NewLine, String.Join(Environment.NewLine, duplicateTokens |> Array.map (fun count -> fst count)))
            raise (new DuplicateTokensException(message))
        allTokens
