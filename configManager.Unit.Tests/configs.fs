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
        | 0 -> None
        | _ -> configs |> List.head |> Some

    let searchForConfigs directory = 
        let globalTokens = Directory.GetFiles(directory, "global.tokens", SearchOption.AllDirectories)
        if (globalTokens.Length = 0) then
            raise (new ArgumentException(String.Format("Could not find any global.tokens file in: {0}{1}", Environment.NewLine, directory)))
        Directory.GetDirectories(directory, "*", SearchOption.AllDirectories) |> List.ofArray
            |> List.choose (fun dir -> findConfigsInDir dir)
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
            let applicationConfig = searchForConfigs ".\projectA" |> List.head
            applicationConfig.appTokens |> should equal @".\projectA\src\test.tokens.config"
        [<Test>] 
        let ``should find master file`` ()=
            let applicationConfig = searchForConfigs ".\projectA" |> List.head
            applicationConfig.masterConfig |> should equal @".\projectA\src\test.master.config"
        [<Test>] 
        let ``should find global tokens file`` ()=
            let applicationConfig = searchForConfigs ".\projectA" |> List.head
            applicationConfig.globalTokens |> should equal @".\projectA\global.tokens"
        [<Test>] 
        let ``missing global tokens file should report an error`` ()=
            (fun () -> searchForConfigs @"c:\temp" |> ignore) |> should throw typeof<ArgumentException>
        [<Test>] 
        let ``searching a directory should return a tuple of master and tokens`` ()=
            let configFiles = findConfigsInDir @".\projectA\src"
            configFiles.Value |> should equal (@".\projectA\src\test.master.config", @".\projectA\src\test.tokens.config")
        [<Test>] 
        let ``searching a directory with no configs should return an empty tuple`` ()=
            let configFiles = findConfigsInDir @".\projectB"
            configFiles |> should equal None