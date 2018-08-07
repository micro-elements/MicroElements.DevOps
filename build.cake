///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////
#load ./scripts/imports.cake

ScriptArgs args = new ScriptArgs(Context)
    .PrintHeader()
    .UseDefaultConventions()
    .Build();

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Info")
.Does(() => {
    // args.PrintParams();
    args.PrintGitInfo();
});

Task("AddReadme")
    .Does(() => UpdateReadmeBadges(args));

Task("Package")
.Does(() => {
    CleanDirectory(args.ArtifactsDir);

    var description = System.IO.File.ReadAllText("./README.md");
    //todo: here must be more clever...
    description = description.Substring(0, description.IndexOf("## Tasks"));
    var releaseNotes = System.IO.File.ReadAllText("./CHANGELOG.md");
    var buildDir = args.ArtifactsDir / "build";
    var packSettings = new NuGetPackSettings()
    {
        Id = "MicroElements.DevOps",
        OutputDirectory = args.PackagesDir,
        BasePath = buildDir,
        ReleaseNotes = new string[] {releaseNotes},
        Description = description
    };
    
    
    CopyDirectory("./resources", $"{buildDir}/resources");
    CopyDirectory("./scripts", $"{buildDir}/scripts");
    CopyDirectory("./templates", $"{buildDir}/templates");
 
    DotNetUtils.DotNetNuspecPack(Context, "MicroElements.DevOps.nuspec", packSettings);
});

Task("DoVersioning")
    .Does(() => DoVersioning(args));

Task("CopyPackagesToArtifacts")
    .Does(() => CopyPackagesToArtifacts(args));

Task("UploadPackage")
    .Does(() => args.UploadPackages());

Task("Default")
    .IsDependentOn("Package")
;

Task("Travis")
    .IsDependentOn("Info")
    .IsDependentOn("Package")
    //.IsDependentOn("CopyPackagesToArtifacts")
    .IsDependentOn("UploadPackage")
;

Task("AppVeyor")
    .IsDependentOn("DoVersioning")
    .IsDependentOn("Package")
;

Task("TestDevOps")
    .IsDependentOn("Info")
;

RunTarget(args.Target);
