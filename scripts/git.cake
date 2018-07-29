#load imports.cake

public class GitInfo
{
    public string Branch {get; set;}
    public string CommitSha {get; set;}
    public string CommitSubject {get; set;}
    public string CommitShaShort {get; set;}
    public string CommitAuthorName {get; set;}
    public string CommitAuthorEmail {get; set;}
}

public static void PrintGitInfo(this ScriptArgs args)
{
    var context = args.Context;

    context.Information($"GetGitBranch1: {context.GetGitBranch()}");
    context.Information($"GetGitBranch2: {context.GetGitBranch2()}");
    context.Information($"GetGitBranch3: {context.GetGitBranch3()}");

    context.Information($"GetGitCommitSha: {context.GetGitCommitSha()}");
    context.Information($"GetGitCommitShaShort: {context.GetGitCommitShaShort()}");
    context.Information($"GetGitCommitAuthorName: {context.GetGitCommitAuthorName()}");
    context.Information($"GetGitCommitAuthorEmail: {context.GetGitCommitAuthorEmail()}");
    context.Information($"GetGitCommitSubject: {context.GetGitCommitSubject()}");
}

public static string GitCommand(this ICakeContext context, string command)
{
    context.Information($"Running 'git {command}'");
    var result = ProcessUtils.StartProcessAndReturnOutput(context, "git", command);
    context.Verbose($"ExitCode: {result.ExitCode}");
    context.Verbose($"Output: {result.Output}");
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

public static string GetGitBranch3(this ICakeContext context) =>
    context.GitCommand("rev-parse --abbrev-ref HEAD");

public static string GetGitLastChange(this ICakeContext context)
{
    var branchName = context.GitCommand(@"log -n 1 --format=format:$(_ShortShaFormat) ""version,props""");
    return branchName;
}

public static string GetGitCommitSha(this ICakeContext context) =>
    context.GitCommand("log --format=format:%H -n 1");

public static string GetGitCommitShaShort(this ICakeContext context) =>
    context.GitCommand("log --format=format:%h -n 1");

public static string GetGitCommitAuthorName(this ICakeContext context) =>
    context.GitCommand("log --format=format:%an -n 1");

public static string GetGitCommitAuthorEmail(this ICakeContext context) =>
    context.GitCommand("log --format=format:%ae -n 1");

public static string GetGitCommitSubject(this ICakeContext context) =>
    context.GitCommand("log --format=format:%s -n 1");


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