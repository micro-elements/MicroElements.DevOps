#load common.cake

public static ScriptArgs UseDefaultConventions(this ScriptArgs args)
{
    var context = args.Context;

    args.InitializeParams(new InitializeParamSettings{
        ReCreate = false,
        InitFromAttributes = true,
        InitFromArgs = true
    });

    args.Target.SetDefaultValue("Default").Build(args);
    
    args.Configuration
        .SetDefaultValue("Release")
        .SetValidValues("Release", "Debug")
        .Build(args);

    args.upload_nuget.SetDefaultValue("https://api.nuget.org/v3/index.json").Build(args);
    args.upload_nuget_api_key.SetIsSecret().Build(args);

    args.nuget_source1.SetDefaultValue("https://api.nuget.org/v3/index.json").Build(args);
    args.nuget_source2.Build(args);
    args.nuget_source3.Build(args);

    // any, linux-x64, win-x64, rhel.7-x64 // see: https://docs.microsoft.com/ru-ru/dotnet/core/rid-catalog
    args.RuntimeName
        .SetDefaultValue("any")
        .SetValidValues("any", "linux-x64", "win-x64")
        .Build(args);

    args.ProjectName.SetValue(a=>a.RootDir.Value.GetDirectoryName()).Build(args);
    args.SrcDir.SetValue(a=>a.RootDir/"src").Build(args);
    args.TestDir.SetValue(a=>a.RootDir/"test").Build(args);
    args.ToolsDir.SetValue(a=>a.RootDir/"tools").Build(args);

    args.ResourcesDir.SetValue(a=>GetDevopsToolDir(a).Combine("resources")).Build(args);
    args.TemplatesDir.SetValue(a=>GetDevopsToolDir(a).Combine("templates")).Build(args);

    var solutionName = new ScriptParam<string>("solutionName").SetFromArgs().SetValue(a=>$"{a.ProjectName}.sln").SetDefaultValue($"{args.ProjectName.Value}.sln").Build(args);
    args.AddParam(solutionName);
    args.KnownFiles.SolutionFile.SetValue(a=>a.RootDir.Value.CombineWithFilePath(solutionName.Value)).Build(args);

    args.BuildDir.SetValue(a=>a.RootDir/$"build/{a.Configuration}").Build(args);

    args.TestResultsDir.SetValue(a=>a.BuildDir/"test-results").Build(args);
    args.ArtifactsDir.SetValue(a=>a.BuildDir/"artifacts").Build(args);

    // KnownFiles
    args.KnownFiles.VersionProps
        .SetValue(a=>a.RootDir.Value.CombineWithFilePath("version.props")).Build(args);

    args.KnownFiles.ChangeLog
        .SetValue(a=>a.RootDir.Value.CombineWithFilePath("CHANGELOG.md")).Build(args);

    args.KnownFiles.Readme
        .SetValue(a=>a.RootDir.Value.CombineWithFilePath("README.md")).Build(args);

    args.Version = Versioning.ReadVersion(context, args.KnownFiles.VersionProps);
    context.Information($"VERSION: {args.Version.VersionPrefix}");

    args.UseSourceLink.SetDefaultValue(true).Build(args);
    args.TestSourceLink.SetDefaultValue(true).Build(args);

    return args;
}
