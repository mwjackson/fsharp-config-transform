namespace configManager

open System.Yaml

open NUnit.Framework
open FsUnit

open configManager.tokens

module tokens =

    [<TestFixture>] 
    module ``reading yaml files`` =
        let yamlConfig = read(Some("./testFiles/config.yaml"))

        [<Test>] 
        let ``should parse root property of a yaml file`` ()=
            yamlConfig.ContainsKey(new YamlScalar("token1")) |> should equal true
        [<Test>] 
        let ``should parse nested property of a yaml file`` ()=
            let token1 = yamlConfig.[new YamlScalar("token1")] :?> YamlMapping
            let env3 = token1.[new YamlScalar("env3")] :?> YamlScalar
            env3.Value |> should equal "value3"
        [<Test>]
        let ``should convert yamldocument to digestable format`` ()=
            let tokens = toTokens yamlConfig
            let firstToken = tokens |> Seq.sort |> Seq.head 
            firstToken.name |> should equal "token1"
            firstToken.envs |> Map.find "env2" |> should equal "value2"
        [<Test>]
        let ``should be able to look up token easily`` () =
            let tokens = toTokens yamlConfig
            let tokenValue = lookup tokens "token2" "env3"
            tokenValue.Value |> should equal "value6"
        [<Test>]
        let ``looking up token should default when environment not found`` () =
            let tokens = toTokens yamlConfig
            let tokenValue = lookup tokens "token1" "anotherEnv"
            tokenValue.Value |> should equal "defaultValue"
        [<Test>]
        let ``combining global and project tokens files should give a single collection`` () =
            let projectTokens = [ yield { name = "token1"; envs = ([ ("env1", "value1") ] |> Map.ofList) } ]
            let globalTokens = [ yield { name = "token3"; envs = ([ ("env3", "value9") ] |> Map.ofList) } ]
            let allTokens = combine globalTokens projectTokens
            (lookup allTokens "token1" "env1").Value |> should equal "value1"
            (lookup allTokens "token3" "env3").Value |> should equal "value9"
        [<Test>]
        let ``duplicate tokens in global and project should report error`` () =
            let projectTokens = [ yield { name = "token1"; envs = ([ ("env1", "value1") ] |> Map.ofList) } ]
            let globalTokens = [ yield { name = "token1"; envs = ([ ("env3", "value9") ] |> Map.ofList) } ]
            (fun () -> combine globalTokens projectTokens |> ignore ) |> should throw typeof<DuplicateTokensException>
 

