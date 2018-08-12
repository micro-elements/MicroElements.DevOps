#load versioning.cake
#load scriptParam.cake
#load functional.cake

using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

// todo: dependency
// done: auto creation
// todo: remove versioning.cake dependency
// done: factory to param
// DirectoryPath and FilePath ext
// done: DirectoryPath param!!!
// done: add resources dirs
// done: --devopsRoot --devopsVersion

/// <summary>
/// Converts value to ParamValue.
/// </summary>
public static ParamValue<T> ToParamValue<T>(this T value, ParamSource source = ParamSource.Conventions)
    => new ParamValue<T>(value, source);

public static Type CakeGlobalType() => typeof(ScriptArgs).DeclaringType.GetTypeInfo();

public static ParamValue<T> ArgumentOrEnvVar<T>(this ICakeContext context, string name)
{
    if(context.HasArgument(name))
        return new ParamValue<T>(context.Argument<T>(name, default(T)), ParamSource.CommandLine);
    if(context.HasEnvironmentVariable(name))
        return new ParamValue<T>((T)Convert.ChangeType(context.EnvironmentVariable(name), typeof(T)), ParamSource.EnvironmentVariable);
    return new ParamValue<T>(default(T), ParamSource.NoValue);
}

public static string NormalizePath(this string path) => path.ToLowerInvariant().Replace('\\', '/').TrimEnd('/');

public static string GetVersionFromCommandLineArgs(ScriptArgs args)
{
    var context = args.Context;
    var commandLineArgs = System.Environment.GetCommandLineArgs();
    context.Debug("CommandLineArgs: "+System.String.Join(" ", commandLineArgs));

    string devops_version = "";
    foreach(var arg in commandLineArgs.Select(NormalizePath))
    {
        if(arg.Contains("tools") && arg.Contains("microelements.devops"))
        {
            //C:\Projects\ProjName\tools\microelements.devops\0.2.0\scripts\main.cake
            var segments = context.File(arg).Path.Segments;
            int index = System.Array.IndexOf(segments, "microelements.devops");
            if(index>0 && index<segments.Length-1)
            {
                devops_version = segments[index+1];
                break;
            }
        }
    }

    return devops_version;
}

public static DirectoryPath GetDevopsToolDir(this ScriptArgs args)
{
    var devops_version = GetVersionFromCommandLineArgs(args);
    var devops_tool_dir = args.ToolsDir/$"microelements.devops/{devops_version}";
    return devops_tool_dir;
}

public static string GetTemplate(this ScriptArgs args, string fileName)
{
    var templateFileName = args.Context.File(fileName).Path;

    bool found = false;
    if(templateFileName.IsRelative)
    {
        foreach (var templateDir in args.TemplatesDir.Values)
        {
            var fullTemplateFileName = templateDir.CombineWithFilePath(templateFileName);
            if(System.IO.File.Exists(fullTemplateFileName.FullPath))
            {
                templateFileName = fullTemplateFileName;
                found = true;
                break;
            }
        }
    }

    if(!found)
    {
        throw new Exception($"Template {fileName} not found in Template dirs: {args.TemplatesDir.FormattedValue}");
    }

    string templateText = System.IO.File.ReadAllText(templateFileName.FullPath);
    return templateText;
}

public static string GetResource(this ScriptArgs args, string fileName)
{
    var resourceFileName = args.Context.File(fileName).Path;
    resourceFileName = resourceFileName.IsRelative? args.ResourcesDir.Value.CombineWithFilePath(resourceFileName) : resourceFileName;
    string resourceText = System.IO.File.ReadAllText(resourceFileName.FullPath);
    return resourceText;
}

public static void AddFileFromResource(this ScriptArgs args, string name, DirectoryPath destinationDir, string destinationName = null)
{
    var context = args.Context;
    var destinationFile = destinationDir.CombineWithFilePath(destinationName??name);

    if(context.FileExists(destinationFile))
        context.Information($"{destinationFile} file already exists.");
    else
    {
        var content = args.GetResource($"{name}");
        System.IO.File.WriteAllText(destinationFile.FullPath, content);
        context.Information($"{destinationFile} created.");
    }
}

public class AddFileOptions
{
    public string DestinationName {get; private set;}
    public Dictionary<string, string> Replacements {get; private set;} = new Dictionary<string, string>();
    public bool DoFillFromScriptArgs {get; private set;} = false;
    public bool DoReplaceFileIfExists {get; private set;} = false;

