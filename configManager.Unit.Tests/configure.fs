namespace configManager

open System
open System.IO

open NUnit.Framework
open FsUnit

open configManager.configure

module configure =

    [<TestFixture>] 
    module ``end to end`` =
        let expectedConfig = File.ReadAllText("./projectA/expectedOutput.config")
        let expectedOutputFile = "./projectA/src/test.config"

        [<Test>]
        let ``generating config should swap tokens in the entire file`` ()=
            if (File.Exists expectedOutputFile) then
                File.Delete expectedOutputFile

            configureSolutionFor "./projectA" "env2"
            
            let actualConfig = File.ReadAllText expectedOutputFile
            Console.WriteLine (actualConfig)
            actualConfig |> should equal expectedConfig
