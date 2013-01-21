namespace configManager

open System
open System.IO

module configs = 

    type applicationConfig = {
        globalTokens: string
        masterConfig: string
        appTokens: string option
    }

    let findMasterTokenPairs (dir : string) = 
        Console.WriteLine("Looking for config pairs in: {0}", dir)
        let masterConfigs = Directory.GetFiles(dir, "*.master.config", SearchOption.TopDirectoryOnly) |> List.ofArray
        let appTokens = Directory.GetFiles(dir, "*.tokens.config", SearchOption.TopDirectoryOnly) |> List.ofArray
        match (masterConfigs.Length, appTokens.Length) with
        | (0, 0) -> None
        | (1, 0) -> Some(masterConfigs.Head, None) 
        | (0, 1) -> raise (new ArgumentException(String.Format("Missing Master file in {0}", dir)))
        | (1, 1) -> Some(masterConfigs.Head, Some(appTokens.Head)) 
        | (_, _) -> raise (new ArgumentException(String.Format("Multiple Master/Token files in {0}", dir)))

    let removeJunkFolders (dir : string) =
        not(dir.Contains("obj")) && not(dir.Contains(".svn"))

    let toApplicationConfig globalTokens configPair =
        { 
            globalTokens = globalTokens;
            masterConfig = fst configPair;
            appTokens = snd configPair;
        } : applicationConfig 

    let searchForConfigs directory = 
        let dir = new DirectoryInfo(directory)
        let globalTokens = Seq.concat [ 
                            Directory.GetFiles(directory, "global.tokens", SearchOption.AllDirectories);
                            Directory.GetFiles(dir.Parent.FullName, "global.tokens", SearchOption.TopDirectoryOnly);
                            Directory.GetFiles(dir.Parent.Parent.FullName, "global.tokens", SearchOption.TopDirectoryOnly)
                            ] |> List.ofSeq
        if (globalTokens.IsEmpty) then
            raise (new ArgumentException(String.Format("Could not find any global.tokens file in: {0}{1}", Environment.NewLine, directory)))
        Console.WriteLine("Found global.tokens: {0}", globalTokens.Head)
        Directory.GetDirectories(directory, "*", SearchOption.AllDirectories) |> List.ofArray
            |> List.append [directory]
            |> List.filter removeJunkFolders
            |> List.choose findMasterTokenPairs
            |> List.map (toApplicationConfig globalTokens.Head)

