/// <summary>
/// ScriptArgs provides parameters and conventions for interscript communication.
/// </summary>
public class ScriptArgs
{
    public ICakeContext Context {get;}
    public ConvertableDirectoryPath RootDir {get;}
    public ConvertableDirectoryPath BuildDir;
    public ConvertableDirectoryPath SrcDir;
    public ConvertableDirectoryPath TestDir;
    public ConvertableDirectoryPath TestResultsDir;
    public ConvertableDirectoryPath ArtifactsDir;
    public ConvertableDirectoryPath ToolsDir;
    public ConvertableDirectoryPath ResourcesDir;
    public ConvertableDirectoryPath TemplatesDir;

    public ScriptParam<string> Target {get;set;}
    public ScriptParam<string> Configuration {get;set;}
    public ScriptParam<string> ProjectName {get;set;}
    public ScriptParam<string> RuntimeName {get;set;}
    public ScriptParam<string> upload_nuget {get;set;}
    public ScriptParam<string> upload_nuget_api_key {get;set;}
    public ScriptParam<string> nuget_source1 {get;set;}
    public ScriptParam<string> nuget_source2 {get;set;}
    public ScriptParam<string> nuget_source3 {get;set;}

    /// <summary>
    /// Script parameters.
    /// </summary>
    private Dictionary<string,object> Params = new Dictionary<string,object>(StringComparer.InvariantCultureIgnoreCase);

    public ScriptArgs(ICakeContext context, string rootDir = "./", string buildDir = null)
    {
        Context = context;
        RootDir = context.Directory(rootDir);
        BuildDir = buildDir != null ? context.Directory(buildDir) : null;
        ScriptConventions conventions = new DefaultConventions();

        Target                  = Param<string>("target").DefaultValue("Default").Build(this, conventions);
        Configuration           = Param<string>("Configuration").DefaultValue("Release").ValidValues("Release", "Debug").Build(this, conventions);
        ProjectName             = Param<string>("ProjectName").WithValue(conventions.GetProjectName).Build(this, conventions);

        upload_nuget            = Param<string>("upload_nuget").DefaultValue("https://api.nuget.org/v3/index.json").Build(this, conventions);
        upload_nuget_api_key    = Param<string>("upload_nuget_api_key").DefaultValue("00000000-0000-0000-0000-000000000000").IsSecret().Build(this, conventions);
        nuget_source1           = Param<string>("nuget_source1").DefaultValue("https://api.nuget.org/v3/index.json").Build(this, conventions);
        nuget_source2           = Param<string>("nuget_source1").Build(this, conventions);
        nuget_source3           = Param<string>("nuget_source1").Build(this, conventions);

        // any, linux-x64, win-x64, rhel.7-x64 // see: https://docs.microsoft.com/ru-ru/dotnet/core/rid-catalog
        RuntimeName = Param<string>("runtimeName").DefaultValue("any")
                        .ValidValues("any", "linux-x64", "win-x64")
                        .Build(this, conventions);

        SrcDir              = RootDir + context.Directory("src");
        TestDir             = RootDir + context.Directory("test");
        ToolsDir            = RootDir + context.Directory("tools");
        var devops_version  = GetVersionFromCommandLineArgs(context);
        var devops_tool_dir = ToolsDir + context.Directory("microelements.devops") + context.Directory(devops_version);
        ResourcesDir        = devops_tool_dir + context.Directory("resources");
        TemplatesDir        = devops_tool_dir + context.Directory("templates");

        var solutionName = Param<string>("solutionName").WithValue(conventions.GetSolutionName).DefaultValue($"{ProjectName.Value}.sln").Build(this, conventions);
        var solutionFile = Param<string>("solutionFile").WithValue(conventions.GetSolutionFileName).Build(this, conventions);

        // TODO: Runtime evaluation
        BuildDir = BuildDir ?? RootDir + context.Directory("build") + context.Directory(Configuration.Value);
        TestResultsDir = BuildDir + context.Directory("test-results");
        ArtifactsDir = BuildDir + context.Directory("artifacts");

        SetParamsFromProperties();
    }

    private void SetParamsFromProperties()
    {
        SetParam("RootDir", RootDir);
        SetParam("BuildDir", BuildDir);
        SetParam("SrcDir", SrcDir);
        SetParam("TestDir", TestDir);
        SetParam("ToolsDir", ToolsDir);
        SetParam("ResourcesDir", ResourcesDir);
        SetParam("TemplatesDir", TemplatesDir);
    }