    public AddFileOptions SetDestinationName(string destinationName)
    {
        DestinationName = destinationName;
        return this;
    }
    public AddFileOptions Replace(string placeholder, string replacement)
    {
        Replacements.Add(placeholder, replacement);
        return this;
    }
    public AddFileOptions FillFromScriptArgs(bool fillFromScriptArgs = true)
    {
        DoFillFromScriptArgs = fillFromScriptArgs;
        return this;
    }
    public AddFileOptions ReplaceFileIfExists(bool replaceFileIfExists = true)
    {
        DoReplaceFileIfExists = replaceFileIfExists;
        return this;
    }
}

public static string AddFileFromTemplate(
    this ScriptArgs args,
    string templateFileName,
    DirectoryPath destinationDir,
    Func<AddFileOptions,AddFileOptions> opt)
{
    AddFileOptions options = new AddFileOptions();
    options = opt(options);
    return AddFileFromTemplate(args, templateFileName, destinationDir, options);
}

public static string AddFileFromTemplate(
    this ScriptArgs args,
    string templateFileName,
    DirectoryPath destinationDir,
    AddFileOptions options = null)
{
    var context = args.Context;
    options = options ?? new AddFileOptions();
    var destinationFile = destinationDir.CombineWithFilePath(options.DestinationName??templateFileName);

    string content = string.Empty;
    if(context.FileExists(destinationFile) && !options.DoReplaceFileIfExists)
        context.Information($"{destinationFile} file already exists.");
    else
    {
        content = args.GetTemplate(templateFileName);
        if(options.Replacements.Any())
        {
            foreach (var replacement in options.Replacements)
            {
                content = content.Replace(replacement.Key, replacement.Value);
            }
        }
        if(options.DoFillFromScriptArgs)
        {
            content = FillTags(content, args);
        }
        System.IO.File.WriteAllText(destinationFile.FullPath, content);
        context.Information($"{destinationFile} created.");
    }

    return content;
}

public static string FillTags(this string inputTemplate, ScriptArgs args)
{
    if(inputTemplate.Contains("gitHubUser"))
        args.FillProjectAttributes();

    foreach (var key in args.ParamKeys)
    {
        var value = args.GetStringParam(key);
        inputTemplate = inputTemplate.Replace($"${key}$", $"{value}");
        inputTemplate = inputTemplate.Replace($"{{{key}}}", $"{value}");
        inputTemplate = inputTemplate.Replace($"<{key}></{key}>", $"<{key}>{value}</{key}>");
    }
    return inputTemplate;
}

public static T CheckNotNull<T>(this T value, string paramName)
{
    if(value == null)
        throw new ArgumentNullException(paramName??"value");
    return value;
}

public static class ProcessUtils
{
    public static (int ExitCode, string Output) StartProcessAndReturnOutput(
        ICakeContext context,
        FilePath fileName,
        ProcessArgumentBuilder args,
        string workingDirectory = null,
        bool printOutput = false)
    {
        if(printOutput)
            context.Information($"{fileName} {args.Render()}");
        
        var processSettings = new ProcessSettings { Arguments = args, RedirectStandardOutput = true };
        if(workingDirectory!=null)
            processSettings.WorkingDirectory = workingDirectory;
        IEnumerable<string> redirectedStandardOutput;
        var exitCodeWithArgument = context.StartProcess(fileName, processSettings, out redirectedStandardOutput );

        StringBuilder outputString = new StringBuilder();
        foreach(var line in redirectedStandardOutput)
        {
            if(printOutput)
                context.Information(line);
            outputString.AppendLine(line);
        }
        return (exitCodeWithArgument, outputString.ToString());
    }
}

public static (int ExitCode, string Output) StartProcessAndReturnOutput(
    this ICakeContext context,
    FilePath fileName,
    ProcessArgumentBuilder args,
    string workingDirectory = null,
    bool printOutput = false)
{
    if(printOutput)
        context.Information($"{fileName} {args.RenderSafe()}");
    
    var processSettings = new ProcessSettings { Arguments = args, RedirectStandardOutput = true };
    if(workingDirectory!=null)
        processSettings.WorkingDirectory = workingDirectory;
    IEnumerable<string> redirectedStandardOutput;
    var exitCodeWithArgument = context.StartProcess(fileName, processSettings, out redirectedStandardOutput);

    StringBuilder outputString = new StringBuilder();
    foreach(var line in redirectedStandardOutput)
    {
        if(printOutput)
            context.Information(line);
        outputString.AppendLine(line);
    }
    return (exitCodeWithArgument, outputString.ToString());
}

