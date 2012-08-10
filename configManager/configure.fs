namespace configManager

open System
open System.IO

open configs
open tokens
open matching

module configure =

    let printConfig env appConfig =
        Console.WriteLine(String.Format(@"
Configuring For - 
GlobalTokens: {0}
MasterConfig: {1}
AppTokens: {2}
Environment: {3}", appConfig.globalTokens, appConfig.masterConfig, appConfig.appTokens, env))

    let configureFor (env : string) (appConfig : applicationConfig) =
        printConfig env appConfig
        let appTokens = read appConfig.appTokens |> toTokens
        let globalTokens = read appConfig.globalTokens |> toTokens
        let allTokens = combine globalTokens appTokens

        let master = File.ReadAllText appConfig.masterConfig
        let outputFile = appConfig.masterConfig.Replace(".master", "")
        
        let swappedConfig = swapTokens master allTokens env
        File.WriteAllText(outputFile, swappedConfig)

    let configureSolutionFor solutionDir environment =
        searchForConfigs solutionDir |> List.iter (configureFor environment)