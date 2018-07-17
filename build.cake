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
    var description = System.IO.File.ReadAllText("./README.md");
    //todo: here must be more clever...
    description = description.Substring(0, description.IndexOf("## Tasks"));
    var releaseNotes = System.IO.File.ReadAllText("./CHANGELOG.md");
    var packSettings = new NuGetPackSettings()
    {
        Id = "MicroElements.DevOps",
        OutputDirectory = args.PackagesDir,
        BasePath = Directory("./"),
        ReleaseNotes = new string[] {releaseNotes},
        Description = description
    };
    CleanDirectory(args.ArtifactsDir);
    
    CopyDirectory("./resources", $"{args.ArtifactsDir}/resources");
    CopyDirectory("./scripts", $"{args.ArtifactsDir}/scripts");
    CopyDirectory("./templates", $"{args.ArtifactsDir}/templates");
 
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
