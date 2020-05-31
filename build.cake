///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////
#load ./scripts/imports.cake

ScriptArgs args = new ScriptArgs(Context)
    .PrintHeader()
    .UseDefaultConventions()
    .Build();

args.RunCodeCoverage.SetValue(false).Build(args);

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

    var description = "DevOps scripts for CI and CD";
    var mainFeatures = args.GetMarkdownParagraph(args.KnownFiles.Readme.Value.FullPath, "Main features");
    var fullDescription = $"{description}\r\n{mainFeatures}";
    var releaseNotes = args.GetReleaseNotes(opt => opt.FromChangelog().WithNumReleases(5));
    var buildDir = args.ArtifactsDir / "build";
    var packSettings = new NuGetPackSettings()
    {
        Id = "MicroElements.DevOps",
        OutputDirectory = args.PackagesDir,
        BasePath = buildDir,
        ReleaseNotes = new string[] {releaseNotes},
        Description = fullDescription
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

Task("UploadPackages")
    .WithCriteria(()=>args.UploadPackages)
    .WithCriteria(()=>args.Version.IsRelease)
    .Does(() => UploadPackages(args));

Task("Default")
    .IsDependentOn("Package")
;

Task("Travis")
    .IsDependentOn("Info")
    .IsDependentOn("Package")
    .IsDependentOn("DoVersioning")
    .IsDependentOn("UploadPackages")
;

Task("AppVeyor")
    .IsDependentOn("DoVersioning")
    .IsDependentOn("Package")
;

Task("TestDevOps")
    .IsDependentOn("Info")
;

RunTarget(args.Target);
