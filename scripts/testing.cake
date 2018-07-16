#load common.cake

//////////////////////////////////////////////////////////////////////
// TEST METHODS
//////////////////////////////////////////////////////////////////////

/// <summary>
/// Runs dotnet test for each project in test directory.
/// </summary>
public static void RunTests(this ScriptArgs args)
{
    var context = args.Context;

    var testResultsDirArgs = $" --results-directory {args.TestResultsDir}";
    var projectsMask = $"{args.TestDir}/**/*.csproj";

    var test_projects = context.GetFiles(projectsMask).ToList();
    context.Information($"TestProjectsMask: {projectsMask}, Found: {test_projects.Count} test project(s).");
    for (int testProjNum = 0; testProjNum < test_projects.Count; testProjNum++)
    {
        var test_project = test_projects[testProjNum];
        var logFilePath = $"test-result-{testProjNum+1}.trx";
        var loggerArgs = $" --logger trx;logfilename={logFilePath}";
        var testSettings = new DotNetCoreTestSettings()
        {
            Configuration = args.Configuration,
            //NoBuild = true,
            ArgumentCustomization = arg => arg
                .Append(testResultsDirArgs)
                .Append(loggerArgs)
        };
        context.DotNetCoreTest(test_project.FullPath, testSettings);
    }
}

/// <summary>
/// Uploads test results to AppVeyor.
/// see: https://www.appveyor.com/docs/running-tests/#uploading-xml-test-results
/// </summary>
public static void UploadTestResultsToAppVeyor(this ScriptArgs args)
{
    var appVeyor = args.Context.BuildSystem().AppVeyor;
    var testResultsMask = $"{args.TestResultsDir}/*.trx";
    var testResults = args.Context.GetFiles(testResultsMask);
    foreach (var testResult in testResults)
    {
        appVeyor.UploadTestResults(testResult, AppVeyorTestResultsType.MSTest);
    }
}
