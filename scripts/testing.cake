#load common.cake

//////////////////////////////////////////////////////////////////////
// TEST METHODS
//////////////////////////////////////////////////////////////////////

/// <summary>
/// Returns all test projects.
/// </summary>
public static IReadOnlyList<FilePath> GetTestProjects(this ScriptArgs args)
{
    IReadOnlyList<FilePath> testProjects = Array.Empty<FilePath>();
    if(args.GetTestProjectsFunc!=null)
    {
        testProjects = args.GetTestProjectsFunc();
        args.Context.Information($"GetTestProjects by GetTestProjectsFunc: Found: {testProjects.Count} test project(s).");
    }
    else
    {
        var testProjectsMask = $"{args.TestDir}/**/*.csproj";
        testProjects = args.Context.GetFiles(testProjectsMask).ToList();
        args.Context.Information($"TestProjectsMask: {testProjectsMask}, Found: {testProjects.Count} test project(s).");
    }
    return testProjects;
}

/// <summary>
/// Runs dotnet test for each project in test directory.
/// </summary>
public static ScriptArgs RunTests(this ScriptArgs args)
{
    var context = args.Context;

    var testResultsDirArgs = $" --results-directory {args.TestResultsDir}";

    var testProjects = args.GetTestProjects();
  
    for (int testProjNum = 0; testProjNum < testProjects.Count; testProjNum++)
    {
        var testProject = testProjects[testProjNum];
        var logFilePath = $"test-result-{testProjNum+1}.trx";
        var loggerArgs = $" --logger trx;logfilename={logFilePath}";
        var testSettings = new DotNetCoreTestSettings()
        {
            Configuration = args.Configuration,
            ArgumentCustomization = arg => arg
                .Append(testResultsDirArgs)
                .Append(loggerArgs)
        };
        context.DotNetCoreTest(testProject.FullPath, testSettings);
    }
    return args;
}

/// <summary>
/// Uploads test results to AppVeyor.
/// see: https://www.appveyor.com/docs/running-tests/#uploading-xml-test-results
/// </summary>
public static ScriptArgs UploadTestResultsToAppVeyor(this ScriptArgs args)
{
    var appVeyor = args.Context.BuildSystem().AppVeyor;
    var testResultsMask = $"{args.TestResultsDir}/*.trx";
    var testResults = args.Context.GetFiles(testResultsMask);
    foreach (var testResult in testResults)
    {
        args.Context.Information($"Uploading test result {testResult} to AppVeyor");
        appVeyor.UploadTestResults(testResult, AppVeyorTestResultsType.MSTest);
    }
    return args;
}
