#load versioning.cake

/// <summary>
/// ScriptArgs provides parameters and conventions for interscript communication.
/// </summary>
public class ScriptArgs
{
    public ICakeContext Context {get;}
    public ScriptParam<DirectoryPath> RootDir {get;}

    public VersionInfo Version {get; set;}

    public ScriptParam<DirectoryPath> BuildDir;
    public ScriptParam<DirectoryPath> SrcDir;
    public ScriptParam<DirectoryPath> TestDir;
    public ScriptParam<DirectoryPath> TestResultsDir;
    public ScriptParam<DirectoryPath> ArtifactsDir;
    public ScriptParam<DirectoryPath> ToolsDir;
    public ScriptParam<DirectoryPath> ResourcesDir;
    public ScriptParam<DirectoryPath> TemplatesDir;

    public class KnownFilesList
    {
        public ScriptParam<FilePath> ChangeLog;
        public ScriptParam<FilePath> Readme;
        public ScriptParam<FilePath> VersionProps;
        public ScriptParam<FilePath> SolutionFile;
    }

    public KnownFilesList KnownFiles = new KnownFilesList();

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

    public ScriptArgs(ICakeContext context, string rootDir = "./")
    {
        Context = context;
        RootDir = Param<DirectoryPath>("RootDir").WithValue(context.Directory(rootDir).Path).Build(this);
    }

    public void Build()
    {
        // PrintParams();
        Context.Information($"VERSION: {Version.VersionPrefix}");
    }

