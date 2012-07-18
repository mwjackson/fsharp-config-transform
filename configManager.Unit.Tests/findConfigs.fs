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
type ``finding config files`` ()=
    let configManager = new configManager()
    [<Test>] member test.
        ``should find tokens file`` ()=
        configManager.findConfigs "." |> should contain @".\testFiles\test.tokens.config"
    [<Test>] member test.
        ``should find master file`` ()=
        configManager.findConfigs "." |> should contain @".\testFiles\test.master.config"
    [<Test>] member test.
        ``should find global tokens file`` ()=
        configManager.findConfigs "." |> should contain @".\testFiles\global.tokens"

open System.Yaml

type yamlReader() =
    member this.read file =
        Seq.ofArray<YamlNode>(YamlNode.FromYamlFile(file)) 
        |> Seq.head<YamlNode> :?> YamlMapping

[<TestFixture>] 
type ``reading yaml files`` ()=
    let yamlReader = new yamlReader()
    let yamlConfig = yamlReader.read("./testFiles/config.yaml")
    [<Test>] member test.
        ``should parse root property of a yaml file`` ()=
        yamlConfig.ContainsKey(new YamlScalar("token1")) |> should equal true
    [<Test>] member test.
        ``should parse nested propery of a yaml file`` ()=
        let token1 = yamlConfig.[new YamlScalar("token1")] :?> YamlMapping in 
        let env3 = token1.[new YamlScalar("env3")] :?> YamlScalar in 
        env3.Value |> should equal "value3"

open System.Text.RegularExpressions

type sunshineConfig ()=
    member this.swapTokens master tokens env =
        let regex = new Regex(@"\$\$(\w+)\$\$")
        regex.Replace (master, new MatchEvaluator(this.matchEvaluator))
    member this.matchEvaluator (regexMatch : Match) =
        "value3"

[<TestFixture>] 
type ``substituting tokens`` ()=
    let yamlReader = new yamlReader()
    let tokens = yamlReader.read("./testFiles/config.yaml")
    let sunshineConfig = new sunshineConfig()
    [<Test>] member test.
        ``finding a token should replace it from the sunshineMapping`` ()=
        let masterConfig = "<add name=\"Sunshine\" connectionString=\"$$token2$$\"/>" in
        let swappedConfig = sunshineConfig.swapTokens masterConfig tokens "env3" in
        swappedConfig |> should equal "<add name=\"Sunshine\" connectionString=\"value3\"/>"