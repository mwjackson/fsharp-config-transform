namespace configManager

open System

open NUnit.Framework
open FsUnit

open configManager.configs

module configs = 

    [<TestFixture>] 
    module ``finding config files`` =
        [<Test>] 
        let ``should find tokens file`` ()=
            let applicationConfig = searchForConfigs ".\projectA" |> List.head
            applicationConfig.appTokens.Value |> should equal @".\projectA\src\test.tokens.config"
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
            (fun () -> searchForConfigs @"c:\inetpub\wwwroot" |> ignore) |> should throw typeof<ArgumentException>
        [<Test>] 
        let ``searching a directory should return a tuple of master and tokens`` ()=
            let configFiles = findMasterTokenPairs @".\projectA\src"
            configFiles.Value |> should equal (@".\projectA\src\test.master.config", Some(@".\projectA\src\test.tokens.config"))
        [<Test>] 
        let ``searching a directory with no configs should return an empty tuple`` ()=
            let configFiles = findMasterTokenPairs @".\projectB"
            configFiles |> should equal None
        [<Test>] 
        let ``searching a directory with missing tokens file should return optional`` ()=
            let pairs = findMasterTokenPairs @".\projectC\srcC\"
            snd pairs.Value |> should equal None
        [<Test>] 
        let ``should ignore bin directories`` ()=
            let configFiles = searchForConfigs ".\projectD" 
            let containsBinInPaths = configFiles |> List.exists (fun config -> config.appTokens.Value.Contains "bin" || config.masterConfig.Contains "bin" )
            containsBinInPaths |> should equal false