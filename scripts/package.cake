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
        
        var nugetCsprojFileName = settings.OutputDirectory.CombineWithFilePath(context.File($"{settings.Id}.csproj"));
 
        string nugetCsproj = nugetCsprojTemplate
            .Replace("$NuspecFile$", nugetNuspecFileName)
            .Replace("$NuspecBasePath$", settings.BasePath.FullPath);

        context.Information(nugetCsproj);
        context.CreateDirectory(settings.OutputDirectory);

        System.IO.File.WriteAllText(nugetCsprojFileName.FullPath, nugetCsproj);
        var nuspecOutputPath = settings.OutputDirectory.CombineWithFilePath(System.IO.Path.GetFileName(nugetNuspecFileName));
        context.CopyFile(nugetNuspecFileName, nuspecOutputPath);

        var packSettings = new DotNetCorePackSettings()
        {
            OutputDirectory = settings.OutputDirectory,
            WorkingDirectory = "./"
        };
        context.DotNetCorePack(nugetCsprojFileName.FullPath, packSettings);

        //context.DeleteFile(nugetCsprojFileName);
    }
}