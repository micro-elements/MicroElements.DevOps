///////////////////////////////////////////////////////////////////////////////
// IMPORTS
///////////////////////////////////////////////////////////////////////////////

#load imports.cake

///////////////////////////////////////////////////////////////////////////////
// SCRIPT ARGS AND CONVENTIONS
///////////////////////////////////////////////////////////////////////////////

ScriptArgs args = new ScriptArgs(Context)
    .PrintHeader()
    .UseDefaultConventions()
    .Build();

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

Task("AddAppVeyorFile")
    .Does(() => AddAppVeyorFile(args));

Task("AddCakeBootstrapFiles")
    .Does(() => AddCakeBootstrapFiles(args));

Task("AddChangeLog")
    .Does(() => AddChangeLog(args));

Task("AddStyleCop")
    .Does(() => AddStyleCop(args));

Task("Build")
    .Does(() => Build(args));

Task("Test")
    .WithCriteria(()=>args.RunTests)
    .Does(() => RunTests(args));

Task("UploadTestResultsToAppVeyor")
    .WithCriteria(()=>args.RunTests)
    .Does(() => UploadTestResultsToAppVeyor(args));

Task("CopyPackagesToArtifacts")
    .IsDependentOn("Build")
    .Does(() => CopyPackagesToArtifacts(args));

Task("UploadPackages")
    .WithCriteria(()=>args.UploadPackages)
    .WithCriteria(()=>args.Version.IsRelease)
    .Does(() => UploadPackages(args));

Task("DoVersioning")
    .WithCriteria(()=>args.Version.IsRelease)
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
    .IsDependentOn("AddAppVeyorFile")
    .IsDependentOn("AddCakeBootstrapFiles")
    .IsDependentOn("AddChangeLog")
    .IsDependentOn("AddStyleCop")
;

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("CopyPackagesToArtifacts")
    ;

Task("Travis")
    .IsDependentOn("DoVersioning")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("CopyPackagesToArtifacts")
    .IsDependentOn("UploadPackages")
    ;

Task("AppVeyor")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("UploadTestResultsToAppVeyor")
    ;

RunTarget(args.Target);
