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

type configManager() =
    member this.findConfigs dir = 
        let configs = Directory.GetFiles(dir, "*.config", SearchOption.AllDirectories)
        let tokens = Directory.GetFiles(dir, "global.tokens", SearchOption.AllDirectories)
        Array.append configs tokens

[<TestFixture>] 
module ``finding config files`` =
    let configManager = new configManager()

    [<Test>] 
    let ``should find tokens file`` ()=
        configManager.findConfigs "." |> should contain @".\testFiles\test.tokens.config"
    [<Test>] 
    let ``should find master file`` ()=
        configManager.findConfigs "." |> should contain @".\testFiles\test.master.config"
    [<Test>] 
    let ``should find global tokens file`` ()=
        configManager.findConfigs "." |> should contain @".\testFiles\global.tokens"

open System.Yaml
open System.Collections.Generic

type token = {
    name : string
    envs : (string * string) list
    }

type yamlReader() =
    member this.read file =
        List.ofArray<YamlNode>(YamlNode.FromYamlFile(file)) 
        |> List.head<YamlNode> :?> YamlMapping
    member this.toTokens (yamlDoc : YamlMapping) =
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
    member this.lookup (tokens:token list) (token:string) (env:string) =
        let tokenValues = tokens |> List.find(fun (t : token) -> t.name = token)
        tokenValues.envs |> List.find(fun (e : (string * string)) -> (fst e) = env) |> snd

[<TestFixture>] 
module ``reading yaml files`` =
    let yamlReader = new yamlReader()
    let yamlConfig = yamlReader.read("./testFiles/config.yaml")

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
        let tokens = yamlReader.toTokens yamlConfig
        let firstToken = (Seq.head tokens)
        firstToken.name |> should equal "token2"
        Seq.head(firstToken.envs) |> snd |> should equal "value2"
    [<Test>]
    let ``should be able to look up token easily`` () =
        let tokens = yamlReader.toTokens yamlConfig
        let tokenValue = yamlReader.lookup tokens "token2" "env3"
        tokenValue |> should equal "value3"

open System.Text.RegularExpressions

type sunshineConfig ()=
    member this.swapTokens master tokens env =
        let regex = new Regex(@"\$\$(\w+)\$\$")
        regex.Replace (master, new MatchEvaluator(this.matchEvaluator))
    member this.matchEvaluator (tokenMatch : Match) =
        let value = tokenMatch.Groups.[0].Value
        "value3"

[<TestFixture>] 
module ``substituting tokens`` =
    let yamlReader = new yamlReader()
    let tokens = yamlReader.read("./testFiles/config.yaml")
    let sunshineConfig = new sunshineConfig()

    [<Test>] [<Ignore("pending")>]
    let ``finding a token should replace it from the sunshineMapping`` ()=
        let masterConfig = "<add name=\"Sunshine\" connectionString=\"$$token2$$\"/>" 
        let swappedConfig = sunshineConfig.swapTokens masterConfig tokens "env3" 
        swappedConfig |> should equal "<add name=\"Sunshine\" connectionString=\"value3\"/>"