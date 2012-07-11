namespace configManager.Tests

open configManager
open NUnit.Framework
open FsUnit

[<TestFixture>] 
type ``finding config files`` ()=
    let configManager = new configManager()
    [<Test>] member test.
        ``should return any config file`` ()=
        configManager.findConfigs "." |> should contain ".\configManager.exe.config"