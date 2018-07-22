#load imports.cake
#addin nuget:?package=Cake.Coverlet
#addin nuget:?package=Cake.Coveralls
#tool nuget:?package=coveralls.net&version=0.7.0

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
    //needs <PackageReference Include="coverlet.msbuild" Version="2.1.1" PrivateAssets="All" /> 
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

public static void UploadCoverageReportsToCoveralls(this ScriptArgs args)
{
    var fileMask = $"{args.CoverageResultsDir}/*opencover.xml";
    var files = args.Context.GetFiles(fileMask).ToList();
    foreach(var file in files)
    {
        args.Context.Information($"Uploading report {file} to Coveralls.io" );
        args.Context.CoverallsNet(file, CoverallsNetReportType.OpenCover);
    }   
}