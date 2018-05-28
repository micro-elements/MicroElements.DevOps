#load common.cake

//////////////////////////////////////////////////////////////////////
// TEST METHODS
//////////////////////////////////////////////////////////////////////

public static void Test(this ScriptArgs args)
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