    public ScriptParamBuilder<T> Param<T>(string name)
    {
        var builder = new ScriptParamBuilder<T>(name);
        if(typeof(T) == typeof(string) || typeof(T) == typeof(bool))
            builder.WithValue(args=>DefaultConventions.ArgumentOrEnvVar<T>(args.Context, name));
        return builder;
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

    public IEnumerable<string> ParamKeys => Params.Keys;

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

/// <summary>
/// ParamValue contains value and source of value.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
public class ParamValue<T> : IEquatable<ParamValue<T>>
{
    public static ParamValue<T> NoValue = new ParamValue<T>(default(T), ParamSource.NoValue);

    public T Value { get; }

    public ParamSource Source { get; }

    public ParamValue(T value, ParamSource source)
    {
        Value = value;
        Source = source;
    }

    public bool Equals(ParamValue<T> other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return EqualityComparer<T>.Default.Equals(Value, other.Value) && Source == other.Source;
    }
}

public static bool HasNoValue<T>(this ParamValue<T> paramValue)
{
    return paramValue.Source==ParamSource.NoValue || EqualityComparer<T>.Default.Equals(paramValue.Value, default(T));
}

public static bool HasValue<T>(this ParamValue<T> paramValue) => !paramValue.HasNoValue();

public delegate ParamValue<T> GetParam<T>(ICakeContext context, string name);

/// <summary>
/// GetValue delegate. Returns ParamValue.
/// </summary>
public delegate ParamValue<T> GetValue<T>(ScriptArgs args);

/// <summary>
/// Simplified GetValue. Translates to ParamValue with ParamSource.Conventions
/// </summary>
public delegate T GetSimpleValue<T>(ScriptArgs args);

/// <summary>
/// Converts value to ParamValue.
/// </summary>
public static ParamValue<T> ToParamValue<T>(this T value, ParamSource source = ParamSource.Conventions) => new ParamValue<T>(value, source);

public class DefaultConventions 
{
    public static ParamValue<T> ArgumentOrEnvVar<T>(ICakeContext context, string name)
    {
        if(context.HasArgument(name))
            return new ParamValue<T>(context.Argument<T>(name, default(T)), ParamSource.CommandLine);
        if(context.HasEnvironmentVariable(name))
            return new ParamValue<T>((T)Convert.ChangeType(context.EnvironmentVariable(name), typeof(T)), ParamSource.EnvironmentVariable);
        return new ParamValue<T>(default(T), ParamSource.NoValue);
    }

    public ScriptParamBuilder<T> Param<T>(string name)
    {
        var builder = new ScriptParamBuilder<T>(name);
        if(typeof(T) == typeof(string) || typeof(T) == typeof(bool))
            builder.WithValue(args=>DefaultConventions.ArgumentOrEnvVar<T>(args.Context, name));
        return builder;
    }
}

public class ScriptParam<T> 
{
    private List<GetValue<T>> _getValueChain = new List<GetValue<T>>();

    public ScriptParam(string name, IEnumerable<GetValue<T>> getValueChain  = null)
    {
        Name = name;
        _getValueChain.AddRange(getValueChain.NotNull());
    }

    public string Name {get;}
    public string Description {get; set;}
    public bool IsSecret {get; set;} = false;

    public ParamValue<T> BuildedValue {get;private set;}
    public T Value { get { return GetBuildedValue(); } }
    public T DefaultValue {get; set;}

    public bool Required {get; set;} = false;
    public T[] ValidValues {get; set;}
    
    /// <summary>
    /// Builds Param. Evaluates value, checks rules.
    /// </summary>
    public ScriptParam<T> Build(ScriptArgs args)
    {
        var paramValue = EvaluateValue(args);
        paramValue = CheckRules(paramValue);
        BuildedValue = paramValue;

        args.Context.Information($"PARAM: {Name}={Formated}; SOURCE: {BuildedValue.Source}");
        return this;
    }

    public ParamValue<T> CheckRules(ParamValue<T> paramValue)
    {
        if(paramValue==default(ParamValue<T>))
            throw new Exception($"Parameter {Name} is null.");

        if(Required && !paramValue.HasValue())
            throw new Exception($"Parameter {Name} is required but value is not provided.");

        var comparer = EqualityComparer<T>.Default;
        if(ValidValues!=null && !ValidValues.Contains(paramValue.Value, comparer) && !comparer.Equals(paramValue.Value, default(T)))
        {
            var errorMessage = $"Value '{paramValue.Value}' is not allowed. Use one of: {string.Join(",", ValidValues)}";
            throw new Exception(errorMessage);
        }

        return paramValue;
    }

    /// <summary>
    /// Gets value that already builded.
    /// </summary>
    public T GetBuildedValue()
    {
        if(BuildedValue==default(ParamValue<T>))
            throw new Exception($"Parameter {Name} was not builded but value was requested.");

        if(BuildedValue.HasValue())
            return BuildedValue.Value;

        if(Required)
            throw new Exception($"Parameter {Name} is required but value is not provided.");

        return default(T); 
    }

    /// <summary>
    /// Evaluates value.
    /// </summary>
    public ParamValue<T> EvaluateValue(ScriptArgs args)
    {
        ParamValue<T> paramValue = ParamValue<T>.NoValue;
        foreach (var getValue in _getValueChain)
        {
            paramValue = getValue(args) ?? ParamValue<T>.NoValue;
            if(HasValue(paramValue))
                break;
        }

        if(HasValue(paramValue))
            return paramValue;

        if(Required)
            throw new Exception($"Parameter {Name} is required but value is not provided.");

        return paramValue;
    }

    public string Formated => IsSecret? "***" : this.BuildedValue.HasValue() ? $"{Value}" : "{NoValue}";

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

    List<GetValue<T>> _getValueChain = new List<GetValue<T>>();

    T _defaultValue;
    T[] _validValues;
    bool _required = false;
    bool _isSecret = false;

    public ScriptParamBuilder(string name)
    {
        _name = name.CheckNotNull("name");
    }

    public ScriptParamBuilder<T> WithValue(GetValue<T> getValue)
    {
        getValue.CheckNotNull("getValue");
        _getValueChain.Add(getValue);
        return this;
    }

    public ScriptParamBuilder<T> WithValue(GetSimpleValue<T> getSimpleValue)
    {
        getSimpleValue.CheckNotNull("getValue");
        _getValueChain.Add(args=>getSimpleValue(args).ToParamValue(ParamSource.Conventions));
        return this;
    }

    public ScriptParamBuilder<T> WithValue(T value)
    {
        value.CheckNotNull("value");
        _getValueChain.Add(args=>value.ToParamValue(ParamSource.Conventions));
        return this;
    }

    public ScriptParamBuilder<T> DefaultValue(T defaultValue)
    {
        _defaultValue = defaultValue;
        _getValueChain.Add(args=>defaultValue.ToParamValue(ParamSource.DefaultValue));
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

    public ScriptParamBuilder<T> IsSecret()
    {
        _isSecret = true;
        return this;
    }

    public ScriptParam<T> Build(ScriptArgs args)
    {
        try
        {
            var param = new ScriptParam<T>(_name, _getValueChain);
            param.Description = _description;
            param.DefaultValue = _defaultValue;
            param.ValidValues = _validValues;
            param.Required = _required;
            param.IsSecret = _isSecret;
            param.Build(args);

            args.SetParam(param.Name, param.Value);
            
            return param;
        }
        catch (System.Exception e)
        {
            args.Context.Error($@"Building param ""{_name}"". Error: {e.ToString()}");
            throw;
        }
    } 
}

public static ScriptParam<T> ShouldHaveValue<T>(this ScriptParam<T> param)
{
    if(!param.BuildedValue.HasValue())
        throw new Exception($"Param {param.Name} should have value");
    return param;
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

public static DirectoryPath GetDevopsToolDir(this ScriptArgs args)
{
    var devops_version       = GetVersionFromCommandLineArgs(args.Context);
    var devops_tool_dir      = args.ToolsDir.Value.Combine("microelements.devops").Combine(devops_version);
    return devops_tool_dir;
}

public static string GetTemplate(this ScriptArgs args, string fileName)
{
    var templateFileName = args.Context.File(fileName).Path;
    templateFileName = templateFileName.IsRelative? args.TemplatesDir.Value.CombineWithFilePath(templateFileName) : templateFileName;
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

public static void AddFileFromTemplate(this ScriptArgs args, string name, DirectoryPath destinationDir, string destinationName = null)
{
    var context = args.Context;
    var destinationFile = destinationDir.CombineWithFilePath(destinationName??name);

    if(context.FileExists(destinationFile))
        context.Information($"{destinationFile} file already exists.");
    else
    {
        var content = args.GetTemplate($"{name}");
        System.IO.File.WriteAllText(destinationFile.FullPath, content);
        context.Information($"{destinationFile} created.");
    }
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

public static T CheckNotNull<T>(this T value, string paramName)
{
    if(value == null)
        throw new ArgumentNullException(paramName??"value");
    return value;
}

public static IEnumerable<T> NotNull<T>(this IEnumerable<T> collection) => collection ?? Array.Empty<T>();

public static ICollection<T> NotNull<T>(this ICollection<T> collection) => collection ?? Array.Empty<T>();

public static T[] NotNull<T>(this T[] collection) => collection ?? Array.Empty<T>();

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
