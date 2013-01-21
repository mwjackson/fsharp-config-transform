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

    let configureForOutputtingAs (env : string) (appConfig : applicationConfig) (outputFile : string) =
        printConfig env appConfig

        let appTokens = read appConfig.appTokens |> toTokens
        let globalTokens = read (Some(appConfig.globalTokens)) |> toTokens
        let allTokens = combine globalTokens appTokens

        let master = File.ReadAllText appConfig.masterConfig

        let swappedConfig = swapTokens master allTokens env
        Console.WriteLine("Writing to config: {0}", outputFile)
        File.WriteAllText(outputFile, swappedConfig)

    let configureAppConfigFor solutionDir environment =
        let configs = searchForConfigs solutionDir 
        for config in configs do
            let outputFile = config.masterConfig.Replace(".master", "")
            configureForOutputtingAs environment config outputFile

    let configurePackageForEnvironments solutionDir environments =
        let configs = searchForConfigs solutionDir 
        for config in configs do
            for environment in environments do
                let outputFile = config.masterConfig.Replace("master", environment)
                configureForOutputtingAs environment config outputFile

    let configureSolutionFor (solutionDir : string) (environment : string) =
        Console.WriteLine("Searching in {0}", solutionDir)
        if (environment = "all") then
            configurePackageForEnvironments solutionDir ["local"; "dev"; "sit"; "uat"; "prod"; "uathedani"; "prodhedani"]
        else
            configureAppConfigFor solutionDir environment