#load imports.cake

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
    <PackageReleaseNotes><![CDATA[${version.ReleaseNotes}]]></PackageReleaseNotes>

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
            if(line.Contains("<VersionPrefix>") && line.Contains("</VersionPrefix>"))
            {
                version.VersionPrefix = GetTagValue(line, "VersionPrefix");
            }
            if(line.Contains("<VersionSuffix>") && line.Contains("</VersionSuffix>"))
            {
                version.VersionSuffix = GetTagValue(line, "VersionSuffix");
            }

            // todo: this is not right (releaseNotes contains more than one line...)
            if(line.Contains("<PackageReleaseNotes>") && line.Contains("</PackageReleaseNotes>"))
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

            var travisCI = context.TravisCI().Environment;
            version.BuildNumber = travisCI.Build.BuildNumber;
            version.BranchName = travisCI.Build.Branch;
            version.CommitSha = travisCI.Repository.Commit;

            version.IsPullRequest = travisCI.Repository.PullRequest != null;
        }
        else if(context.BuildSystem().IsRunningOnAppVeyor)
        {
            context.Information("IsRunningOnAppVeyor");

            var appVeyor = context.AppVeyor().Environment;
            version.BuildNumber = appVeyor.Build.Number;
            version.BranchName = appVeyor.Repository.Branch;
            version.CommitSha = appVeyor.Repository.Commit.Id;
            var commitTimestamp = appVeyor.Repository.Commit.Timestamp;
            var commitAuthor = appVeyor.Repository.Commit.Author;
            var commitEmail = appVeyor.Repository.Commit.Email;
            var commitMessage = appVeyor.Repository.Commit.Message;
            var commitExtendedMessage = appVeyor.Repository.Commit.ExtendedMessage;

            version.IsPullRequest = appVeyor.PullRequest.IsPullRequest;
        }
        else
        {
            var isLocalBuild = context.BuildSystem().IsLocalBuild;

            version.BranchName = context.GetGitBranch();
            version.CommitSha = context.GetGitCommitSha();

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
    version.ReleaseNotes = args.GetReleaseNotes(opt => opt.FromChangelog().WithNumReleases(5));

    // Dump version values
    context.Information($"version.IsRelease: {version.IsRelease}");
    context.Information($"version.IsPullRequest: {version.IsPullRequest}");
    context.Information($"version.BranchName: {version.BranchName}");
    context.Information($"version.BuildNumber: {version.BuildNumber}");
    context.Information($"version.CommitSha: {version.CommitSha}");
    context.Information($"version.VersionPrefix: {version.VersionPrefix}");
    context.Information($"version.VersionSuffix: {version.VersionSuffix}");

    // Format and write version.props
    var versionPropsFileName = args.KnownFiles.VersionProps.Value.FullPath;
    var versionPropsContent= Versioning.FormatVersionProps(version);
    System.IO.File.WriteAllText(versionPropsFileName, versionPropsContent);

    // Dump version.props
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
