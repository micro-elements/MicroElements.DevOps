#load common.cake

/// <summary>
/// Gets merged <see cref="ScriptArgs.NugetSource"> value in form "--source Source1 --source Source2".
/// </summary>
public static string NugetSourcesArg(this ScriptArgs args) =>
    args.NugetSource.Values.Where(s => !string.IsNullOrEmpty(s)).Distinct().Aggregate("", (s, s1) => $@"{s} --source ""{s1}""");

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

/// <summary>
/// Alias for <see cref="BuildProjects">.
/// </summary>
public static ScriptArgs BuildSrc(this ScriptArgs args) => args.BuildProjects();

/// <summary>
/// Builds src dir.
/// <p>It's the main build method that builds projects with SourceLink.</p>
/// </summary>
/// <param name="args">ScriptArgs to use.</param>
public static ScriptArgs BuildProjects(this ScriptArgs args)
{
    var context = args.Context;

    // Delete old packages
    context.DeleteFiles($"{args.SrcDir}/**/*.nupkg");

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
        
        // Build project
        using(context.UseDiagnosticVerbosity())
        {
            context.DotNetCoreBuild(project.FullPath, settings);
        }

        // test sourcelink result
        if(args.UseSourceLink && args.TestSourceLink)
        {
            TestSourceLink(args, project);
        }
    }
    return args;
}

/// <summary>
/// Builds samples.
/// </summary>
/// <param name="args">ScriptArgs to use.</param>
/// <returns>ScriptArgs for chaining methods.</returns>
public static ScriptArgs BuildSamples(this ScriptArgs args)
{
    return args.BuildDirectory(args.RootDir/"samples");
}

/// <summary>
/// Builds all projects in directory.
/// </summary>
/// <param name="args">ScriptArgs to use.</param>
/// <returns>ScriptArgs for chaining methods.</returns>
public static ScriptArgs BuildDirectory(this ScriptArgs args, DirectoryPath directory)
{
    var context = args.Context;
    var settings = new DotNetCoreBuildSettings 
    {
        Configuration = args.Configuration
    };

    var projectsMask = $"{directory}/**/*.csproj";
    var projects = context.GetFiles(projectsMask).ToList();
    context.Information($"ProjectsMask: {projectsMask}, Found: {projects.Count} project(s).");
    foreach(var project in projects)
    {
        context.Information($"Building project: {project}");
        
        // Build project
        using(context.UseDiagnosticVerbosity())
        {
            context.DotNetCoreBuild(project.FullPath, settings);
        }
    }
    return args;
}

/// <summary>
/// Tests SourceLink.
/// </summary>
/// <param name="args">ScriptArgs to use.</param>
/// <param name="project">Project file path to test.</param>
/// <returns>ScriptArgs for chaining methods.</returns>
public static ScriptArgs TestSourceLink(this ScriptArgs args, FilePath project)
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
    return args;
}
