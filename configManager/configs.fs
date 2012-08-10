namespace configManager

open System
open System.IO

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

