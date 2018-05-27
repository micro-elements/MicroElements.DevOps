///////////////////////////////////////////////////////////////////////////////
// IMPORTS
///////////////////////////////////////////////////////////////////////////////

#load common.cake
#load init.cake
#load build.cake
#load package.cake
#load versioning.cake
#load scenarios.cake

Information("MicroElements DevOps scripts.");

///////////////////////////////////////////////////////////////////////////////
// SCRIPT ARGS AND CONVENTIONS
///////////////////////////////////////////////////////////////////////////////

var rootDir = Argument("rootDir", "./");
ScriptArgs args = new ScriptArgs(Context, rootDir);
args.Conventions = new DefaultConventions();
args.DefaultScenario();
args.Build();

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
    .IsDependentOn("Info")
    .IsDependentOn("CreateProjectStructure")
    .IsDependentOn("CheckOrDownloadGitIgnore")
    .IsDependentOn("GitIgnoreAddCakeRule")
    .IsDependentOn("CreateProjects")
    .IsDependentOn("EditorConfig")
    .IsDependentOn("SourceLink")
    .IsDependentOn("CreateCommonProjectFiles")
    .IsDependentOn("AddTravisFile")
    .IsDependentOn("AddCakeBootstrapFiles")
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
