namespace configManager.Tests

open System
open System.IO

open configManager
open NUnit.Framework
open FsUnit

module configManager =

    type applicationConfig = {
        globalTokens: string
        appTokens: string
        masterConfig: string
    }

    let findConfigs dir = 
        let globalTokens = Directory.GetFiles(dir, "global.tokens", SearchOption.AllDirectories)
        if (globalTokens.Length = 0) then
            raise (new ArgumentException(String.Format("Could not find any global.tokens file in: {0}{1}", Environment.NewLine, dir)))
        let directories = Directory.GetDirectories(dir, "*", SearchOption.AllDirectories)
        [
            for directory in directories do
            let masterConfigs = Directory.GetFiles(directory, "*.master.config", SearchOption.TopDirectoryOnly)
            let appTokens = Directory.GetFiles(directory, "*.tokens.config", SearchOption.TopDirectoryOnly)
            if (masterConfigs.Length = 1 && appTokens.Length = 1) then
                yield {
                    globalTokens = globalTokens.[0]
                    appTokens = appTokens.[0]
                    masterConfig = masterConfigs.[0]
                }
        ]

    [<TestFixture>] 
    module ``finding config files`` =
        [<Test>] 
        let ``should find tokens file`` ()=
            let applicationConfig = findConfigs "." |> List.head
            applicationConfig.appTokens |> should equal @".\testFiles\projectA\test.tokens.config"
        [<Test>] 
        let ``should find master file`` ()=
            let applicationConfig = findConfigs "." |> List.head
            applicationConfig.masterConfig |> should equal @".\testFiles\projectA\test.master.config"
        [<Test>] 
        let ``should find global tokens file`` ()=
            let applicationConfig = findConfigs "." |> List.head
            applicationConfig.globalTokens |> should equal @".\testFiles\global.tokens"
        [<Test>] 
        let ``missing global tokens file should report an error`` ()=
            (fun () -> findConfigs @"c:\temp" |> ignore) |> should throw typeof<ArgumentException>

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
        let tokenValues = tokens |> List.tryFind(fun (t : token) -> t.name = token)
        match tokenValues with
        | None -> "$$" + token + "$$"
        | Some token -> token.envs |> List.find(fun (e : (string * string)) -> (fst e) = env) |> snd

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
            Seq.head(firstToken.envs) |> snd |> should equal "value5"
        [<Test>]
        let ``should be able to look up token easily`` () =
            let tokens = toTokens yamlConfig
            let tokenValue = lookup tokens "token2" "env3"
            tokenValue |> should equal "value6"

    open System.Text.RegularExpressions

    type MissingTokensException (message)=
        inherit Exception(message)

    let matchEvaluator (tokenMatch : Match) (tokens : token list) (env : string) =
        let token = tokenMatch.Groups.[1].Value
        lookup tokens token env

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
        let tokens = read("./testFiles/config.yaml") |> toTokens

        [<Test>]
        let ``finding a token should replace it from the sunshineMapping`` ()=
            let masterConfig = "<add name=\"Sunshine\" connectionString=\"$$token2$$\"/>" 
            let swappedConfig = swapTokens masterConfig tokens "env3" 
            swappedConfig |> should equal "<add name=\"Sunshine\" connectionString=\"value6\"/>"

        [<Test>]
        let ``if there are any tokens left over there should be an error`` ()=
            let masterConfig = "<add name=\"Sunshine\" connectionString=\"$$token5$$\"/>" 
            (fun () -> swapTokens masterConfig tokens "env3" |> ignore) |> should throw typeof<MissingTokensException>

    let generateFor (env : string) (appConfig : applicationConfig) =
        let tokens = read appConfig.appTokens |> toTokens
        let master = File.ReadAllText appConfig.masterConfig
        let outputFile = appConfig.masterConfig.Replace(".master", "")
        let swappedConfig = swapTokens master tokens env
        File.WriteAllText(outputFile, swappedConfig)

    let findAndGenerateFor dir env =
        findConfigs dir |> List.iter (generateFor env)

    [<TestFixture>] 
    module ``end to end`` =
        let expectedConfig = File.ReadAllText("./testFiles/expectedOutput.config")
        let expectedOutputFile = "./testFiles/projectA/test.config"
        [<Test>]
        let ``generating config should swap tokens in the entire file`` ()=
            if (File.Exists expectedOutputFile) then
                File.Delete expectedOutputFile

            findAndGenerateFor "./testFiles" "env2"
            
            let actualConfig = File.ReadAllText expectedOutputFile
            Console.WriteLine (actualConfig)
            actualConfig |> should equal expectedConfig