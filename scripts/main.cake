///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

#load common.cake
#load init.cake
#load package.cake
#load versioning.cake

var rootDir         = Argument("rootDir", "./");
var buildDir        = Argument<string>("buildDir", null);
ScriptArgs args     = new ScriptArgs(Context, Directory(rootDir));

var target                  = ArgumentOrEnvVar(args, "target", "Default");
var configuration           = ArgumentOrEnvVar(args, "configuration", "Release", new []{"Release", "Debug"});
var projectName             = Argument(args, "projectName", args.Root.Path.GetDirectoryName());
var upload_nuget            = ArgumentOrEnvVar(args, "upload_nuget", "https://api.nuget.org/v3/index.json");
var upload_nuget_api_key    = ArgumentOrEnvVar(args, "upload_nuget_api_key", "00000000-0000-0000-0000-000000000000", secret: true);
var nuget_source1           = ArgumentOrEnvVar(args, "nuget_source1", "https://api.nuget.org/v3/index.json");
var nuget_source2           = ArgumentOrEnvVar<string>(args, "nuget_source2", null);
var nuget_source3           = ArgumentOrEnvVar<string>(args, "nuget_source3", null);

// any, linux-x64, win-x64, rhel.7-x64 // see: https://docs.microsoft.com/ru-ru/dotnet/core/rid-catalog
var runtimeName             = ArgumentOrEnvVar(args, "runtimeName", "any", new []{"any", "linux-x64", "win-x64"});

//////////////////////////////////////////////////////////////////////
// CONVENTIONS
//////////////////////////////////////////////////////////////////////

var version_props_file = args.Root + File("version.props");
var solutionFile = args.Root + File($"{projectName}.sln");

args.BuildDir = args.BuildDir ?? args.Root + Directory("build") + Directory(configuration);
var testResultsDir = buildDir + Directory("test-results");
var artifactsDir = buildDir + Directory("artifacts");

// Reading version
var versionInfo = Versioning.ReadVersion(Context, version_props_file);
Information($"VERSION:{versionInfo.VersionPrefix}");

//////////////////////////////////////////////////////////////////////
// TOOL ARGUMENTS 
//////////////////////////////////////////////////////////////////////

var nugetSourcesArg = new string[]{nuget_source1, nuget_source2, nuget_source3, upload_nuget}.Where(s => s != null).Aggregate("", (s, s1) => $@"{s} --source ""{s1}""");
var runtimeArg = runtimeName != "any" ? $" --runtime {runtimeName}" : "";

var sourceLinkArgs =" /p:SourceLinkCreate=true";
var noSourceLinkArgs =" /p:SourceLinkCreate=false";
var sourceLinkArgsFull =" /p:SourceLinkCreate=true /p:SourceLinkServerType={SourceLinkServerType} /p:SourceLinkUrl={SourceLinkUrl}";

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Info")
.Does(() => {
    Information("MicroElements DevOps scripts.");
    Information($"args.Root: {args.Root}");
    Information($"projectName: {projectName}");
    FillProjectAttributes(args);
});

// see: https://github.com/micro-elements/MicroElements.DevOps.Tutorial/blob/master/docs/01_project_structure.md
Task("CreateProjectStructure")
.Does(() => CreateProjectStructure(Context, args));

Task("CheckOrDownloadGitIgnore")
.Description("Checks that gitignore exists. If not exists downloads from github")
.Does(() => {
    var gitIgnoreFile = args.Root + File(".gitignore");
    var gitIgnoreFileName = gitIgnoreFile.Path.FullPath;
    var gitIgnoreExternalPath = "https://raw.githubusercontent.com/github/gitignore/master/VisualStudio.gitignore";

    if(FileExists(gitIgnoreFile.Path))
    {
        Information(".gitignore exists.");
    }
    else
    {
        DownloadFile(gitIgnoreExternalPath, gitIgnoreFile);
        Information($".gitignore downloaded from {gitIgnoreExternalPath}.");
    }
});

