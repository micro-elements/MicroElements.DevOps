#load scriptParam.cake

/// <summary>
/// ScriptArgs provides parameters and conventions for interscript communication.
/// </summary>
public class ScriptArgs
{
    public ICakeContext Context {get;}
    public ScriptParam<DirectoryPath> RootDir {get;} = new ScriptParam<DirectoryPath>("RootDir");

    public ScriptArgs(ICakeContext context, string rootDir = "./")
    {
        Context = context;
        RootDir.SetFromArgs().SetValue(context.MakeAbsolute(context.Directory(rootDir))).Build(this);
    }

    public VersionInfo Version {get; set;}

    public ScriptParam<DirectoryPath> BuildDir {get; set;}
    public ScriptParam<DirectoryPath> SrcDir {get; set;}
    public ScriptParam<DirectoryPath> TestDir {get; set;}
    public ScriptParam<DirectoryPath> TestResultsDir {get; set;}
    public ScriptParam<DirectoryPath> ArtifactsDir {get; set;}
    public ScriptParam<DirectoryPath> ToolsDir {get; set;}
    public ScriptParam<DirectoryPath> ResourcesDir {get; set;}
    public ScriptParam<DirectoryPath> TemplatesDir {get; set;}

    public class KnownFilesList
    {
        public ScriptParam<FilePath> ChangeLog {get; set;}
        public ScriptParam<FilePath> Readme {get; set;}
        public ScriptParam<FilePath> VersionProps {get; set;}
        public ScriptParam<FilePath> SolutionFile {get; set;}
    }

    public KnownFilesList KnownFiles {get; set;} = new KnownFilesList();

    public ScriptParam<string> Target {get; set;}

    [DefaultValue("Release")]
    [Description("The configuration of build. Common values are: Release and Debug.")]
    public ScriptParam<string> Configuration {get; set;}
    public ScriptParam<string> ProjectName {get; set;}
    public ScriptParam<string> RuntimeName {get; set;}
    public ScriptParam<string> upload_nuget {get; set;}
    public ScriptParam<string> upload_nuget_api_key {get; set;}
    public ScriptParam<string> nuget_source1 {get; set;}
    public ScriptParam<string> nuget_source2 {get; set;}
    public ScriptParam<string> nuget_source3 {get; set;}

    [DefaultValue(true)]
    public ScriptParam<bool> UseSourceLink {get; set;}

    [DefaultValue(true)]
    public ScriptParam<bool> TestSourceLink {get; set;}

    /// <summary>
    /// Script parameters.
    /// </summary>
    private Dictionary<string,IScriptParam> Params = new Dictionary<string,IScriptParam>(StringComparer.InvariantCultureIgnoreCase);

    public void Build()
    {
        // todo: Rebuild values if needed
        // PrintParams
        foreach (var scriptParam in Params.Values)
        {
            // todo: order?
            scriptParam.Build(this);
        }
    }

    public void InitializeParams(InitializeParamSettings settings)
    {
        var params1 = Initializer.InitializeParams(Context, this, settings);
        var params2 = Initializer.InitializeParams(Context, KnownFiles, settings.WithNamePrefix("KnownFiles"));
        var scriptParams = params1.Concat(params2).ToList();
        foreach (var scriptParam in scriptParams)
        {
            Params[scriptParam.Name] = scriptParam;
        }
    }

    public T Init<T>(System.Linq.Expressions.Expression<Func<ScriptArgs, T>> propExpression, InitializeParamSettings settings = null) where T : IScriptParam
    {
        var propertyInfo = (PropertyInfo)((System.Linq.Expressions.MemberExpression)propExpression.Body).Member;
        Initializer.InitializeParam(Context, this, propertyInfo, settings);
        return propExpression.Compile().Invoke(this);
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

    public ScriptParam<T> SetParam<T>(string name, T value)
    {
        return GetOrCreateParam(name, value).SetValue(value);
    }

    public ScriptParam<T> AddParam<T>(ScriptParam<T> scriptParam)
    {
        Params.Add(scriptParam.Name, scriptParam);
        return scriptParam;
    }

    public ScriptParam<T> AddParam<T>(string name, T value)
    {
        var scriptParam = new ScriptParam<T>(name).SetValue(value);
        return AddParam(scriptParam);
    }

    public IScriptParam GetParam(string name)
    {
        if(!Params.TryGetValue(name, out var scriptParam))
            throw new Exception($"No param {name} exist!");
        return scriptParam;
    }

    public ScriptParam<T> GetParam<T>(string name)
    {
        if(!Params.TryGetValue(name, out var scriptParam))
            throw new Exception($"No param {name} exist!");
        if(typeof(T)!=scriptParam.Type)
            throw new Exception($"Param {name} has type {scriptParam.Type} but type {typeof(T)} was requested!");
        return (ScriptParam<T>)scriptParam;
    }

    public ScriptParam<T> GetOrCreateParam<T>(string name, T value)
    {
        if(!Params.TryGetValue(name, out var scriptParam))
            return AddParam<T>(name, value);
        if(typeof(T)!=scriptParam.Type)
            throw new Exception($"Param {name} has type {scriptParam.Type} but type {typeof(T)} was requested!");
        return (ScriptParam<T>)scriptParam;
    }

    public void SetParamIfNotExists<T>(string name, T value)
    {
        if(!ContainsKey(name))
            SetParam(name, value);
    }

    public T GetParamOrDefault<T>(string name, T defaultValue)
    {
        if(!ContainsKey(name))
            return defaultValue;
        return GetParam<T>(name).Value;
    }

    public string GetStringParam(string name)
    {
        return $"{GetParam(name).FormattedValue}";
    }
}