    public ScriptParamBuilder<T> Param<T>(string name)
    {
        bool accepted = typeof(T) == typeof(string) || typeof(T) == typeof(bool);
        if(!accepted)
            throw new Exception($"Param {name} must be one of type: string, bool");
        return new ScriptParamBuilder<T>(name);
    }

    public void PrintParams()
    {
        foreach (var param in Params)
        {
            Context.Information($"{param.Key}: {param.Value}");
        }
    }

    public bool ContainsKey(string key)
    {
        return ParamKeys.Contains(key);
    }

    public IEnumerable<string> ParamKeys
    {
        get
        {
            return Params.Keys;
        }
    }

    public void SetParam<T>(string name, T value)
    {
        Params[name] = value;
    }

    public void SetParamIfNotExists<T>(string name, T value)
    {
        if(!Params.ContainsKey(name))
            SetParam(name, value);
    }

    public object GetParam(string name)
    {
        if(!Params.ContainsKey(name))
            throw new Exception($"Parameter {name} is not exists. Fill it before use.");
        return Params[name];
    }

    public T GetParamOrDefault<T>(string name, T defaultValue)
    {
        if(!Params.ContainsKey(name))
            return defaultValue;
        return (T)Params[name];
    }

    public string GetStringParam(string name)
    {
        return $"{GetParam(name)}";
    }

    public bool GetBoolParam(string name)
    {
        var value = GetParam(name);
        if(value is bool)
            return (bool)value;
        if(value is string)
            return Convert.ToBoolean(value);
        throw new Exception($"Param {name} has value {value} that cannot be converted to bool.");
    }
}

public class ParamValue<T>
{
    public T Value {get;}
    public ParamSource Source {get;}

    public ParamValue(T value, ParamSource source)
    {
        Value = value;
        Source = source;
    }
}

public delegate ParamValue<T> GetParam<T>(ICakeContext context, string name);
public delegate ParamValue<T> GetValue<T>(ScriptArgs args);


public class ScriptConventions
{
    public GetParam<string> GetStringParam;
    public GetParam<bool> GetBoolParam;
    public GetValue<string> GetProjectName;
    public GetValue<string> GetSolutionName;
    public GetValue<string> GetSolutionFileName;
}

public static ParamValue<string> ToParamValue(this string value) => new ParamValue<string>(value, ParamSource.Conventions);

public class DefaultConventions : ScriptConventions
{
    public DefaultConventions()
    {
        GetStringParam = ArgumentOrEnvVar<string>;
        GetBoolParam = ArgumentOrEnvVar<bool>;
        GetProjectName = (args) => args.RootDir.Path.GetDirectoryName().ToParamValue();
        GetSolutionName = (args) => $"{args.ProjectName.Value}.sln".ToParamValue();
        GetSolutionFileName = (args) => args.RootDir.Path.CombineWithFilePath(args.Context.File(args.GetStringParam("solutionName"))).FullPath.ToParamValue();
    }

    public static ParamValue<T> ArgumentOrEnvVar<T>(ICakeContext context, string name)
    {
        if(context.HasArgument(name))
            return new ParamValue<T>(context.Argument<T>(name, default(T)), ParamSource.CommandLine);
        if(context.HasEnvironmentVariable(name))
            return new ParamValue<T>((T)Convert.ChangeType(context.EnvironmentVariable(name), typeof(T)), ParamSource.EnvironmentVariable);
        return new ParamValue<T>(default(T), ParamSource.NoValue);
    }
}

public class ScriptParam<T> 
{
    public ScriptParam(string name)
    {
        Name = name;
    }
    public string Name {get;}
    public string Description {get; set;}
    public bool HasValue {get; private set;}
    private T _value;
    public T Value { get { return GetValue(); } }
    public T DefaultValue {get; set;}
    public T[] ValidValues {get; set;}

    public bool Required = true;
    public bool CanBeNull = true;
    public bool IsSecret = false;
    public ParamSource Source;

    public T GetValue()
    {
        if(HasValue)
            return _value;
        if(Required)
            throw new Exception($"Parameter {Name} is required but value is not provided.");
        return DefaultValue;
    }

    public void SetValue(T value)
    {
        var comparer = EqualityComparer<T>.Default;
        var nullValue = default(T);
        if(!comparer.Equals(value, nullValue))
        {
            _value = value; 
            HasValue = true;
        }
        else
        {
            if(CanBeNull)
            {
                _value = nullValue;
                HasValue = true;
            }
            else
            {
                HasValue = false;
            }
        }
    }