Task("GitIgnore")
.IsDependentOn("CheckOrDownloadGitIgnore")
.Description("Adds cake rules to gitignore")
.Does(() => {
    var gitIgnoreFile = args.Root + File(".gitignore");
    var gitIgnoreFileName = gitIgnoreFile.Path.FullPath;
    var cakeRule = "tools/**";
    var cakeRuleCommented = "# tools/**";

    if(FileExists(gitIgnoreFile.Path))
    {  
        var gitIgnoreText = System.IO.File.ReadAllText(gitIgnoreFileName);
        if(gitIgnoreText.Contains(cakeRule) && !gitIgnoreText.Contains(cakeRuleCommented))
        {
            Information(".gitignore already has cake rules.");
            return;
        }

        var message = $"uncommented {cakeRule} in .gitignore.";
        var gitIgnoreChanged = gitIgnoreText.Replace(cakeRuleCommented, cakeRule);
        if(gitIgnoreChanged==gitIgnoreText)
        {
            message = $"added {cakeRule} to .gitignore.";
            gitIgnoreChanged = gitIgnoreText + Environment.NewLine + cakeRule;
        }

        System.IO.File.WriteAllText(gitIgnoreFileName, gitIgnoreChanged);
        Information(message);
    }
    else
    {
        Information(".gitignore does not exists. Download it from 'https://github.com/github/gitignore/blob/master/VisualStudio.gitignore'");
    }
});

Task("CreateProjects")
.Does(() => {
    var projectDir = args.SrcDir + Directory(projectName);

    if(DirectoryExists(projectDir))
        Information("projectDir already exists.");
    else
    {
        CreateDirectory(projectDir);
        Information("projectDir created.");

        // dotnet new classlib
        DotNetCoreTool(projectDir.Path.FullPath, "new", 
            new ProcessArgumentBuilder().Append("classlib").Append($"--output {projectName}") );
    }

    var testProjectDir = args.TestDir + Directory(projectName+".Tests");

    if(DirectoryExists(testProjectDir))
        Information("testProjectDir already exists.");
    else
    {
        CreateDirectory(testProjectDir);
        Information("testProjectDir created.");

        // dotnet new test project
        DotNetCoreTool(testProjectDir.Path.FullPath, "new", 
            new ProcessArgumentBuilder().Append("xunit").Append($"--output {projectName}.Tests") );
    }

    var slnFile = args.Root + File($"{projectName}.sln");
    if(!FileExists(slnFile))
    {
        // dotnet new sln
        StartProcess("dotnet", new ProcessSettings()
            .UseWorkingDirectory(args.Root)
            .WithArguments(arguments=>arguments.Append($"new sln --name {projectName}")));

        // dotnet sln add
        StartProcess("dotnet", new ProcessSettings()
            .UseWorkingDirectory(args.Root)
            .WithArguments(arguments=>arguments.Append($"sln add {projectDir}/{projectName}.csproj")));
        
        // dotnet sln add
        StartProcess("dotnet", new ProcessSettings()
            .UseWorkingDirectory(args.Root)
            .WithArguments(arguments=>arguments.Append($"sln add {testProjectDir}/{projectName}.Tests.csproj")));
    }
    else
    {
        Information($"Solution file {slnFile} already exists.");
    }
});

Task("SourceLink")
.Does(() => {
    Information("Adding SourceLink.");

    string dirBuildPropsText = @"<Project>
  <ItemGroup>
    <PackageReference Include=""SourceLink.Create.CommandLine"" Version=""2.8.0"" PrivateAssets=""All"" /> 
  </ItemGroup>
  <PropertyGroup>
    <DebugType>embedded</DebugType>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>
</Project>";

    System.IO.File.WriteAllText(args.SrcDir + File("Directory.Build.props"), dirBuildPropsText); 
});

Task("EditorConfig")
.Does(() => {
    Information("Adding EditorConfig.");
    var file = args.ResourcesDir + File(".editorconfig");
    CopyFileToDirectory(file, args.Root);
});

Task("CreateCommonProjectFiles")
.Does(() => CreateCommonProjectFiles(args));

Task("Build")
.Does(() => {
    var settings = new DotNetCoreBuildSettings 
    { 
        Configuration = configuration,
        ArgumentCustomization =
          args => args
            .Append("/p:SourceLinkCreate=true")
    };
    var projects = GetFiles("./src/**/*.csproj");
    foreach(var project in projects)
    {
        DotNetCoreBuild(project.FullPath, settings);
    }
});

Task("Test")
.Does(() => {
    var test_projects = GetFiles("./test/**/*.csproj");
    foreach(var test_project in test_projects)
    {
        var testSettings = new DotNetCoreTestSettings()
        {
            Configuration = configuration,
            NoBuild = true
        };
        DotNetCoreTest(test_project.FullPath, testSettings);
    }
});

Task("Init")
    .IsDependentOn("Info")
    .IsDependentOn("CreateProjectStructure")
    .IsDependentOn("GitIgnore")
    .IsDependentOn("CreateProjects")
    .IsDependentOn("EditorConfig")
    .IsDependentOn("SourceLink")
    .IsDependentOn("CreateCommonProjectFiles")
;

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Test");

RunTarget(target);
