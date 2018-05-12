#load ./scripts/main.cake

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

//var target = Argument("target", "Info");

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

//see: https://github.com/cake-build/cake/issues/1527
#load ./scripts/package.cake
Task("PackCurrentProjectByNuspec")
.Does(() => {
    var packSettings = new NuGetPackSettings()
    {
        Id = "MicroElements.DevOps",
        Version = "0.2.0",
        OutputDirectory = "./artifacts",
        BasePath = Directory("./")
    };
    CleanDirectory("./artifacts");
    
    CopyDirectory("./resources", "./artifacts/resources");
    CopyDirectory("./scripts", "./artifacts/scripts");
    CopyDirectory("./templates", "./artifacts/templates");
 
    DotNetUtils.DotNetNuspecPack(Context, "MicroElements.DevOps.nuspec", packSettings);
});

RunTarget(target);
