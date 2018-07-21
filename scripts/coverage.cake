#load imports.cake
#addin nuget:?package=Cake.Coverlet

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
    //coverlet.msbuild

    return args;
}

public static void RunCoverlet(this ScriptArgs args)
{
    var testSettings = new DotNetCoreTestSettings {
    };

    var coveletSettings = new CoverletSettings {
        CollectCoverage = args.RunCodeCoverage,
        CoverletOutputFormat = CoverletOutputFormat.opencover,
        CoverletOutputDirectory = args.CoverageResultsDir,
        CoverletOutputName = $"results-{DateTime.UtcNow:dd-MM-yyyy-HH-mm-ss-FFF}"
    };

    var testProjects = args.GetTestProjects();
    for (int testProjNum = 0; testProjNum < testProjects.Count; testProjNum++)
    {
        var testProject = testProjects[testProjNum];
        args.Context.DotNetCoreTest(testProject.FullPath, testSettings, coveletSettings);
    }   
}