#load common.cake

public class VersionInfo
{
    public string BranchName = "";

    public string CommitSha = "";

    public int BuildNumber = 0;

    public string VersionPrefix;

    public string VersionSuffix = "";

    public string ReleaseNotes = "";

    public bool IsRelease = true;

    public bool IsPullRequest = false;

    public string NuGetVersion => String.IsNullOrEmpty(VersionSuffix)? $"{VersionPrefix}": $"{VersionPrefix}-{VersionSuffix}";

    public string InformationalVersion => $"{VersionPrefix}.{VersionSuffix}.{BranchName}.{BuildNumber}.Sha.{CommitSha}";
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

            var result = ProcessUtils.StartProcessAndReturnOutput(context, "git", "branch");
            version.BranchName = result.Output
                .SplitLines()
                .FirstOrDefault(line=>line.StartsWith("*"))
                .Substring(1)
                .Trim();
            context.Information($"BranchName: {version.BranchName}");

            var result2 = ProcessUtils.StartProcessAndReturnOutput(context, "git", $"rev-parse {version.BranchName}");
            version.CommitSha = result2.Output.Trim();
            context.Information($"SHA: {version.CommitSha}");

            //git log --max-count 2 -- version.json
            //git log --max-count 2 --pretty=oneline -- version.json

            /*
            562cc83398f6667deb39f7c3ce3fd28510b5fd43 version in version.json
            fb86462fa8b02874613180586633be5ffc55e922 version
            */

            var versionFileName = "version.json";
            var result3 = ProcessUtils.StartProcessAndReturnOutput(context, "git", $"log --max-count 2 --pretty=oneline -- {versionFileName}", true);
            context.Information($"result3: {result3.Output}");
            var revs = result.Output
                .SplitLines()
                .Select(line=> line.Split().First())
                .ToList();
                
            //git rev-list --count feature/jenkins5

        }

        return version;
    }
}

public static void DoVersioning(this ScriptArgs args)
{
    var context = args.Context;

    var version = Versioning.GetVersionInfo(context, args.Version);
    version.IsRelease = version.BranchName == "master" || version.BranchName.StartsWith("release/");

    if(version.IsRelease && version.BuildNumber>0)
    {
        var semVerPatch = 0; //ParseSemVer(version.VersionPrefix);
        if(semVerPatch==0)
        {
            version.VersionPrefix = version.VersionPrefix+$".{version.BuildNumber}";
        }
    }
    
    var prereleaseTag = "";
    if(!version.IsRelease)
        prereleaseTag = $"unstable.{version.BuildNumber}";
    version.VersionSuffix = prereleaseTag;
  
    var version_props_file = args.GetStringParam("version_props_file");
    var version_props_file_content = Versioning.FormatVersionProps(version);
    System.IO.File.WriteAllText(version_props_file, version_props_file_content);

    context.Information("VERSION_PROPS:");
    context.Information(version_props_file_content);
}
