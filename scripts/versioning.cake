#load common.cake

public class VersionInfo
{
    public string BranchName = "";

    public string CommitSha = "";

    public int BuildNumber = 0;

    public string VersionPrefix;//base version???

    public string VersionSuffix = "";

    public string ReleaseNotes = "";

    public bool IsRelease = true;

    public bool IsPullRequest = false;

    public string NuGetVersion => String.IsNullOrEmpty(VersionSuffix)? $"{VersionPrefix}": $"{VersionPrefix}-{VersionSuffix}";

    public string InformationalVersion => $"{VersionPrefix}.{VersionSuffix}.{BranchName}.{BuildNumber}.Sha.{CommitSha}";

    public string BranchNameShort => Versioning.CleanFileName(Versioning.TrimStdBranchPrefix(BranchName));

    public override string ToString() => NuGetVersion;
}

public class Versioning
{
    public static string FormatVersionProps(VersionInfo version)
    {
        var version_props = $@"<!-- This file can be overwritten by automation. -->
<Project>
  <PropertyGroup>
    <VersionPrefix>{version.VersionPrefix}</VersionPrefix>
    <VersionSuffix>{version.VersionSuffix}</VersionSuffix>
    <PackageReleaseNotes>{version.ReleaseNotes}</PackageReleaseNotes>

    <AssemblyVersion>$(VersionPrefix)</AssemblyVersion>
    <FileVersion>$(VersionPrefix)</FileVersion>
    <InformationalVersion>{version.InformationalVersion}</InformationalVersion>
  </PropertyGroup>
</Project>";
        return version_props;
    }

    public static VersionInfo ReadVersion(ICakeContext context, FilePath fileName)
    {
        VersionInfo version = new VersionInfo();
        var file = context.FileSystem.GetFile(fileName);
        if(!file.Exists)
            return version;

        string GetTagValue(string line, string tagName)
        {
            int i1 = line.IndexOf($"<{tagName}>") + $"<{tagName}>".Length;
            int i2 = line.IndexOf($"</{tagName}>");
            return line.Substring(i1, i2-i1);
        }

        foreach(var line in file.ReadLines(Encoding.UTF8))
        {
            if(line.Contains("<VersionPrefix>"))
            {
                version.VersionPrefix = GetTagValue(line, "VersionPrefix");
            }
            if(line.Contains("<VersionSuffix>"))
            {
                version.VersionSuffix = GetTagValue(line, "VersionSuffix");
            }
            if(line.Contains("<PackageReleaseNotes>"))
            {
                version.ReleaseNotes = GetTagValue(line, "PackageReleaseNotes");
            }
        }
        return version;
    } 

    public static VersionInfo GetVersionInfo(ICakeContext context, VersionInfo version = null)
    {
        version = version ?? new VersionInfo();

        if(context.BuildSystem().IsRunningOnJenkins)
        {
            context.Information("IsRunningOnJenkins");

            version.BuildNumber = context.Jenkins().Environment.Build.BuildNumber;
            version.BranchName = context.Jenkins().Environment.Repository.GitBranch;
            version.CommitSha = context.Jenkins().Environment.Repository.GitCommitSha;
        }
        else if(context.BuildSystem().IsRunningOnTravisCI)
        {
            context.Information("IsRunningOnTravisCI");

            version.BuildNumber = context.TravisCI().Environment.Build.BuildNumber;
            version.BranchName = context.TravisCI().Environment.Build.Branch;
            version.CommitSha = context.TravisCI().Environment.Repository.Commit;
        }
        else if(context.BuildSystem().IsRunningOnAppVeyor)
        {
            context.Information("IsRunningOnAppVeyor");

            version.BuildNumber = context.AppVeyor().Environment.Build.Number;
            version.BranchName = context.AppVeyor().Environment.Repository.Branch;
            version.CommitSha = context.AppVeyor().Environment.Repository.Commit.Id;
            version.IsPullRequest = context.AppVeyor().Environment.PullRequest.IsPullRequest;
        }
        else
        {
            var isLocalBuild = context.BuildSystem().IsLocalBuild;

            version.BranchName = context.GetGitBranch();
            version.CommitSha = context.GetGitCommit();

            context.Information($"GitBranch: {version.BranchName}");
            context.Information($"GitCommit: {version.CommitSha}");

        }

        return version;
    }

    public static string CleanFileName(string fileName, string replaceSymbol = "_")
        => string.Join(replaceSymbol, fileName.Split(System.IO.Path.GetInvalidFileNameChars()));