    public string Formated => IsSecret? "***" : $"{Value}";

    public static implicit operator T(ScriptParam<T> scriptParam) => scriptParam.Value;

    public override string ToString() => $"{Value}";
}

public enum ParamSource
{
    NoValue,
    CommandLine,
    EnvironmentVariable,
    DefaultValue,
    Conventions
}

public class ScriptParamBuilder<T>
{
    string _name;
    string _description;
    GetValue<T> _getValue;
    T _defaultValue;
    T[] _validValues;
    bool _required = false;
    bool _canBeNull = false;
    bool _isSecret = false;

    public ScriptParamBuilder(string name)
    {
        _name = name;
    }

    public ScriptParamBuilder<T> WithValue(GetValue<T> getValue)
    {
        _getValue = getValue;
        return this;
    }

    public ScriptParamBuilder<T> DefaultValue(T defaultValue)
    {
        _defaultValue = defaultValue;
        return this;
    }

    public ScriptParamBuilder<T> ValidValues(params T[] validValues)
    {
        _validValues = validValues;
        return this;
    }

    public ScriptParamBuilder<T> Description(string description)
    {
        _description = description;
        return this;
    }

    public ScriptParamBuilder<T> Required()
    {
        _required = true;
        return this;
    }

    public ScriptParamBuilder<T> CanBeNull()
    {
        _canBeNull = true;
        return this;
    }

    public ScriptParamBuilder<T> IsSecret()
    {
        _isSecret = true;
        return this;
    }

    public ScriptParam<T> Build(ScriptArgs args, ScriptConventions conventions)
    {
        var context = args.Context;
        var param = new ScriptParam<T>(_name);
        param.Description = _description;
        param.CanBeNull = _canBeNull;
        param.DefaultValue = _defaultValue;
        param.ValidValues = _validValues;
        param.Required = _required;
        param.CanBeNull = _canBeNull;
        param.IsSecret = _isSecret;

        var comparer = EqualityComparer<T>.Default;
        var nullValue = default(T);

        T value = nullValue;
        ParamSource varSource = ParamSource.NoValue;
        if(_getValue!= null)
        {
            var paramValue = _getValue(args);
            value = paramValue.Value;
            varSource = paramValue.Source;
        }

        if(comparer.Equals(value, nullValue))
        {
            if(typeof(T)==typeof(string))
            {
                var paramValue = conventions.GetStringParam(context, _name);
                varSource = paramValue.Source;
                var hasValue = !string.IsNullOrEmpty(paramValue.Value);
                if(hasValue)
                {
                    value = (T)Convert.ChangeType(paramValue.Value, typeof(T));
                }
            }
            if(typeof(T)==typeof(bool))
            {
                var paramValue = conventions.GetBoolParam(context, _name);
                varSource = paramValue.Source;

                var hasValue = paramValue.Source != ParamSource.NoValue;
                if(hasValue)
                {
                    value = (T)Convert.ChangeType(paramValue.Value, typeof(T));
                }
            }
        }

        if(varSource==ParamSource.NoValue && !comparer.Equals(param.DefaultValue, nullValue))
        {
            value = param.DefaultValue;
            varSource = ParamSource.DefaultValue;
        }

        param.SetValue(value);
        param.Source = varSource;

        context.Information($"PARAM: {param.Name}={param.Formated}; SOURCE: {varSource}");

        if(args!=null)
        {
            args.SetParam(param.Name, param.Value);
        }
        return param;
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

    string devops_version = "";
    foreach(var arg in commandLineArgs.Select(a=>a.ToLower()))
    {
        if(arg.Contains("microelements.devops"))
        {
            //C:\Projects\ProjName\tools\microelements.devops\0.2.0\scripts\main.cake
            var segments = context.File(arg).Path.Segments;
            int index = System.Array.IndexOf(segments, "microelements.devops");
            if(index>0 && index<segments.Length-1)
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

public static string FillTags(string inputXml, ScriptArgs args)
{
    foreach (var key in args.ParamKeys)
    {
        inputXml = inputXml.Replace($"$<{key}>$", $"<{args.GetStringParam(key)}");
        inputXml = inputXml.Replace($"<{key}></{key}>", $"<{key}>{args.GetStringParam(key)}</{key}>");
    }
    return inputXml;
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
