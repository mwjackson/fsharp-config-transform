namespace configManager

open System
open System.IO
open System.Yaml
open System.Collections.Generic

open NUnit.Framework
open FsUnit

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

    [<TestFixture>] 
    module ``reading yaml files`` =
        let yamlConfig = read("./testFiles/config.yaml")

        [<Test>] 
        let ``should parse root property of a yaml file`` ()=
            yamlConfig.ContainsKey(new YamlScalar("token1")) |> should equal true
        [<Test>] 
        let ``should parse nested property of a yaml file`` ()=
            let token1 = yamlConfig.[new YamlScalar("token1")] :?> YamlMapping
            let env3 = token1.[new YamlScalar("env3")] :?> YamlScalar
            env3.Value |> should equal "value3"
        [<Test>]
        let ``should convert yamldocument to digestable format`` ()=
            let tokens = toTokens yamlConfig
            let firstToken = (Seq.head tokens)
            firstToken.name |> should equal "token2"
            firstToken.envs |> Map.find "env2" |> should equal "value5"
        [<Test>]
        let ``should be able to look up token easily`` () =
            let tokens = toTokens yamlConfig
            let tokenValue = lookup tokens "token2" "env3"
            tokenValue.Value |> should equal "value6"
        [<Test>]
        let ``looking up token should default when environment not found`` () =
            let tokens = toTokens yamlConfig
            let tokenValue = lookup tokens "token1" "anotherEnv"
            tokenValue.Value |> should equal "defaultValue"
        [<Test>]
        let ``combining global and project tokens files should give a single collection`` () =
            let projectTokens = [ yield { name = "token1"; envs = ([ ("env1", "value1") ] |> Map.ofList) } ]
            let globalTokens = [ yield { name = "token3"; envs = ([ ("env3", "value9") ] |> Map.ofList) } ]
            let allTokens = combine globalTokens projectTokens
            (lookup allTokens "token1" "env1").Value |> should equal "value1"
            (lookup allTokens "token3" "env3").Value |> should equal "value9"
        [<Test>]
        let ``duplicate tokens in global and project should report error`` () =
            let projectTokens = [ yield { name = "token1"; envs = ([ ("env1", "value1") ] |> Map.ofList) } ]
            let globalTokens = [ yield { name = "token1"; envs = ([ ("env3", "value9") ] |> Map.ofList) } ]
            (fun () -> combine globalTokens projectTokens |> ignore ) |> should throw typeof<DuplicateTokensException>
 

