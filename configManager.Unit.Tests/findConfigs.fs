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