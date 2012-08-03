﻿namespace configManager

open System
open System.IO

open NUnit.Framework
open FsUnit

open configManager.configs
open configManager.tokens
open configManager.matching

module configure =

    let configureFor (env : string) (appConfig : applicationConfig) =
        let tokens = read appConfig.appTokens |> toTokens
        let master = File.ReadAllText appConfig.masterConfig
        let outputFile = appConfig.masterConfig.Replace(".master", "")
        let swappedConfig = swapTokens master tokens env
        File.WriteAllText(outputFile, swappedConfig)

    let configureSolutionFor solutionDir environment =
        searchForConfigs solutionDir |> List.iter (configureFor environment)

    [<TestFixture>] 
    module ``end to end`` =
        let expectedConfig = File.ReadAllText("./testFiles/expectedOutput.config")
        let expectedOutputFile = "./testFiles/projectA/test.config"

        [<Test>]
        let ``generating config should swap tokens in the entire file`` ()=
            if (File.Exists expectedOutputFile) then
                File.Delete expectedOutputFile

            configureSolutionFor "./testFiles" "env2"
            
            let actualConfig = File.ReadAllText expectedOutputFile
            Console.WriteLine (actualConfig)
            actualConfig |> should equal expectedConfig
