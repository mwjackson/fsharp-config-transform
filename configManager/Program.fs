namespace configManager

open System

open configure

module Program =

    let printUsage () =
        Console.WriteLine(@"
Usage: 
Option 1 - configManager <searchDirectory> <environment>
Option 2 - configManager <environment>
Option 3 - configManager all
Arguments:
<searchDirectory> - The root directory to begin recursive search for configs (defaults to '.' if not supplied)
<environment> - The environment to configure the files for
all - Create a config for each environment (local, dev, sit, uat, prod)") 
    
    [<EntryPoint>]
    let main(args : string[]) = 
        Console.WriteLine("Arguments:")
        args |> Array.iter (fun arg -> Console.WriteLine(arg))
        try
            match (args.Length) with
            | 0 -> printUsage()
            | 1 -> configureSolutionFor "." args.[0] 
            | 2 -> configureSolutionFor args.[0] args.[1]
            | _ -> printUsage()
            0
        with ex ->
            Console.WriteLine ex
            1