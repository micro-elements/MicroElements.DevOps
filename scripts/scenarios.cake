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
    args.nuget_source2           = args.Param<string>("nuget_source2").Build();
    args.nuget_source3           = args.Param<string>("nuget_source3").Build();

    // any, linux-x64, win-x64, rhel.7-x64 // see: https://docs.microsoft.com/ru-ru/dotnet/core/rid-catalog
    args.RuntimeName = args.Param<string>("runtimeName").DefaultValue("any")
                    .ValidValues("any", "linux-x64", "win-x64")
                    .Build();

    args.SrcDir              = args.Param<DirectoryPath>("SrcDir").WithValue(a=>a.RootDir.Value.Combine("src")).Build(args);
    args.TestDir             = args.Param<DirectoryPath>("TestDir").WithValue(a=>a.RootDir.Value.Combine("test")).Build(args);
    args.ToolsDir            = args.Param<DirectoryPath>("ToolsDir").WithValue(a=>a.RootDir.Value.Combine("tools")).Build(args);

    DirectoryPath GetDevopsToolDir(ScriptArgs a)
    {
        var devops_version       = GetVersionFromCommandLineArgs(a.Context);
        var devops_tool_dir      = a.ToolsDir.Value.Combine("microelements.devops").Combine(devops_version);
        return devops_tool_dir;
    }

    args.ResourcesDir        = args.Param<DirectoryPath>("ResourcesDir").WithValue(a=>GetDevopsToolDir(a).Combine("resources")).Build(args);
    args.TemplatesDir        = args.Param<DirectoryPath>("TemplatesDir").WithValue(a=>GetDevopsToolDir(a).Combine("templates")).Build(args);

    var solutionName = args.Param<string>("solutionName").WithValue(conventions.GetSolutionName).DefaultValue($"{args.ProjectName.Value}.sln").Build(args);
    var solutionFile = args.Param<string>("solutionFile").WithValue(conventions.GetSolutionFileName).Build();

    args.BuildDir = args.Param<DirectoryPath>("BuildDir").WithValue(a=>a.RootDir.Value.Combine("build") + context.Directory(args.Configuration.Value)).Build(args);
    args.TestResultsDir = args.Param<DirectoryPath>("TestResultsDir").WithValue(args.BuildDir + context.Directory("test-results")).Build(args);
    args.ArtifactsDir = args.Param<DirectoryPath>("ArtifactsDir").WithValue(args.BuildDir + context.Directory("artifacts")).Build(args);

    // KnownFiles
    args.KnownFiles.VersionProps = args.Param<FilePath>("KnownFiles.VersionProps")
        .WithValue((a)=>a.RootDir.Value.CombineWithFilePath("version.props")).Build(args);

    args.KnownFiles.ChangeLog = args.Param<FilePath>("KnownFiles.ChangeLog")
        .WithValue((a)=>a.RootDir.Value.CombineWithFilePath("CHANGELOG.md")).Build(args);

    args.KnownFiles.Readme = args.Param<FilePath>("KnownFiles.Readme")
        .WithValue((a)=>a.RootDir.Value.CombineWithFilePath("README.md")).Build(args);

    args.Version = Versioning.ReadVersion(context, args.KnownFiles.VersionProps);

    return args;
}
