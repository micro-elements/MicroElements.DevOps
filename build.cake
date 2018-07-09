///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////
#load ./scripts/imports.cake

ScriptArgs args = new ScriptArgs(Context);
args.UseDefaultConventions();
args.ArtifactsDir.SetValue(a=>a.RootDir/"artifacts").Build(args);
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
    var releaseNotes = System.IO.File.ReadAllText("./CHANGELOG.md");
    var packSettings = new NuGetPackSettings()
    {
        Id = "MicroElements.DevOps",
        OutputDirectory = args.ArtifactsDir,
        BasePath = Directory("./"),
        ReleaseNotes = new string[] {releaseNotes}
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

Task("TestDevOps")
    .IsDependentOn("Info")
;

RunTarget(args.Target);
