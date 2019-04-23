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
    .UseCoverlet()
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

Task("AddBuildProps")
    .Does(() => AddBuildProps(args));

Task("AddBuildPropsForTests")
    .Does(() => AddBuildPropsForTests(args));

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

Task("AddReadme")
    .Does(() => AddReadme(args));

Task("UpdateReadmeBadges")
    .Does(() => UpdateReadmeBadges(args));

Task("AddStyleCop")
    .Does(() => AddStyleCop(args));

Task("Build")
    .Does(() => BuildProjects(args));

Task("Test")
    .WithCriteria(()=>args.RunTests, "RunTests disabled")
    .Does(() => RunTests(args));

Task("UploadTestResultsToAppVeyor")
    .WithCriteria(()=>args.RunTests)
    .Does(() => UploadTestResultsToAppVeyor(args));

Task("CopyPackagesToArtifacts")
    .IsDependentOn("Build")
    .Does(() => CopyPackagesToArtifacts(args));

Task("UploadPackages")
    .Does(() => UploadPackagesIfNeeded(args));

Task("DoVersioning")
    .Does(() => DoVersioning(args));

Task("CodeCoverage")
    .Does(() => RunCoverage(args));

Task("UploadCoverageReportsToCoveralls")
    .Does(() => UploadCoverageReportsToCoveralls(args));

Task("Init")
    .IsDependentOn("CreateProjectStructure")
    .IsDependentOn("CheckOrDownloadGitIgnore")
    .IsDependentOn("GitIgnoreAddCakeRule")
    .IsDependentOn("CreateProjects")
    .IsDependentOn("EditorConfig")
    .IsDependentOn("AddBuildProps")
    .IsDependentOn("AddBuildPropsForTests")  
    .IsDependentOn("CreateCommonProjectFiles")
    .IsDependentOn("AddTravisFile")
    .IsDependentOn("AddAppVeyorFile")
    .IsDependentOn("AddCakeBootstrapFiles")
    .IsDependentOn("AddChangeLog")
    .IsDependentOn("AddReadme")
    .IsDependentOn("AddStyleCop")
;

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("CopyPackagesToArtifacts")
    .IsDependentOn("Test")
    ;

Task("Travis")
    .IsDependentOn("DoVersioning")
    .IsDependentOn("Build")
    .IsDependentOn("CopyPackagesToArtifacts")
    .IsDependentOn("Test")
    .IsDependentOn("CodeCoverage")
    .IsDependentOn("UploadCoverageReportsToCoveralls")
    .IsDependentOn("UploadPackages")
    ;

Task("AppVeyor")
    .IsDependentOn("Build")
    //.IsDependentOn("CopyPackagesToArtifacts")
    .IsDependentOn("Test")
    .IsDependentOn("UploadTestResultsToAppVeyor")
    ;

RunTarget(args.Target);
