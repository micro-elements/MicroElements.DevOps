///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////
#load ./scripts/init.cake

Task("Info")
.Does(() => {
    Information("MicroElements DevOps scripts.");
    ScriptArgs args = new ScriptArgs(Context, "./");
    FillProjectAttributes(args);
});

#load ./scripts/packaging.cake
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

Task("Default")
    .IsDependentOn("Package")
;

RunTarget(target);
