///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

#load common.cake
#load init.cake
#load package.cake
#load versioning.cake

Information("MicroElements DevOps scripts.");

var rootDir         = Argument("rootDir", "./");
var buildDir        = Argument<string>("buildDir", null);
ScriptArgs args     = new ScriptArgs(Context, rootDir, buildDir);

//////////////////////////////////////////////////////////////////////
// TOOL ARGUMENTS 
//////////////////////////////////////////////////////////////////////

var nugetSourcesArg = new string[]{args.nuget_source1, args.nuget_source2, args.nuget_source3, args.upload_nuget}.Where(s => !string.IsNullOrEmpty(s)).Aggregate("", (s, s1) => $@"{s} --source ""{s1}""");
var runtimeArg = args.RuntimeName != "any" ? $" --runtime {args.RuntimeName}" : "";
var sourceLinkArgs =" /p:SourceLinkCreate=true";
var noSourceLinkArgs =" /p:SourceLinkCreate=false";
var sourceLinkArgsFull =" /p:SourceLinkCreate=true /p:SourceLinkServerType={SourceLinkServerType} /p:SourceLinkUrl={SourceLinkUrl}";
var tesResultsDirArgs = $" --results-directory {args.TestResultsDir}";

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Info")
.Does(() => {
    Information("MicroElements DevOps scripts.");
    args.PrintParams();
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

Task("AddTravisFile")
.Does(() => AddTravisFile(args));

Task("Build")
.Does(() => {
    var settings = new DotNetCoreBuildSettings 
    { 
        Configuration = args.Configuration,
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
            Configuration = args.Configuration,
            //NoBuild = true,
            ArgumentCustomization = args => args
                .Append(tesResultsDirArgs)
                .Append(loggerArgs)
        };
        DotNetCoreTest(test_project.FullPath, testSettings);
    }
});

Task("CopyPackagesToArtifacts")
    .IsDependentOn("Build")
    .Does(() => CopyPackagesToArtifacts(args));

Task("UploadPackages")
    .WithCriteria(args.Version.IsRelease)
    .Does(() => UploadPackages(args));

Task("DoVersioning")
    .WithCriteria(args.Version.IsRelease)
    .Does(() => DoVersioning(args));

Task("Init")
    .IsDependentOn("Info")
    .IsDependentOn("CreateProjectStructure")
    .IsDependentOn("CheckOrDownloadGitIgnore")
    .IsDependentOn("GitIgnoreAddCakeRule")
    .IsDependentOn("CreateProjects")
    .IsDependentOn("EditorConfig")
    .IsDependentOn("SourceLink")
    .IsDependentOn("CreateCommonProjectFiles")
    .IsDependentOn("AddTravisFile")
;

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("CopyPackagesToArtifacts")
    ;

Task("Travis")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("CopyPackagesToArtifacts")
    .IsDependentOn("UploadPackages")
    ;

RunTarget(args.Target);
