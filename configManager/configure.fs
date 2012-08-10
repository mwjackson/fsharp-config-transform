namespace configManager

open System.IO

open configs
open tokens
open matching

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