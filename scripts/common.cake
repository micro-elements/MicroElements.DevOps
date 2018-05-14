/// <summary>ScriptArgs is args for interscript communication.</summary> 
public class ScriptArgs
{
    public ICakeContext Context {get;}
    public ConvertableDirectoryPath Root {get;}

    public ConvertableDirectoryPath SrcDir;
    public ConvertableDirectoryPath TestDir;
    public ConvertableDirectoryPath ToolsDir;
    public ConvertableDirectoryPath ResourcesDir;
    public ConvertableDirectoryPath TemplatesDir;

    public ConvertableDirectoryPath BuildDir;

    public Dictionary<string,object> Params;

    public ScriptArgs(ICakeContext context, ConvertableDirectoryPath root)
    {
        Context = context;
        Root = root;
        SrcDir = Root + context.Directory("src");
        TestDir = Root + context.Directory("test");
        ToolsDir = Root + context.Directory("tools");
        var devops_version = GetVersionFromCommandLineArgs(context);
        var devops_tool_dir = ToolsDir + context.Directory("microelements.devops") + context.Directory(devops_version);
        ResourcesDir = devops_tool_dir + context.Directory("resources");
        TemplatesDir = devops_tool_dir + context.Directory("templates");
    }
}

/// <summary>Returns command line argument or environment variable or default value.</summary>
public static T ArgumentOrEnvVar<T>(ICakeContext context, string name, T defaultValue, T[] variants = null, bool secret = false)
{
    var result = context.HasArgument(name) ? context.Argument<T>(name, default(T)) 
        : context.HasEnvironmentVariable(name) ? (T)Convert.ChangeType(context.EnvironmentVariable(name), typeof(T))
        : defaultValue;
    var varSource = context.HasArgument(name)? "Argument" : context.HasEnvironmentVariable(name)? "EnvironmentVariable" : "DefaultValue";
    var resultText = secret? "***" : $"{result}";   
    context.Information($"VARIABLE: {name}={resultText}; SOURCE: {varSource}");
    var comparer = EqualityComparer<T>.Default;
    if(variants!=null && !variants.Contains(result, comparer) && !comparer.Equals(result,defaultValue))
    {
        var errorMessage = $"Value '{result}' is not allowed. Use one of: {string.Join(",", variants)}";
        context.Error(errorMessage);
        throw new Exception(errorMessage);
    }
    return result;
}

public static string GetVersionFromCommandLineArgs(ICakeContext context)
{
    var commandLineArgs = System.Environment.GetCommandLineArgs();
    context.Information("CommandLineArgs: "+System.String.Join(" ", commandLineArgs));

    string cake_version = "";
    string devops_version = "";
    foreach(var arg in commandLineArgs.Select(a=>a.ToLower()))
    {
        if(arg.EndsWith("cake.dll"))
        {

        }
        if(arg.Contains("microelements.devops"))
        {
            //C:\Projects\ProjName\tools\microelements.devops\0.2.0\scripts\main.cake
            var segments = context.File(arg).Path.Segments;
            int index = System.Array.IndexOf(segments, "microelements.devops");
            devops_version = segments[index+1];
        }       
    }

    return devops_version;
}

public static string ReadTemplate(ScriptArgs args, string fileName)
{
    var templateFileName = args.Context.File(fileName);
    templateFileName = templateFileName.Path.IsRelative? args.TemplatesDir + templateFileName : templateFileName;
    string templateText = System.IO.File.ReadAllText(templateFileName.Path.FullPath);
    return templateText;
}

public class ProcessUtils
{
    public static (int ExitCode, string Output) StartProcessAndReturnOutput(ICakeContext context, FilePath fileName, ProcessArgumentBuilder args, bool printOutput = false)
    {
        if(printOutput)
            context.Information($"{fileName} {args.Render()}");
        
        IEnumerable<string> redirectedStandardOutput;
        var exitCodeWithArgument =
            context.StartProcess( fileName,
                new ProcessSettings {
                    Arguments = args,
                    RedirectStandardOutput = true
                },
                out redirectedStandardOutput
            );

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

//todo
//args.Context.TransformText()
