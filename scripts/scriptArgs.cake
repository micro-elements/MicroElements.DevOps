#load scriptParam.cake
using System.Collections;

/// <summary>
/// ScriptArgs provides parameters and conventions for interscript communication.
/// </summary>
public class ScriptArgs
{
    /// <summary>Represents a context for scripts and script aliases.</summary>
    public ICakeContext Context {get;}

    /// <summary>Root project dir. Repo root dir.</summary>
    public ScriptParam<DirectoryPath> RootDir {get;} = new ScriptParam<DirectoryPath>("RootDir");

    public ScriptArgs(ICakeContext context, string rootDir = "./")
    {
        Context = context;
        RootDir.SetFromArgs().SetValue(context.MakeAbsolute(context.Directory(rootDir))).Build(this);
        KeyComparer = new KeyEqualityComparer();
        Params = new ScriptParamList(KeyComparer);
    }

    [Description("The name of task to run.")]
    [DefaultValue("Default")]
    public ScriptParam<string> Target {get; set;}

    [Description("The configuration of build. Common values are: Release and Debug.")]
    [DefaultValue("Release")]
    public ScriptParam<string> Configuration {get; set;}

    public ScriptParam<VersionInfo> VersionParam {get; set;}
    public VersionInfo Version => VersionParam.Value;
  
    [Description("Sources directory. Contains projects.")]
    public ScriptParam<DirectoryPath> SrcDir {get; set;}

    [Description("Directory that contains test projects.")]
    public ScriptParam<DirectoryPath> TestDir {get; set;}

    [Description("Optional GetTestProjectsFunc gets test projects.")]
    public Func<IReadOnlyList<FilePath>> GetTestProjectsFunc {get; set;}

    [Description("Directory for storing build artifacts.")]
    public ScriptParam<DirectoryPath> ArtifactsDir {get; set;}

    [Description("Directory that contains test results.")]
    public ScriptParam<DirectoryPath> TestResultsDir {get; set;}

    [Description("Directory that contains code coverage results.")]
    public ScriptParam<DirectoryPath> CoverageResultsDir {get; set;}

    [Description("Directory that contains packages.")]
    public ScriptParam<DirectoryPath> PackagesDir {get; set;}

    [Description("Tools directory to store build tools.")]
    public ScriptParam<DirectoryPath> ToolsDir {get; set;}

    [ScriptParam(Name="DevOpsRoot")]
    public ScriptParam<DirectoryPath> DevOpsRootDir {get; set;}
    public ScriptParam<string> DevOpsVersion {get; set;}
    public ScriptParam<DirectoryPath> ResourcesDir {get; set;}
    [ScriptParam(IsList=true)]
    public ScriptParam<DirectoryPath> TemplatesDir {get; set;}
    public class KnownFilesList
    {
        public ScriptParam<FilePath> ChangeLog {get; set;}
        public ScriptParam<FilePath> Readme {get; set;}
        public ScriptParam<FilePath> VersionProps {get; set;}
        public ScriptParam<FilePath> SolutionFile {get; set;}
    }

    public KnownFilesList KnownFiles {get; set;} = new KnownFilesList();

    public ScriptParam<string> ProjectName {get; set;}
    public ScriptParam<string> RuntimeName {get; set;}
    public ScriptParam<string> upload_nuget {get; set;}
    public ScriptParam<string> upload_nuget_api_key {get; set;}

    [ScriptParam(IsList=true)]
    public ScriptParam<string> NugetSource {get; set;}

    [DefaultValue(true)]
    public ScriptParam<bool> UseSourceLink {get; set;}

    [DefaultValue(true)]
    public ScriptParam<bool> TestSourceLink {get; set;}

    //Should section

    [DefaultValue(true)]
    public ScriptParam<bool> RunTests {get; set;}

    [DefaultValue(true)]
    public ScriptParam<bool> RunCodeCoverage {get; set;}

    public Action<ScriptArgs> CoverageTask {get; set;}

    /// <summary>
    /// Determine whether to upload packages.
    /// </summary>
    [DefaultValue(true)]
    public ScriptParam<bool> UploadPackages {get; set;}

