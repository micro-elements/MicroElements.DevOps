///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

//see: https://github.com/cake-build/cake/issues/1527
#load ./scripts/package.cake
Task("Package")
.Does(() => {
    var packSettings = new NuGetPackSettings()
    {
        Id = "MicroElements.DevOps",
        OutputDirectory = "./artifacts",
        BasePath = Directory("./")
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
