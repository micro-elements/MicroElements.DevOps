#load common.cake

public static string NugetSourcesArg(this ScriptArgs args) =>
    new string[]{args.nuget_source1, args.nuget_source2, args.nuget_source3}.Where(s => !string.IsNullOrEmpty(s)).Distinct().Aggregate("", (s, s1) => $@"{s} --source ""{s1}""");

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

public static void Build(ScriptArgs args)
{
    var context = args.Context;

    var nugetSourcesArg = args.NugetSourcesArg();
    var sourceLinkArgs = args.UseSourceLink? "/p:SourceLinkCreate=true" : "/p:SourceLinkCreate=false";

    var settings = new DotNetCoreBuildSettings 
    {
        Configuration = args.Configuration,
        NoIncremental = true,
        ArgumentCustomization = arg => arg
            .Append(nugetSourcesArg)
            .Append(sourceLinkArgs)
    };

    var projectsMask = $"{args.SrcDir}/**/*.csproj";
    var projects = context.GetFiles(projectsMask).ToList();
    context.Information($"ProjectsMask: {projectsMask}, Found: {projects.Count} project(s).");
    foreach(var project in projects)
    {
        context.Information($"Building project: {project}");
        
        // Delete old packages
        context.DeleteFiles($"{args.SrcDir}/**/*.nupkg");

        // Build project
        using(context.UseDiagnosticVerbosity())
        {
            context.DotNetCoreBuild(project.FullPath, settings);

            // test sourcelink result
            if(args.UseSourceLink && args.TestSourceLink)
            {
                TestSourceLink(args, project);
            }
        }
    }
}

public static void TestSourceLink(ScriptArgs args, FilePath project)
{
    var context = args.Context;
    var projectDir = project.GetDirectory().FullPath;
    var mask = $"{projectDir}/**/*.nupkg";
    var nupkgs = context.GetFiles(mask).ToList();
    foreach (var nupkg in nupkgs)
    {
        var result = ProcessUtils.StartProcessAndReturnOutput(context, "dotnet", "sourcelink", projectDir);
        var hasSourceLinkTool = result.ExitCode==0;
        if(hasSourceLinkTool)
        {
            // dotnet sourcelink test
            context.DotNetCoreTool(project.FullPath, "sourcelink",
                new ProcessArgumentBuilder()
                    .Append("test")
                    .Append(nupkg.FullPath));
        }
        else
        {
            context.Warning($"sourcelink tool is not available for project {project}");
            context.Warning($"To test source link add: <DotNetCliToolReference Include=\"dotnet-sourcelink\" Version=\"2.8.0\" />");
        }
    }
}
