///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

#load common.cake
#load init.cake
#load package.cake

var target          = Argument("target", "Default");
var configuration   = Argument("configuration", "Release");
var rootDir         = Argument("rootDir", "./");

ScriptArgs args = new ScriptArgs();
args.Root = Directory(rootDir);
args.SrcDir = args.Root + Directory("src");
args.TestDir = args.Root + Directory("test");

var tools = args.Root + Directory("tools");

var resources = tools + Directory("microelements.devops") + Directory("0.1.0") + Directory("resources");

var projectDirName = args.Root.Path.GetDirectoryName();
var projectName = Argument("projectName", projectDirName);

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Info")
.Does(() => {
    Information("MicroElements DevOps scripts.");
    Information($"args.Root: {args.Root}");
    Information($"projectDirName: {projectDirName}");
    Information($"projectName: {projectName}");
});

// see: https://github.com/micro-elements/MicroElements.DevOps.Tutorial/blob/master/docs/01_project_structure.md
Task("CreateProjectStructure")
.Does(() => {
    CreateProjectStructure(Context, args);
});

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
    }

    // dotnet new classlib
    DotNetCoreTool(projectDir.Path.FullPath, "new", 
        new ProcessArgumentBuilder().Append("classlib").Append($"--output {projectName}") );

    var testProjectDir = args.TestDir + Directory(projectName+".Tests");

    if(DirectoryExists(testProjectDir))
        Information("testProjectDir already exists.");
    else
    {
        CreateDirectory(testProjectDir);
        Information("testProjectDir created.");
    }

    // dotnet new test project
    DotNetCoreTool(testProjectDir.Path.FullPath, "new", 
        new ProcessArgumentBuilder().Append("xunit").Append($"--output {projectName}.Tests") );

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
    var file = resources + File(".editorconfig");
    CopyFileToDirectory(file, args.Root);
});

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
    .IsDependentOn("SourceLink");

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Test");

RunTarget(target);
