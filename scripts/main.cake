///////////////////////////////////////////////////////////////////////////////
// IMPORTS
///////////////////////////////////////////////////////////////////////////////

#load common.cake
#load init.cake
#load building.cake
#load testing.cake
#load packaging.cake
#load versioning.cake
#load conventions.cake

Information("MicroElements DevOps scripts.");

///////////////////////////////////////////////////////////////////////////////
// SCRIPT ARGS AND CONVENTIONS
///////////////////////////////////////////////////////////////////////////////

var rootDir = Argument("rootDir", "./");
ScriptArgs args = new ScriptArgs(Context, rootDir);
args.UseSingleComponentConventions();
args.Build();

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("CreateProjectStructure")
    .Does(() => CreateProjectStructure(args));

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

Task("AddCakeBootstrapFiles")
    .Does(() => AddCakeBootstrapFiles(args));

Task("AddChangeLog")
    .Does(() => AddChangeLog(args));

Task("Build")
    .Does(() => Build(args));

Task("Test")
    .Does(() => Test(args));

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
    .IsDependentOn("CreateProjectStructure")
    .IsDependentOn("CheckOrDownloadGitIgnore")
    .IsDependentOn("GitIgnoreAddCakeRule")
    .IsDependentOn("CreateProjects")
    .IsDependentOn("EditorConfig")
    .IsDependentOn("SourceLink")
    .IsDependentOn("CreateCommonProjectFiles")
    .IsDependentOn("AddTravisFile")
    .IsDependentOn("AddCakeBootstrapFiles")
    .IsDependentOn("AddChangeLog")
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
