
public class ScriptArgs
{
    public ConvertableDirectoryPath Root;
    public ConvertableDirectoryPath SrcDir;
    public ConvertableDirectoryPath TestDir;

    public Dictionary<string,object> Params;
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
