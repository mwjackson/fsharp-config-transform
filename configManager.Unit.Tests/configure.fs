namespace configManager

open System
open System.IO

open NUnit.Framework
open FsUnit

open configManager.configs
open configManager.tokens
open configManager.matching

module configure =

    let configureFor (env : string) (appConfig : applicationConfig) =
        let appTokens = read appConfig.appTokens |> toTokens
        let globalTokens = read appConfig.globalTokens |> toTokens
        let allTokens = List.concat [ globalTokens; appTokens]

        let master = File.ReadAllText appConfig.masterConfig
        let outputFile = appConfig.masterConfig.Replace(".master", "")
        
        let swappedConfig = swapTokens master allTokens env
        File.WriteAllText(outputFile, swappedConfig)

    let configureSolutionFor solutionDir environment =
        searchForConfigs solutionDir |> List.iter (configureFor environment)

    [<TestFixture>] 
    module ``end to end`` =
        let expectedConfig = File.ReadAllText("./projectA/expectedOutput.config")
        let expectedOutputFile = "./projectA/src/test.config"

        [<Test>]
        let ``generating config should swap tokens in the entire file`` ()=
            if (File.Exists expectedOutputFile) then
                File.Delete expectedOutputFile

            configureSolutionFor "./projectA" "env2"
            
            let actualConfig = File.ReadAllText expectedOutputFile
            Console.WriteLine (actualConfig)
            actualConfig |> should equal expectedConfig