    [DefaultValue(false)]
    public ScriptParam<bool> ForceUploadPackages {get; set;}

    /// <summary>
    /// Key comparer.
    /// </summary>
    public KeyEqualityComparer KeyComparer {get;}

    /// <summary>
    /// Script parameters.
    /// </summary>
    private ScriptParamList Params {get;}

    public ScriptArgs Build(bool buildOnlyEmptyParams=true)
    {
        foreach (var scriptParam in Params)
        {
            var isBuildNeeded = (buildOnlyEmptyParams && !scriptParam.HasValue) || !buildOnlyEmptyParams;
            if(isBuildNeeded)
                scriptParam.Build(this);
        }
        return this;
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
            Context.Information(param.Format());
        }
    }

    public bool ContainsKey(string key)
    {
        return ParamKeys.Contains(key, KeyComparer);
    }

    public IEnumerable<string> ParamKeys => Params.Keys;

    public ScriptParam<T> SetParam<T>(string name, T value, ParamSource paramSource = ParamSource.Conventions)
    {
        return GetOrCreateParam<T>(name).SetValue(value, paramSource).Build(this);
    }

    public ScriptParam<T> AddParam<T>(ScriptParam<T> scriptParam)
    {
        return Params.AddParam(scriptParam);
    }

    public ScriptParam<T> AddParam<T>(string name)
    {
        var scriptParam = new ScriptParam<T>(name);
        return AddParam(scriptParam);
    }

    public ScriptParam<T> AddParam<T>(string name, T value, ParamSource paramSource = ParamSource.Conventions)
    {
        var scriptParam = new ScriptParam<T>(name).SetValue(value, paramSource);
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

    public ScriptParam<T> GetOrCreateParam<T>(string name, T value, ParamSource paramSource)
    {
        if(!Params.TryGetValue(name, out var scriptParam))
            return AddParam<T>(name, value, paramSource);
        if(typeof(T)!=scriptParam.Type)
            throw new Exception($"Param {name} has type {scriptParam.Type} but type {typeof(T)} was requested!");
        return (ScriptParam<T>)scriptParam;
    }

    public ScriptParam<T> GetOrCreateParam<T>(string name)
    {
        if(!Params.TryGetValue(name, out var scriptParam))
            return AddParam<T>(name);
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

/// <summary>
/// Looks like ordered list with search by name.
/// </summary>
public class ScriptParamList : IEnumerable<IScriptParam>
{
    private List<IScriptParam> _params = new List<IScriptParam>();

    private IEqualityComparer<string> _keyComparer;

    public ScriptParamList(IEqualityComparer<string> keyComparer)
    {
        _keyComparer = keyComparer.CheckNotNull(nameof(keyComparer));
    }

    public IEnumerator<IScriptParam> GetEnumerator() => _params.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<IScriptParam>)_params).GetEnumerator();

    public IReadOnlyCollection<string> Keys => _params.Select(p=>p.Name).ToList();

    public ScriptParam<T> AddParam<T>(ScriptParam<T> scriptParam)
    {
        _params.Add(scriptParam);
        return scriptParam;
    }

    public bool TryGetValue(string name, out IScriptParam scriptParam)
    {
        var index = GetIndex(name);
        if(index>=0)
        {
            scriptParam = _params[index];
            return true;
        }

        scriptParam = null;
        return false;
    }

    public IScriptParam this[string name]
    {
        get
        {
            var index = GetIndex(name);
            if(index>=0)
                return _params[index];
            throw new Exception($"No param {name} exists!");
        }
        set
        {
            var index = GetIndex(name);
            if(index>=0)
                _params[index] = value;
            else
                _params.Add(value);
        }
    }

    private int GetIndex(string name) => _params.FindIndex(sp => _keyComparer.Equals(sp.Name, name));
}

/// <summary>
/// Key comparer.
/// </summary>
public class KeyEqualityComparer : IEqualityComparer<string>
{
    public bool Equals(string x, string y) => string.Equals(x, y, StringComparison.InvariantCultureIgnoreCase);

    public int GetHashCode(string obj) => (obj ?? "").ToLower().GetHashCode();
}
