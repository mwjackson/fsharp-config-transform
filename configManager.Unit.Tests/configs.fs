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

    let findMasterTokenPairs dir = 
        let masterConfigs = Directory.GetFiles(dir, "*.master.config", SearchOption.TopDirectoryOnly) |> List.ofArray
        let appTokens = Directory.GetFiles(dir, "*.tokens.config", SearchOption.TopDirectoryOnly) |> List.ofArray
        match (masterConfigs.Length, appTokens.Length) with
        | (0, 0) -> None
        | (1, 0) -> raise (new ArgumentException(String.Format("Missing Master/Token file pair in {0}", dir)))
        | (0, 1) -> raise (new ArgumentException(String.Format("Missing Master/Token file pair in {0}", dir)))
        | (1, 1) -> List.zip masterConfigs appTokens |> List.head |> Some
        | (_, _) -> raise (new ArgumentException(String.Format("Multiple Master/Token files in {0}", dir)))

    let removeBinFolders (dir : string) =
        dir.Contains("bin") = false

    let toApplicationConfig globalTokens configPair =
        { 
            globalTokens = globalTokens;
            masterConfig = fst configPair;
            appTokens = snd configPair;
        } : applicationConfig 

    let searchForConfigs directory = 
        let globalTokens = Directory.GetFiles(directory, "global.tokens", SearchOption.AllDirectories)
        if (globalTokens.Length = 0) then
            raise (new ArgumentException(String.Format("Could not find any global.tokens file in: {0}{1}", Environment.NewLine, directory)))
        Directory.GetDirectories(directory, "*", SearchOption.AllDirectories) |> List.ofArray
            |> List.filter removeBinFolders
            |> List.choose findMasterTokenPairs
            |> List.map (toApplicationConfig globalTokens.[0])

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
            let configFiles = findMasterTokenPairs @".\projectA\src"
            configFiles.Value |> should equal (@".\projectA\src\test.master.config", @".\projectA\src\test.tokens.config")
        [<Test>] 
        let ``searching a directory with no configs should return an empty tuple`` ()=
            let configFiles = findMasterTokenPairs @".\projectB"
            configFiles |> should equal None
        [<Test>] 
        let ``searching a directory with missing config pair should report error`` ()=
            (fun () -> findMasterTokenPairs @".\projectC\srcC\" |> ignore) |> should throw typeof<ArgumentException>
        [<Test>] 
        let ``should ignore bin directories`` ()=
            let configFiles = searchForConfigs ".\projectD" 
            let containsBinInPaths = configFiles |> List.exists (fun config -> config.appTokens.Contains "bin" || config.masterConfig.Contains "bin" )
            containsBinInPaths |> should equal false