#I @"packages\FAKE.1.58.3\tools"
#r "FakeLib.dll"
open Fake
 
// Properties
let buildDir = @".\build\"
let nugetDir = @"..\nugetpackage" 

let appReferences  = !! @"FsRavenDbTools\FsRavenDbTools.fsproj"
 
Target "Clean" (fun _ ->
    CleanDirs [buildDir]
)

Target "BuildApp" (fun _ ->                     
    MSBuildDebug buildDir "Build" appReferences
        |> Log "AppBuild-Output: "
)


Target "CreateNuGet" (fun _ -> 
    let nugetLibsDir = nugetDir @@ @"lib\net40"
    XCopy (buildDir @@ @"Strangelights.FsRavenDbTools.dll") nugetLibsDir
    XCopy (buildDir @@ @"Strangelights.FsRavenDbTools.pdb") nugetLibsDir

    NuGet (fun p -> 
        {p with               
            Authors = ["Robert Pickering"]
            Project = "FsRavenDbTools"
            Description = "Help using RavenDB from F# (and Newtonsoft.Json)"
            Version = getBuildParam "version"
            ToolPath = @"..\tools\Nuget.exe"
            OutputPath = nugetDir
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey" }) @"FsRavenDbTools.nuspec"
)

"Clean"
    ==> "BuildApp"
    ==> "CreateNuGet"


RunParameterTargetOrDefault "target" "BuildApp"