/// <summary>
/// Temporary sets logging verbosity.
/// </summary>
/// <example>
/// <code>
/// // Temporary sets logging verbosity to Diagnostic.
/// using(context.UseVerbosity(Verbosity.Diagnostic))
/// {
///     context.DotNetCoreBuild(project, settings);
/// }
/// </code>
/// </example>
public static VerbosityChanger UseVerbosity(this ICakeContext context, Verbosity newVerbosity) =>
     new VerbosityChanger(context.Log, newVerbosity);

/// <summary>
/// Temporary sets logging verbosity to Diagnostic.
/// </summary>
/// <example>
/// <code>
/// // Temporary sets logging verbosity to Diagnostic.
/// using(context.UseDiagnosticVerbosity())
/// {
///     context.DotNetCoreBuild(project, settings);
/// }
/// </code>
/// </example>
public static VerbosityChanger UseDiagnosticVerbosity(this ICakeContext context) =>
    context.UseVerbosity(Verbosity.Diagnostic);

/// <summary>
/// Disposable VerbosityChanger. On dispose old verbosity returns. 
/// </summary>
public class VerbosityChanger : IDisposable
{
    ICakeLog _log;
    Verbosity _oldVerbosity;

    public VerbosityChanger(ICakeLog log, Verbosity newVerbosity)
    {
        _log = log;
        _oldVerbosity = log.Verbosity;
        _log.Verbosity = newVerbosity;
    }

    public void Dispose() => _log.Verbosity = _oldVerbosity;
}

/// <summary>
/// Installs tool with standard cake mechanism.
/// </summary>
public static void RequireTool(this ScriptArgs args, string tool)
{
    var scriptPath = args.Context.MakeAbsolute(args.Context.File(string.Format("./{0}.cake", Guid.NewGuid())));
    try
    {
        System.IO.File.WriteAllText(scriptPath.FullPath, tool);

        var arguments = new Dictionary<string, string>();
        args.Context.CakeExecuteScript(scriptPath,
            new CakeSettings { Arguments = arguments });
    }
    finally
    {
        if (args.Context.FileExists(scriptPath))
        {
            args.Context.DeleteFile(scriptPath);
        }
    }
}

/// <summary>
/// Prints multiline header.
/// Header can be overriden in list param "Header".
/// <p><example><code>--Header="-----,YourCompany,YourProject,-----"</code></example></p>
/// </summary>
public static ScriptArgs PrintHeader(this ScriptArgs args, string[] headers = null)
{
    #addin nuget:?package=Cake.Figlet&version=1.1.0

    headers = headers ?? new [] {"------------", "MicroElements", "DevOps", "------------"};

    var headerParam = args.GetOrCreateParam<string>("Header")
        .SetIsList()
        .SetFromArgs()
        .AddValues(headers, ParamSource.DefaultValue)
        .Build(args);

    var context = args.Context;
    var headerValues = headerParam.Values;

    Func<string, string> Figlet = (input) => context.Figlet(input);
    Func<string, int> FigletWidth = (input) => Figlet(input).SplitLines().First().Length;
    var maxFigletWidth = headerValues.Select(FigletWidth).Max();
    Func<string, int> PadLen = (input) => (maxFigletWidth - input.Length)/2;
    Func<string, string> RemoveEmptyLines = (input) => string.Join(Environment.NewLine, input.SplitLines().Where(s=>!string.IsNullOrWhiteSpace(s)));
    Func<string, string> PadLines = (input) => string.Join(Environment.NewLine, input.SplitLines().Select(s=>s.PadLeft(s.Length+PadLen(s))));
    var SlimFiglet = Figlet.Then(RemoveEmptyLines);
    var PaddedFiglet = Figlet.Then(RemoveEmptyLines).Then(PadLines);
    Action<string> PrintAct = (input) => Console.WriteLine(input);
    Func<string, string> Print = PrintAct.ToFunc();

    Console.ForegroundColor = ConsoleColor.Green;
    headerValues.ForEach(PaddedFiglet.Then(Print));
    Console.ResetColor();
    context.Information("");

    return args;
}
