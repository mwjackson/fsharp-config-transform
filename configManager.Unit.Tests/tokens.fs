namespace configManager

open System
open System.IO

open configManager
open NUnit.Framework
open FsUnit

open System.Yaml
open System.Collections.Generic

module tokens =

    type MissingTokensException (message)=
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
            Map.find env envs
        else if (Map.containsKey "default" envs) then
            Map.find "default" envs
        else 
            "$$" + token + "$$"

    let lookup (tokens:token list) (token:string) (env:string) =
        let tokenValues = tokens |> List.tryFind(fun (t : token) -> t.name = token)
        match tokenValues with
        | None -> "$$" + token + "$$"
        | Some tok -> lookupValue env tok.envs token

    [<TestFixture>] 
    module ``reading yaml files`` =
        let yamlConfig = read("./testFiles/config.yaml")

        [<Test>] 
        let ``should parse root property of a yaml file`` ()=
            yamlConfig.ContainsKey(new YamlScalar("token1")) |> should equal true
        [<Test>] 
        let ``should parse nested propery of a yaml file`` ()=
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
            tokenValue |> should equal "value6"
        [<Test>]
        let ``looking up token should default when environment not found`` () =
            let tokens = toTokens yamlConfig
            let tokenValue = lookup tokens "token1" "anotherEnv"
            tokenValue |> should equal "defaultValue"

