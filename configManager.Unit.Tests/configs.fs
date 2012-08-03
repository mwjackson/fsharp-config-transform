namespace configManager

open System
open System.IO

open NUnit.Framework
open FsUnit

module configs = 

    type applicationConfig = {
        globalTokens: string
        appTokens: string
        masterConfig: string
    }

    let findConfigsInDir dir = 
        let masterConfigs = Directory.GetFiles(dir, "*.master.config", SearchOption.TopDirectoryOnly)
        let appTokens = Directory.GetFiles(dir, "*.tokens.config", SearchOption.TopDirectoryOnly)
        let configs = Array.zip masterConfigs appTokens |> List.ofArray
        match configs.Length with
        | 0 -> ("", "")
        | _ -> configs |> List.head

    let searchForConfigs directory = 
        let globalTokens = Directory.GetFiles(directory, "global.tokens", SearchOption.AllDirectories)
        if (globalTokens.Length = 0) then
            raise (new ArgumentException(String.Format("Could not find any global.tokens file in: {0}{1}", Environment.NewLine, directory)))
        Directory.GetDirectories(directory, "*", SearchOption.AllDirectories) |> List.ofArray
            |> List.map (fun dir -> findConfigsInDir dir)
            |> List.filter (fun config -> 
                match config with
                | ("", "") -> false
                | (_, _) -> true)
            |> List.map (fun config -> 
                { 
                    globalTokens = globalTokens.[0];
                    masterConfig = fst config;
                    appTokens = snd config;
                } : applicationConfig)

    [<TestFixture>] 
    module ``finding config files`` =
        [<Test>] 
        let ``should find tokens file`` ()=
            let applicationConfig = searchForConfigs "." |> List.head
            applicationConfig.appTokens |> should equal @".\testFiles\projectA\test.tokens.config"
        [<Test>] 
        let ``should find master file`` ()=
            let applicationConfig = searchForConfigs "." |> List.head
            applicationConfig.masterConfig |> should equal @".\testFiles\projectA\test.master.config"
        [<Test>] 
        let ``should find global tokens file`` ()=
            let applicationConfig = searchForConfigs "." |> List.head
            applicationConfig.globalTokens |> should equal @".\testFiles\global.tokens"
        [<Test>] 
        let ``missing global tokens file should report an error`` ()=
            (fun () -> searchForConfigs @"c:\temp" |> ignore) |> should throw typeof<ArgumentException>
        [<Test>] 
        let ``search a directory should return a tuple of master and tokens`` ()=
            let configFiles = findConfigsInDir @".\testFiles\projectA"
            configFiles |> should equal (@".\testFiles\projectA\test.master.config", @".\testFiles\projectA\test.tokens.config")