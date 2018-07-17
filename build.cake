///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////
#load ./scripts/imports.cake

ScriptArgs args = new ScriptArgs(Context);
args.UseDefaultConventions();
args.Build();

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Info")
.Does(() => {
    Information("MicroElements DevOps scripts.");
});

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

Task("CopyPackagesToArtifacts")
    .Does(() => CopyPackagesToArtifacts(args));

Task("UploadPackage")
    .Does(() => args.UploadPackages());

Task("Default")
    .IsDependentOn("Package")
;

Task("Travis")
    .IsDependentOn("Package")
    //.IsDependentOn("CopyPackagesToArtifacts")
    .IsDependentOn("UploadPackage")
;

Task("AppVeyor")
    .IsDependentOn("Package")
;

Task("TestDevOps")
    .IsDependentOn("Info")
;

RunTarget(args.Target);
