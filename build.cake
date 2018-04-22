///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
   // Executed BEFORE the first task.
   Information("Running tasks...");
});

Teardown(ctx =>
{
   // Executed AFTER the last task.
   Information("Finished running tasks.");
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Default")
.Does(() => {
   Information("Hello Cake!");
});

var root = Directory("./");

var repoDir = root;

// see: https://github.com/micro-elements/MicroElements.DevOps.Tutorial/blob/master/docs/01_project_structure.md
Task("CreateProjectStructure")
.Does(() => {
    var srcDir = repoDir + Directory("src");
    var testDir = repoDir + Directory("test");

    if(DirectoryExists(srcDir))
        Information("src already exists.");
    else
    {
        CreateDirectory(srcDir);
        Information("src created.");
    }

    if(DirectoryExists(testDir))
        Information("test already exists.");
    else
    {
        CreateDirectory(testDir);
        Information("test created.");
    }
});

Task("CheckOrDownloadGitIgnore")
.Description("Checks that gitignore exists. If not exists downloads from github")
.Does(() => {
    var gitIgnoreFile = root + File(".gitignore");
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
    var gitIgnoreFile = root + File(".gitignore");
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

Task("Init")
    .IsDependentOn("CreateProjectStructure")
    .IsDependentOn("GitIgnore");

RunTarget(target);