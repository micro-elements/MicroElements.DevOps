#load common.cake

//////////////////////////////////////////////////////////////////////
// TOOL ARGUMENTS 
//////////////////////////////////////////////////////////////////////

public static void ToolArguments(ScriptArgs args)
{
    // todo: is there ability to do it best?
    var runtimeArg = args.RuntimeName != "any" ? $" --runtime {args.RuntimeName}" : "";
    var sourceLinkArgs =" /p:SourceLinkCreate=true";
    var noSourceLinkArgs =" /p:SourceLinkCreate=false";
    var sourceLinkArgsFull =" /p:SourceLinkCreate=true /p:SourceLinkServerType={SourceLinkServerType} /p:SourceLinkUrl={SourceLinkUrl}";
    var testResultsDirArgs = $" --results-directory {args.TestResultsDir}";
}

public static string NugetSourcesArg(this ScriptArgs args) => new string[]{args.nuget_source1, args.nuget_source2, args.nuget_source3, args.upload_nuget}.Where(s => !string.IsNullOrEmpty(s)).Distinct().Aggregate("", (s, s1) => $@"{s} --source ""{s1}""");

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

public static void Build(ScriptArgs args)
{
    var context = args.Context;

    var nugetSourcesArg = args.NugetSourcesArg();
    var sourceLinkArgs =" /p:SourceLinkCreate=true";
    var noSourceLinkArgs =" /p:SourceLinkCreate=false";
    var sourceLinkArgsFull =" /p:SourceLinkCreate=true /p:SourceLinkServerType={SourceLinkServerType} /p:SourceLinkUrl={SourceLinkUrl}";

    var settings = new DotNetCoreBuildSettings 
    { 
        Configuration = args.Configuration,
        NoIncremental = true,
        ArgumentCustomization = arg => arg
            .Append(nugetSourcesArg)
            .Append(noSourceLinkArgs)
    };

    var projectsMask = $"{args.SrcDir}/**/*.csproj";
    var projects = context.GetFiles(projectsMask).ToList();
    context.Information($"ProjectsMask: {projectsMask}, Found: {projects.Count} project(s).");
    foreach(var project in projects)
    {
        context.Information($"Building project: {project}");
        // Delete old packages
        context.DeleteFiles($"{args.SrcDir}/**/*.nupkg");
        context.DotNetCoreBuild(project.FullPath, settings);
    }
}
