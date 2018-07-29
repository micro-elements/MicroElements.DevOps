#load common.cake

public static ScriptArgs UseDefaultConventions(this ScriptArgs args)
{
    var context = args.Context;

    args.InitializeParams(new InitializeParamSettings{
        ReCreate = false,
        InitFromAttributes = true,
        InitFromArgs = true
    });

    args.Target
        .SetEmptyValues("")//Jenkins can send empty string so we need to treat empty string as NoValue
        .SetDefaultValue("Default")
        .Build(args);

    args.Configuration
        .SetDefaultValue("Release")
        .SetValidValues("Release", "Debug")
        .Build(args);

    args.upload_nuget.SetDefaultValue("https://api.nuget.org/v3/index.json").Build(args);
    args.upload_nuget_api_key.SetIsSecret().Build(args);

    args.NugetSource.AddValue(a=>"https://api.nuget.org/v3/index.json").Build(args);

    // any, linux-x64, win-x64, rhel.7-x64 // see: https://docs.microsoft.com/ru-ru/dotnet/core/rid-catalog
    args.RuntimeName
        .SetDefaultValue("any")
        .SetValidValues("any", "linux-x64", "win-x64")
        .Build(args);

    args.ProjectName.SetValue(a=>a.RootDir.Value.GetDirectoryName()).Build(args);
    args.SrcDir.SetValue(a=>a.RootDir/"src").Build(args);
    args.TestDir.SetValue(a=>a.RootDir/"test").Build(args);
    args.ToolsDir.SetValue(a=>a.RootDir/"tools").Build(args);

    args.DevOpsRootDir.SetValue(GetDevopsToolDir).Build(args);
    args.DevOpsVersion.SetValue(GetVersionFromCommandLineArgs).Build(args);
    args.ResourcesDir.SetValue(a=>a.DevOpsRootDir/"resources").Build(args);
    args.TemplatesDir.SetValue(a=>a.DevOpsRootDir/"templates").Build(args);

    var solutionName = new ScriptParam<string>("solutionName").SetFromArgs().SetValue(a=>$"{a.ProjectName}.sln").SetDefaultValue($"{args.ProjectName.Value}.sln").Build(args);
    args.AddParam(solutionName);
    args.KnownFiles.SolutionFile.SetValue(a=>a.RootDir.Value.CombineWithFilePath(solutionName.Value)).Build(args);

    args.ArtifactsDir.SetValue(a=>a.RootDir/"artifacts").Build(args);
    args.TestResultsDir.SetValue(a=>a.ArtifactsDir/"test-results").Build(args);
    args.PackagesDir.SetValue(a=>a.ArtifactsDir/"packages").Build(args);
    args.CoverageResultsDir.SetValue(a=>a.ArtifactsDir/"coverage-results").Build(args);

    // KnownFiles
    args.KnownFiles.VersionProps
        .SetValue(a=>a.RootDir.Value.CombineWithFilePath("version.props")).Build(args);

    args.KnownFiles.ChangeLog
        .SetValue(a=>a.RootDir.Value.CombineWithFilePath("CHANGELOG.md")).Build(args);

    args.KnownFiles.Readme
        .SetValue(a=>a.RootDir.Value.CombineWithFilePath("README.md")).Build(args);

    args.VersionParam.SetValue(a=>Versioning.ReadVersion(a.Context, a.KnownFiles.VersionProps)).Build(args);
    context.Information($"VERSION: {args.Version.VersionPrefix}");

    args.UseSourceLink.SetDefaultValue(true).Build(args);
    args.TestSourceLink.SetDefaultValue(true).Build(args);

    return args;
}
