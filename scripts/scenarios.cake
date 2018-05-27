#load common.cake

public static ScriptArgs DefaultScenario(this ScriptArgs args)
{
    var context = args.Context;
    var conventions = args.Conventions;

    args.Target                  = args.Param<string>("Target").DefaultValue("Default").Build();
    args.Configuration           = args.Param<string>("Configuration").DefaultValue("Release").ValidValues("Release", "Debug").Build();
    args.ProjectName             = args.Param<string>("ProjectName").WithValue(conventions.GetProjectName).Build();

    args.upload_nuget            = args.Param<string>("upload_nuget").DefaultValue("https://api.nuget.org/v3/index.json").Build();
    args.upload_nuget_api_key    = args.Param<string>("upload_nuget_api_key").DefaultValue("00000000-0000-0000-0000-000000000000").IsSecret().Build();
    args.nuget_source1           = args.Param<string>("nuget_source1").DefaultValue("https://api.nuget.org/v3/index.json").Build();
    args.nuget_source2           = args.Param<string>("nuget_source1").Build();
    args.nuget_source3           = args.Param<string>("nuget_source1").Build();

    // any, linux-x64, win-x64, rhel.7-x64 // see: https://docs.microsoft.com/ru-ru/dotnet/core/rid-catalog
    args.RuntimeName = args.Param<string>("runtimeName").DefaultValue("any")
                    .ValidValues("any", "linux-x64", "win-x64")
                    .Build();

    args.SrcDir              = args.RootDir + context.Directory("src");
    args.TestDir             = args.RootDir + context.Directory("test");
    args.ToolsDir            = args.RootDir + context.Directory("tools");
    var devops_version       = GetVersionFromCommandLineArgs(context);
    var devops_tool_dir      = args.ToolsDir + context.Directory("microelements.devops") + context.Directory(devops_version);
    args.ResourcesDir        = devops_tool_dir + context.Directory("resources");
    args.TemplatesDir        = devops_tool_dir + context.Directory("templates");

    var solutionName = args.Param<string>("solutionName").WithValue(conventions.GetSolutionName).DefaultValue($"{args.ProjectName.Value}.sln").Build();
    var solutionFile = args.Param<string>("solutionFile").WithValue(conventions.GetSolutionFileName).Build();

    args.BuildDir = args.BuildDir ?? args.RootDir + context.Directory("build") + context.Directory(args.Configuration.Value);
    args.TestResultsDir = args.BuildDir + context.Directory("test-results");
    args.ArtifactsDir = args.BuildDir + context.Directory("artifacts");

    var version_props_file = 
        args.Param<FilePath>("version_props_file")
        .WithValue((a)=>a.RootDir + context.File("version.props"))
        .Build();

    args.Version = Versioning.ReadVersion(context, version_props_file);
    return args;
}
