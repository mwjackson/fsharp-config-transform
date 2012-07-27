namespace configManager.Tests

open System
open System.IO

open configManager
open NUnit.Framework
open FsUnit

module Util =
    let printReturn (obj) =
        Console.WriteLine ( typeof<obj>.ToString  )
        obj

module configManager =

    type applicationConfig = {
        globalTokens: string
        appTokens: string
        masterConfig: string
    }

    let findConfigs dir = 
        let globalTokens = Directory.GetFiles(dir, "global.tokens", SearchOption.AllDirectories).[0]
        let directories = Directory.GetDirectories(dir, "*", SearchOption.AllDirectories)
        [
            for directory in directories do
            let masterConfigs = Directory.GetFiles(directory, "*.master.config", SearchOption.TopDirectoryOnly)
            let appTokens = Directory.GetFiles(directory, "*.tokens.config", SearchOption.TopDirectoryOnly)
            if (masterConfigs.Length = 1 && appTokens.Length = 1) then
                yield {
                    globalTokens = globalTokens
                    appTokens = appTokens.[0]
                    masterConfig = masterConfigs.[0]
                }
        ]

    [<TestFixture>] 
    module ``finding config files`` =
        [<Test>] 
        let ``should find tokens file`` ()=
            let applicationConfig = findConfigs "." |> List.head
            applicationConfig.appTokens |> should equal @".\testFiles\test.tokens.config"
        [<Test>] 
        let ``should find master file`` ()=
            let applicationConfig = findConfigs "." |> List.head
            applicationConfig.masterConfig |> should equal @".\testFiles\test.master.config"
        [<Test>] 
        let ``should find global tokens file`` ()=
            let applicationConfig = findConfigs "." |> List.head
            applicationConfig.globalTokens |> should equal @".\testFiles\global.tokens"

    open System.Yaml
    open System.Collections.Generic

    type token = {
        name : string
        envs : (string * string) list
        }

    let read file =
        List.ofArray<YamlNode>(YamlNode.FromYamlFile(file)) 
        |> List.head<YamlNode> :?> YamlMapping

    let toTokens (yamlDoc : YamlMapping) =
        [
            for configSetting in yamlDoc.Keys do
            yield { 
                name = (configSetting :?> YamlScalar).Value; 
                envs = 
                [
                    for token in yamlDoc.[configSetting] :?> YamlMapping do
                        let env = (token.Key :?> YamlScalar).Value
                        let value = (token.Value :?> YamlScalar).Value
                        yield (env, value)
                ]
            } 
        ]

    let lookup (tokens:token list) (token:string) (env:string) =
            let tokenValues = tokens |> List.find(fun (t : token) -> t.name = token)
            tokenValues.envs |> List.find(fun (e : (string * string)) -> (fst e) = env) |> snd

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
            Seq.head(firstToken.envs) |> snd |> should equal "value2"
        [<Test>]
        let ``should be able to look up token easily`` () =
            let tokens = toTokens yamlConfig
            let tokenValue = lookup tokens "token2" "env3"
            tokenValue |> should equal "value3"

    open System.Text.RegularExpressions

    let matchEvaluator (tokenMatch : Match) (tokens : token list) (env : string) =
        let token = tokenMatch.Groups.[1].Value
        lookup tokens token env

    let swapTokens master tokens env =
        let regex = new Regex(@"\$\$(\w+)\$\$")
        regex.Replace (master, new MatchEvaluator(fun (tokenMatch : Match) -> matchEvaluator tokenMatch tokens env))

    [<TestFixture>] 
    module ``substituting tokens`` =
        let tokens = toTokens(read("./testFiles/config.yaml"))

        [<Test>]
        let ``finding a token should replace it from the sunshineMapping`` ()=
            let masterConfig = "<add name=\"Sunshine\" connectionString=\"$$token2$$\"/>" 
            let swappedConfig = swapTokens masterConfig tokens "env3" 
            swappedConfig |> should equal "<add name=\"Sunshine\" connectionString=\"value3\"/>"