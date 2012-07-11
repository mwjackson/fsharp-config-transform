namespace configManager

open System.IO

type configManager() =
    member self.findConfigs dir = 
        Directory.GetFiles(dir, "*.config", SearchOption.AllDirectories)