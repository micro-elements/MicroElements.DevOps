#load imports.cake
#addin nuget:?package=Cake.Coverlet&version=2.2.1
//#addin nuget:?package=Cake.Coveralls
//#tool nuget:?package=coveralls.net&version=0.7.0

public static void RunCoverage(this ScriptArgs args)
{
    if(args.CoverageTask==null)
        args.CoverageTask = (a) => RunCoverlet(a);

    if(args.CoverageTask!=null && args.RunCodeCoverage)
    {
        args.CoverageTask(args);
    }
}

public static ScriptArgs UseCoverlet(this ScriptArgs args)
{
    args.CoverageTask = (a) => RunCoverlet(a);
    //needs <PackageReference Include="coverlet.msbuild" Version="2.6.1" PrivateAssets="All" /> 
    return args;
}

public static void RunCoverlet(this ScriptArgs args)
{
    var testSettings = new DotNetCoreTestSettings()
    {
        Configuration = args.Configuration,
        ArgumentCustomization = arg => arg
            .Append("/p:DebugType=portable")
    };

    var testProjects = args.GetTestProjects();
    for (int testProjNum = 0; testProjNum < testProjects.Count; testProjNum++)
    {
        var testProject = testProjects[testProjNum];

        var coveletSettings = new CoverletSettings {
            CollectCoverage = args.RunCodeCoverage,
            CoverletOutputFormat = CoverletOutputFormat.opencover,
            CoverletOutputDirectory = args.CoverageResultsDir,
            CoverletOutputName = $"coverage-result-{testProjNum+1}"
        };

        args.Context.DotNetCoreTest(testProject.FullPath, testSettings, coveletSettings);
    }   
}

public static void DotNetToolInstall(this ScriptArgs args, string toolName, string version)
{
    args.Context.StartProcessAndReturnOutput("dotnet", new ProcessArgumentBuilder()
            .Append($"tool install {toolName}")
            .Append($"--version {version}")
            .Append($"--tool-path \"{args.ToolsDir}\""), printOutput: true
            );
    //csmacnz.Coveralls.exe
    //args.Context.Tools.RegisterFile()
}

public static void PrintCoverallsInfo(this ScriptArgs args)
{
    var context = args.Context;

    //dotnet tool install coveralls.net --version 1.0.0 --tool-path tools
    args.DotNetToolInstall("coveralls.net", "1.0.0");

    args.Context.StartProcessAndReturnOutput($"{args.ToolsDir}/csmacnz.Coveralls",
        new ProcessArgumentBuilder().Append("--help"), printOutput: true);
}

public static void UploadCoverageReportsToCoveralls(this ScriptArgs args)
{
    var coverallsRepoToken = args.GetOrCreateParam<string>("COVERALLS_REPO_TOKEN")
        .SetIsSecret()
        .SetFromArgs()
        .Build(args);

    //dotnet tool install coveralls.net --version 1.0.0 --tool-path tools
    args.DotNetToolInstall("coveralls.net", "1.0.0");

    args.Context.StartProcessAndReturnOutput($"{args.ToolsDir}/csmacnz.Coveralls",
        new ProcessArgumentBuilder().Append("--version"), printOutput: true);

    if(!coverallsRepoToken.HasValue)
    {
        args.Context.Warning($"To upload coverage to coveralls.io you need to provide parameter COVERALLS_REPO_TOKEN");
        args.Context.Information("Upload coverage cancelled!");
        return;
    }

    var fileMask = $"{args.CoverageResultsDir}/*opencover.xml";
    var files = args.Context.GetFiles(fileMask).ToList();

    foreach(var file in files)
    {
        args.Context.Information($"Uploading report {file} to Coveralls.io" );

        /* 
        --commitId <commitId>                    The git commit hash for the coverage report.
        --commitBranch <commitBranch>            The git branch for the coverage report.
        --commitAuthor <commitAuthor>            The git commit author for the coverage report.
        --commitEmail <commitEmail>              The git commit author email for the coverage report.
        --commitMessage <commitMessage>          The git commit message for the coverage report.
        */

        var commitId = args.Version.CommitSha;
        var commitBranch = args.Version.BranchName;



        args.Context.StartProcessAndReturnOutput($"{args.ToolsDir}/csmacnz.Coveralls", new ProcessArgumentBuilder()
            .Append("--opencover")
            .Append("--repoToken ").AppendSecret(coverallsRepoToken)
            .Append($"--input {file.FullPath}"),
            printOutput: true);
    }   
}