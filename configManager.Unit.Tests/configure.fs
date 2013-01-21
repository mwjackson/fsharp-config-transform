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

        [<SetUp>]
        let ``setup`` () =
            if (File.Exists expectedOutputFile) then
                File.Delete expectedOutputFile

        [<Test>]
        let ``generating config should swap tokens in the entire file`` ()=
            configureSolutionFor "./projectA" "env2"
            
            let actualConfig = File.ReadAllText expectedOutputFile
            Console.WriteLine (actualConfig)
            actualConfig |> should equal expectedConfig

        [<Test>]
        let ``configuring for all environments should swap tokens for all envs`` ()=
            configureSolutionFor "./projectA" "all"

            File.Exists("./projectA/src/test.local.config") |> should equal true
            File.Exists("./projectA/src/test.dev.config") |> should equal true
            File.Exists("./projectA/src/test.sit.config") |> should equal true
            File.Exists("./projectA/src/test.uat.config") |> should equal true
            File.Exists("./projectA/src/test.prod.config") |> should equal true