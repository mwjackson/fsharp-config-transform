namespace configManager

open System

open configure

module Program =

    let printUsage =
        Console.WriteLine(@"
    Usage: 
    Option 1 - configManager <searchDirectory> <environment>
    Option 2 - configManager <environment>
    Arguments:
    <searchDirectory> - The root directory to begin recursive search for configs (defaults to '.' if not supplied)
    <environment> - The environment to configure the files for")
    
    let main(args : string[]) = 
        args |> Array.iter (fun arg -> Console.WriteLine(arg))
        match (args.Length) with
        | 0 -> printUsage
        | 1 -> configureSolutionFor "." args.[0] 
        | 2 -> configureSolutionFor args.[0] args.[1]
        | _ -> printUsage
        0