#load common.cake

public class DotNetUtils
{
    public static void DotNetNugetPack(ICakeContext context, NuGetPackSettings settings)
    {
        string nugetCsprojTemplate = 
        @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <NoBuild>true</NoBuild>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <NuspecFile>$NuspecFile$</NuspecFile>
    <NuspecBasePath>$NuspecBasePath$</NuspecBasePath>
  </PropertyGroup>
</Project>";

    string nugetNuspecTemplate = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>$id$</id>
    <version>$version$</version>
    <description>$description$</description>
    <authors>$authors$</authors>
  </metadata>
  <files>
    <file src=""$src$"" target=""$target$"" />
  </files>
</package>";

        var nugetCsprojFileName = settings.OutputDirectory.CombineWithFilePath(context.File($"{settings.Id}.csproj"));
        var nugetNuspecFileName = settings.OutputDirectory.CombineWithFilePath(context.File($"{settings.Id}.nuspec"));

        string nugetCsproj = nugetCsprojTemplate
            .Replace("$NuspecFile$", $"{settings.Id}.nuspec")
            .Replace("$NuspecBasePath$", settings.BasePath.FullPath);

        string nugetNuspec = nugetNuspecTemplate
            .Replace("$id$", settings.Id)
            .Replace("$version$", settings.Version)
            .Replace("$description$", settings.Description)
            .Replace("$authors$", String.Join(",", settings.Authors))
            .Replace("$src$", settings.Files.First().Source)
            .Replace("$target$", settings.Files.First().Target);

        System.IO.File.WriteAllText(nugetCsprojFileName.FullPath, nugetCsproj);
        System.IO.File.WriteAllText(nugetNuspecFileName.FullPath, nugetNuspec);

        var packSettings = new DotNetCorePackSettings()
        {
            OutputDirectory = settings.OutputDirectory
        };
        context.DotNetCorePack(nugetCsprojFileName.FullPath, packSettings);

        context.DeleteFile(nugetCsprojFileName);
        context.DeleteFile(nugetNuspecFileName);
    }

    public static void DotNetNuspecPack(ICakeContext context, string nugetNuspecFileName, NuGetPackSettings settings)
    {
        string nugetCsprojTemplate = System.IO.File.ReadAllText("./templates/nuget.csproj.xml");
        
        var nugetCsprojFileName = settings.BasePath.CombineWithFilePath(context.File($"{settings.Id}.csproj"));
 
        string nugetCsproj = nugetCsprojTemplate
            .Replace("$NuspecFile$", nugetNuspecFileName)
            .Replace("$NuspecBasePath$", settings.BasePath.FullPath);

        context.Information(nugetCsproj);
        context.CreateDirectory(settings.BasePath);
        context.CreateDirectory(settings.OutputDirectory);

        System.IO.File.WriteAllText(nugetCsprojFileName.FullPath, nugetCsproj);
        
        var nuspecOutputPath = settings.BasePath.CombineWithFilePath(System.IO.Path.GetFileName(nugetNuspecFileName));
        var nuspecContent = System.IO.File.ReadAllText(nugetNuspecFileName);
        var releaseNotes = settings.ReleaseNotes.NotNull().FirstOrDefault() ?? "";
        nuspecContent = nuspecContent
            .Replace("$releaseNotes$", $"<![CDATA[{releaseNotes}]]>")
            .Replace("$description$", $"<![CDATA[{settings.Description}]]>");
        System.IO.File.WriteAllText(nuspecOutputPath.FullPath, nuspecContent);

        var packSettings = new DotNetCorePackSettings()
        {
            OutputDirectory = settings.OutputDirectory,
            WorkingDirectory = settings.BasePath
        };

        using(context.UseDiagnosticVerbosity())
            context.DotNetCorePack(nugetCsprojFileName.FullPath, packSettings);
    }
}

public static void CopyPackagesToArtifacts(this ScriptArgs args)
{
    var context = args.Context;

    var fileMask = $"{args.SrcDir}/**/*.nupkg";
    var files = context.GetFiles(fileMask);
    context.EnsureDirectoryExists(args.PackagesDir);
    context.CleanDirectory(args.PackagesDir);
    using(context.UseDiagnosticVerbosity())
        context.CopyFiles(files, args.PackagesDir);
}

public static void UploadPackages(this ScriptArgs args)
{
    var context = args.Context;

    args.upload_nuget.ShouldHaveValue();
    args.upload_nuget_api_key.ShouldHaveValue();

    context.Information("UploadPackages started.");

    var packageMask = $"{args.PackagesDir}/*.nupkg";
    context.DotNetCoreNuGetPush(packageMask, new DotNetCoreNuGetPushSettings(){
        WorkingDirectory = args.PackagesDir,
        Source = args.upload_nuget,
        ApiKey = args.upload_nuget_api_key,
    });

    context.Information("UploadPackages finished.");
}

/// <summary>
/// Options for GetReleaseNotes.
/// </summary>
public class GetReleaseNotesOptions
{
    internal bool IsFromChangelog {get; private set;} = true;
    internal int NumReleases {get; private set;} = 0;

    public GetReleaseNotesOptions FromChangelog(bool fromChangelog = true){IsFromChangelog = fromChangelog; return this;}
    public GetReleaseNotesOptions WithNumReleases(int numReleases = 1){NumReleases = numReleases; return this;}
}

/// <summary>
/// Gets ReleaseNotes. By default uses Changelog.md.
/// </summary>
/// <param name="args">ScriptArgs to use.</param>
/// <param name="options">Options for GetReleaseNotes.</param>
/// <returns>Release notes.</returns>
public static string GetReleaseNotes(this ScriptArgs args, Func<GetReleaseNotesOptions,GetReleaseNotesOptions> options = null)
{
    GetReleaseNotesOptions opt = new GetReleaseNotesOptions();
    if(options!=null)
        opt = options(opt);
    string releaseNotes = "no release notes";
    if(opt.IsFromChangelog)
    {
        var changeLogPath = args.KnownFiles.ChangeLog.Value.FullPath;
        releaseNotes = System.IO.File.ReadAllText(changeLogPath);

        if(opt.NumReleases > 0)
        {
            bool IsVersionHeader(string line) => line.StartsWith("# ");
            StringBuilder result = new StringBuilder();
            var lines = releaseNotes.SplitLines();
            var numHeaders = lines.Count(IsVersionHeader);

            if(numHeaders > opt.NumReleases)
            {
                int headers = 0;
                foreach(var line in lines)
                {
                    if(IsVersionHeader(line))
                    {
                        headers++;
                        if(headers > opt.NumReleases)
                            break;
                    }

                    result.AppendLine(line);
                }

                result.AppendLine();

                string changeLogUrl = "https://github.com/{gitHubUser}/{gitHubProject}/blob/master/CHANGELOG.md".FillTags(args);
                result.AppendLine($"Full release notes can be found at: {changeLogUrl}");

                releaseNotes = result.ToString();
            }
        }
    }

    return releaseNotes;
}

public static string GetMarkdownParagraph(this ScriptArgs args, string filePath, string headerName)
{
    var content = System.IO.File.ReadAllText(filePath);

    StringBuilder result = new StringBuilder();
    var lines = content.SplitLines();
    bool readLine = false;
    foreach(var line in lines)
    {
        if(!readLine && line.StartsWith($"# {headerName}") || line.StartsWith($"## {headerName}"))
            readLine = true;
        else if(readLine && line.StartsWith($"#"))
            break;

        if(readLine)
            result.AppendLine(line);
    }

    return result.ToString();
}
