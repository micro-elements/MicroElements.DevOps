///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

#load common.cake
#load init.cake
#load package.cake
#load versioning.cake

var rootDir         = Argument(args, "rootDir", "./");
var buildDir        = Argument<string>(args, "buildDir", null);
ScriptArgs args     = new ScriptArgs(Context, Directory(rootDir));

var target                  = ArgumentOrEnvVar(args, "target", "Default");
var configuration           = ArgumentOrEnvVar(args, "configuration", "Release", new []{"Release", "Debug"});
var projectName             = Argument(args, "projectName", args.Root.Path.GetDirectoryName());
var upload_nuget            = ArgumentOrEnvVar(args, "upload_nuget", "https://api.nuget.org/v3/index.json");
var upload_nuget_api_key    = ArgumentOrEnvVar(args, "upload_nuget_api_key", "00000000-0000-0000-0000-000000000000", secret: true);
var nuget_source1           = ArgumentOrEnvVar(args, "nuget_source1", "https://api.nuget.org/v3/index.json");
var nuget_source2           = ArgumentOrEnvVar<string>(args, "nuget_source2", null);
var nuget_source3           = ArgumentOrEnvVar<string>(args, "nuget_source3", null);

// any, linux-x64, win-x64, rhel.7-x64 // see: https://docs.microsoft.com/ru-ru/dotnet/core/rid-catalog
var runtimeName             = ArgumentOrEnvVar(args, "runtimeName", "any", new []{"any", "linux-x64", "win-x64"});

//////////////////////////////////////////////////////////////////////
// CONVENTIONS
//////////////////////////////////////////////////////////////////////

var version_props_file = args.Root + File("version.props");
var solutionName = $"{projectName}.sln";
args.Params["solutionName"] = solutionName;
var solutionFile = args.Root + File(solutionName);

args.BuildDir = args.BuildDir ?? args.Root + Directory("build") + Directory(configuration);
var testResultsDir = buildDir + Directory("test-results");
var artifactsDir = buildDir + Directory("artifacts");

// Reading version
var versionInfo = Versioning.ReadVersion(Context, version_props_file);
Information($"VERSION:{versionInfo.VersionPrefix}");

//////////////////////////////////////////////////////////////////////
// TOOL ARGUMENTS 
//////////////////////////////////////////////////////////////////////

var nugetSourcesArg = new string[]{nuget_source1, nuget_source2, nuget_source3, upload_nuget}.Where(s => s != null).Aggregate("", (s, s1) => $@"{s} --source ""{s1}""");
var runtimeArg = runtimeName != "any" ? $" --runtime {runtimeName}" : "";

var sourceLinkArgs =" /p:SourceLinkCreate=true";
var noSourceLinkArgs =" /p:SourceLinkCreate=false";
var sourceLinkArgsFull =" /p:SourceLinkCreate=true /p:SourceLinkServerType={SourceLinkServerType} /p:SourceLinkUrl={SourceLinkUrl}";
var tesResultsDirArgs = $" --results-directory {testResultsDir}";

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Info")
.Does(() => {
    Information("MicroElements DevOps scripts.");
    Information($"args.Root: {args.Root}");
    Information($"projectName: {projectName}");
    args.DumpParams();
});

// see: https://github.com/micro-elements/MicroElements.DevOps.Tutorial/blob/master/docs/01_project_structure.md
Task("CreateProjectStructure")
.Does(() => CreateProjectStructure(Context, args));

Task("CheckOrDownloadGitIgnore")
.Description("Checks that gitignore exists. If not exists downloads from github")
.Does(() => CheckOrDownloadGitIgnore(args));

Task("GitIgnoreAddCakeRule")
.Description("Adds cake rules to gitignore")
.Does(() => GitIgnoreAddCakeRule(args));

Task("CreateProjects")
.Does(() => CreateProjects(args));

Task("SourceLink")
.Does(() => AddBuildProps(args));

Task("EditorConfig")
.Does(() => AddEditorConfig(args));

Task("CreateCommonProjectFiles")
.Does(() => CreateCommonProjectFiles(args));

Task("Build")
.Does(() => {
    var settings = new DotNetCoreBuildSettings 
    { 
        Configuration = configuration,
        ArgumentCustomization =
          args => args
            .Append("/p:SourceLinkCreate=true")
            .Append(nugetSourcesArg)
            .Append(noSourceLinkArgs)
    };

    var projectsMask = $"{args.SrcDir}/**/*.csproj";
    var projects = GetFiles(projectsMask).ToList();
    Information($"ProjectsMask: {projectsMask}, Found: {projects.Count} project(s).");
    foreach(var project in projects)
    {
        Information($"Building project: {project}");
        DotNetCoreBuild(project.FullPath, settings);
    }
});

Task("Test")
.Does(() => {
    var projectsMask = $"{args.TestDir}/**/*.csproj";
    var test_projects = GetFiles(projectsMask).ToList();
    Information($"TestProjectsMask: {projectsMask}, Found: {test_projects.Count} test project(s).");
    for (int testProjNum = 0; testProjNum < test_projects.Count; testProjNum++)
    {
        var test_project = test_projects[testProjNum];
        var logFilePath = $"test-result-{testProjNum+1}.trx";
        var loggerArgs = $" --logger trx;logfilename={logFilePath}";
        var testSettings = new DotNetCoreTestSettings()
        {
            Configuration = configuration,
            //NoBuild = true,
            ArgumentCustomization = args => args
                .Append(tesResultsDirArgs)
                .Append(loggerArgs)
        };
        DotNetCoreTest(test_project.FullPath, testSettings);
    }
});

Task("Init")
    .IsDependentOn("Info")
    .IsDependentOn("CreateProjectStructure")
    .IsDependentOn("CheckOrDownloadGitIgnore")
    .IsDependentOn("GitIgnoreAddCakeRule")
    .IsDependentOn("CreateProjects")
    .IsDependentOn("EditorConfig")
    .IsDependentOn("SourceLink")
    .IsDependentOn("CreateCommonProjectFiles")
;

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Test");

Task("Travis")
    .IsDependentOn("Build")
    .IsDependentOn("Test");

RunTarget(target);