    public static string CleanVersionSuffix(string versionSuffix)
        => new string(versionSuffix.Select(c=>char.IsLetterOrDigit(c)? c : '.').ToArray());

    public static string TrimStdBranchPrefix(string branchName)
        => branchName
            .Replace("feature/", "")
            .Replace("hotfix/", "")
            .Replace("bugfix/", "")
            .Replace("release/", "");
}

public static void DoVersioning(this ScriptArgs args)
{
    //UseGitVersion, UseManualVersioning, UseGitHeight, UseCIVersioning
    var context = args.Context;
    bool useBuildNumberAsPatch = true;
    bool useManualVersioning = true;
    string prereleasePrefix = "unstable";//ci, preview-0000, beta-000

    VersionInfo version = args.Version;
    
    if(useManualVersioning)
        version = Versioning.GetVersionInfo(context, args.Version);

    version = version.SetIsReleaseAsGitFlow(args);

    if(useBuildNumberAsPatch && !useManualVersioning)
    {
        if(version.IsRelease && version.BuildNumber>0)
        {
            var semVerPatch = 0; //ParseSemVer(version.VersionPrefix);
            if(semVerPatch==0)
            {
                version.VersionPrefix = version.VersionPrefix+$".{version.BuildNumber}";
            }
        }
    }

    if(!useManualVersioning)
    {
        var prereleaseTag = "";
        if(!version.IsRelease)
            prereleaseTag = $"{prereleasePrefix}.{version.BuildNumber}";
        version.VersionSuffix = prereleaseTag;
    }

    // ChangeLog
    version.SetReleaseNotesFromChangeLog(args);

    // Format and write version.props
    var versionPropsFileName = args.KnownFiles.VersionProps.Value.FullPath;
    var versionPropsContent= Versioning.FormatVersionProps(version);
    System.IO.File.WriteAllText(versionPropsFileName, versionPropsContent);

    context.Information("VERSION_PROPS:");
    context.Information(versionPropsContent);
}

public static VersionInfo SetIsReleaseAsGitFlow(this VersionInfo version, ScriptArgs args)
{
    version.IsRelease = version.BranchName == "master" || version.BranchName.StartsWith("release/");
    return version;
}

public static VersionInfo SetReleaseNotesFromChangeLog(this VersionInfo version, ScriptArgs args)
{
    var changeLogFileName = args.KnownFiles.ChangeLog.Value.FullPath;
    if(System.IO.File.Exists(changeLogFileName))
        version.ReleaseNotes = System.IO.File.ReadAllText(changeLogFileName);
    return version;
}

public static string GitCommand(this ICakeContext context, string command)
{
    context.Information($"Running git {command}");
    var result = ProcessUtils.StartProcessAndReturnOutput(context, "git", command);
    context.Debug($"ExitCode: {result.ExitCode}");
    context.Information($"Output: {result.Output}");
    return (result.Output??"").Trim();
}

public static string GetGitBranch(this ICakeContext context)
{
    // Other variants: // https://stackoverflow.com/questions/6245570/how-to-get-the-current-branch-name-in-git
    var branchName = context.GitCommand("name-rev --name-only HEAD");
    return branchName;
}

public static string GetGitBranch2(this ICakeContext context)
{
    var result = context.GitCommand("branch");
    var branchName = result
        .SplitLines()
        .FirstOrDefault(line=>line.StartsWith("*"))
        .Substring(1)
        .Trim();
    return branchName;
}

public static string GetGitCommit(this ICakeContext context)
{
    //log --format=format:%h -n 1
    //log --format=format:%H -n 1
    //rev-parse master
    var commitSha = context.GitCommand("log --format=format:%H -n 1");
    return commitSha;
}

public static string GetFileHistory(this ICakeContext context)
{
    //git log --max-count 2 -- version.props
    //git log --max-count 2 --pretty=oneline -- version.props

    /*
    562cc83398f6667deb39f7c3ce3fd28510b5fd43 version in version.props
    fb86462fa8b02874613180586633be5ffc55e922 version
    */

    var versionFileName = "version.props";
    var result3 = context.GitCommand($"log --max-count 2 --pretty=oneline -- {versionFileName}");

    var revs = result3
        .SplitLines()
        .Select(line=> line.Split().First())
        .ToList();
        
    //git rev-list --count feature/jenkins5
    return "";
}
