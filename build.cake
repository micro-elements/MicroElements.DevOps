///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////
#load ./scripts/packaging.cake
#load ./scripts/conventions.cake

ScriptArgs args = new ScriptArgs(Context, "./");
args.UseSingleComponentConventions();
args.Build();

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
        OutputDirectory = "./artifacts",
        BasePath = Directory("./"),
        ReleaseNotes = new string[] {releaseNotes}
    };
    CleanDirectory("./artifacts");
    
    CopyDirectory("./resources", "./artifacts/resources");
    CopyDirectory("./scripts", "./artifacts/scripts");
    CopyDirectory("./templates", "./artifacts/templates");
 
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
    .IsDependentOn("CopyPackagesToArtifacts")
    .IsDependentOn("UploadPackage")
;

RunTarget(target);
