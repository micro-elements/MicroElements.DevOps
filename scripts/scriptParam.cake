#load scriptArgs.cake
#load common.cake

/// <summary>
/// ScriptParam base interface.
/// </summary>
public interface IScriptParam
{
    /// <summary>
    /// The name of script param.
    /// </summary>
    string Name {get;}

    /// <summary>
    /// Value type.
    /// </summary>
    Type Type {get;}

    /// <summary>
    /// Indicates that param has value.
    /// </summary>
    bool HasValue {get;}

    /// <summary>
    /// Formatted string value.
    /// </summary>
    string FormattedValue {get;}

    /// <summary>
    /// Script param is required.
    /// </summary>
    bool Required {get;}

    /// <summary>
    /// Builds value.
    /// </summary>
    /// <param name="args">ScriptArgs.</param>
    void Build(ScriptArgs args);
}

/// <summary>
/// Script param.
/// </summary>
/// <typeparam name="T">Type of the value.</typeparam>
public class ScriptParam<T> : IScriptParam
{
    private readonly List<ParamValue<T>> _buildedValues = new List<ParamValue<T>>();
    private readonly List<ValueGetter<T>> _getValueChain = new List<ValueGetter<T>>();

    /// <summary>
    /// Creates new instance of ScriptParam.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="getValueChain">GetValue chain.</param>
    public ScriptParam(string name, params ValueGetter<T>[] getValueChain)
    {
        Name = name;
        _getValueChain.AddRange(getValueChain.NotNull());
    }

    /// <summary>
    /// Creates new instance of ScriptParam.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="defaultValue">Default value.</param>
    public ScriptParam(string name, T defaultValue):
        this(name, new ValueGetter<T>(defaultValue, ParamSource.DefaultValue))
    {
    }

    /// <summary>
    /// The name of script param.
    /// </summary>
    public string Name {get;}

    /// <summary>
    /// Value type.
    /// </summary>
    public Type Type => typeof(T);

    /// <summary>
    /// Description of script param.
    /// </summary>
    public string Description {get; set;}

    /// <summary>
    /// Determines that param is secured and real value should'not be printed in logs. Default value is false.
    /// </summary>
    public bool IsSecret {get; set;} = false;

    /// <summary>
    /// Determines that param is list of values.
    /// </summary>
    public bool IsList {get; set;} = false;

    /// <summary>
    /// Uses for list params to split and merge values.
    /// </summary>
    public string ListDelimeter {get; set;} = ",";

    public ParamValue<T> BuildedValue => _buildedValues.FirstOrDefault() ?? ParamValue<T>.NoValue;
    public T Value => GetBuildedValue();
    public T[] Values => _buildedValues.Select(v=>v.Value).ToArray();
    public T GetValue(ScriptArgs args) => Build(args).Value;
    public T[] GetValues(ScriptArgs args) => Build(args).Values;

    public bool Required {get; set;} = false;
    public T[] ValidValues {get; set;}

    public string FormattedValue => IsSecret? "{Secured}" : MergedValue;
    private string MergedValue => _buildedValues.Count>0? String.Join(ListDelimeter, _buildedValues.Select(FormattedParamValue)) : NoValue;
    private string FormattedParamValue(ParamValue<T> paramValue) => paramValue.HasValue() ? $"{paramValue.Value}" : NoValue;
    private static string NoValue = "{NoValue}";

    public ScriptParam<T> SetValue(ValueGetter<T> getValue, bool replaceSameSource = true)
    {
        getValue.CheckNotNull(nameof(getValue));
   
        var existed = _getValueChain.FirstOrDefault(gv=>gv.ParamSource==getValue.ParamSource);
        if(replaceSameSource && existed!=null)
        {
            var index = _getValueChain.IndexOf(existed);
            _getValueChain[index] = getValue;
        }
        else
        {
            _getValueChain.Add(getValue);
        }

        // place default value at the end of chain
        var hasDefaultValue = _getValueChain.Any(gv=>gv.ParamSource==ParamSource.DefaultValue);
        if(hasDefaultValue && _getValueChain.Last().ParamSource!=ParamSource.DefaultValue)
        {
            var defaultValues = _getValueChain.Where(gv=>gv.ParamSource==ParamSource.DefaultValue);
            var values = _getValueChain.Where(gv=>gv.ParamSource!=ParamSource.DefaultValue);
            var sorted = values.Concat(defaultValues).ToList();
            _getValueChain.Clear();
            _getValueChain.AddRange(sorted);
        }

        return this;
    }

    public ScriptParam<T> SetValue(GetSimpleValue<T> getValue, ParamSource paramSource = ParamSource.Conventions, bool replaceSameSource = true)
    {
        getValue.CheckNotNull(nameof(getValue));
        return SetValue(new ValueGetter<T>(getValue, paramSource), replaceSameSource);
    }

    public ScriptParam<T> SetValue(T constValue, ParamSource paramSource = ParamSource.Conventions, bool replaceSameSource = true)
    {
        constValue.CheckNotNull(nameof(constValue));
        return SetValue(new ValueGetter<T>(constValue, paramSource), replaceSameSource);
    }

    public ScriptParam<T> SetValues(IEnumerable<ValueGetter<T>> getValues, bool replaceSameSource = true)
    {
        getValues.CheckNotNull(nameof(getValues));
        getValues.ToList().ForEach(gv=>SetValue(gv, replaceSameSource));
        return this;
    }

    public ScriptParam<T> ClearValue()
    {
        _getValueChain.Clear();
        _buildedValues.Clear();
        return this;
    }

    public ScriptParam<T> AddValue(GetSimpleValue<T> getValue) => SetValue(getValue, replaceSameSource: false);

    public ScriptParam<T> SetDefaultValue(T constValue) => SetValue(constValue, ParamSource.DefaultValue);

    public ScriptParam<T> SetDefaultValue(GetSimpleValue<T> getValue) => SetValue(getValue, ParamSource.DefaultValue);

    public ScriptParam<T> SetFromArgs()
    {
        SetValues(ArgumentOrEnvVar());
        return this;
    }

    public ScriptParam<T> SetValidValues(params T[] validValues)
    {
        this.ValidValues = validValues;
        return this;
    }

    public ScriptParam<T> SetIsSecret(bool isSecret = true)
    {
        this.IsSecret = isSecret;
        return this;
    }

    /// <summary>
    /// Builds Param.
    /// </summary>
    void IScriptParam.Build(ScriptArgs args)
    {
        this.Build(args);
    }

    /// <summary>
    /// Builds Param. Evaluates value, checks rules.
    /// </summary>
    public ScriptParam<T> Build(ScriptArgs args)
    {
        var values = EvaluateValues(args);
        values = CheckRules(values);

        _buildedValues.Clear();
        _buildedValues.AddRange(values);

        string source = _buildedValues.Count>0? String.Join(ListDelimeter, _buildedValues.Select(pv=>pv.Source)) : NoValue;
        string listParamPrefix = IsList ? "PARAM: LIST " : "PARAM: VALUE";
        string listParamSuffix = IsList ? "; IsList=true" : "";
        args.Context.Information($"{listParamPrefix}: {Name}={FormattedValue}; SOURCE: {source}");
        return this;
    }

    /// <summary>
    /// Evaluates value(s).
    /// </summary>
    public IReadOnlyList<ParamValue<T>> EvaluateValues(ScriptArgs args)
    {
        List<ParamValue<T>> values = new List<ParamValue<T>>();

        foreach (var getValue in _getValueChain)
        {
            if(getValue.ParamSource==ParamSource.NoValue)
                continue;
            if(getValue.PreCondition!=null && !getValue.PreCondition(args))
                continue;

            if(IsList)
            {
                var paramValues = getValue.GetValues(args).Where(paramValue=>paramValue.HasValue()).ToList();
                values.AddRange(paramValues);
            }
            else
            {
                var paramValue = getValue.GetValues(args).FirstOrDefault() ?? ParamValue<T>.NoValue;
                if(paramValue.HasValue())
                {
                    values.Add(paramValue);
                    break;
                }
            }
        }

        if(Required && values.Count==0)
            throw new Exception($"Parameter {Name} is required but value was not provided.");

        return values;
    }

    public IReadOnlyList<ParamValue<T>> CheckRules(IReadOnlyList<ParamValue<T>> values)
    {
        if(Required && values.Count==0)
            throw new Exception($"Parameter {Name} is required but value was not provided.");

        var comparer = EqualityComparer<T>.Default;
        foreach (var paramValue in values)
        {
            if(ValidValues!=null && !ValidValues.Contains(paramValue.Value, comparer) && !comparer.Equals(paramValue.Value, default(T)))
            {
                var errorMessage = $"Value '{paramValue.Value}' is not allowed. Use one of: {string.Join(",", ValidValues)}";
                throw new Exception(errorMessage);
            }
        }

        return values;
    }

    public IEnumerable<ValueGetter<T>> ArgumentOrEnvVar()
    {
        var name = Name;
        ConvertFunc<T> convert = null;
        Func<string, object> ConvertToValue;
        Func<string, IEnumerable<string>> Split = input => input.Split(ListDelimeter[0]);

        if(typeof(T) == typeof(string))
            ConvertToValue = input => input;
        else if(typeof(T) == typeof(DirectoryPath))
            ConvertToValue = input => new DirectoryPath(input);
        else if(typeof(T) == typeof(FilePath))
            ConvertToValue = input => new FilePath(input);
        else
            ConvertToValue = input => Convert.ChangeType(input, typeof(T));

        if(IsList)
            convert = input => Split(input).Select(ConvertToValue).Cast<T>();
        else
            convert = input => ConvertToValue(input).AsEnumerable<T>();

        yield return new ValueGetter<T>(
            a=>a.Context.HasArgument(name),
            a=>a.Context.Argument<string>(name),
            convert,
            ParamSource.CommandLine);

        yield return new ValueGetter<T>(
            a=>a.Context.HasEnvironmentVariable(name),
            a=>a.Context.EnvironmentVariable(name),
            convert,
            ParamSource.EnvironmentVariable);
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

    public bool HasValue => BuildedValue.HasValue();

    public static implicit operator T(ScriptParam<T> scriptParam) => scriptParam.Value;

    public override string ToString() => $"{Value}";

    public static T operator + (ScriptParam<T> dir1, string dir2)
    {
        if(typeof(T)==typeof(DirectoryPath))
        {
            return (T)(object)((DirectoryPath)((object)dir1.Value)).Combine(dir2);
        }
        throw new NotImplementedException();
    }

    public static T operator / (ScriptParam<T> dir1, string dir2)
    {
        if(typeof(T)==typeof(DirectoryPath))
        {
            return (T)(object)((DirectoryPath)((object)dir1.Value)).Combine(dir2);
        }
        throw new NotImplementedException();
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

    public override string ToString() => $"Value: {Value}, Source: {Source}";
}

/// <summary>
/// ParamSource identifies the source of value.
/// </summary>
public enum ParamSource
{
    /// <summary>
    /// NoValue means absence of value, value was not set.
    /// </summary>
    NoValue,

    /// <summary>
    /// Value got from command line params.
    /// </summary>
    CommandLine,

    /// <summary>
    /// Value got from environment variable.
    /// </summary>
    EnvironmentVariable,

    /// <summary>
    /// Default value was set.
    /// </summary>
    DefaultValue,

    /// <summary>
    /// Value was evaluated by conventions.
    /// </summary>
    Conventions
}

/// <summary>
/// GetValue delegate. Returns ParamValue.
/// </summary>
public delegate ParamValue<T> GetParamValue<T>(ScriptArgs args);

/// <summary>
/// GetValues delegate. Returns zero, one or more ParamValues.
/// </summary>
public delegate IEnumerable<ParamValue<T>> GetParamValues<T>(ScriptArgs args);

/// <summary>
/// Simplified GetValue. Translates to ParamValue with ParamSource.Conventions
/// </summary>
public delegate T GetSimpleValue<T>(ScriptArgs args);

/// <summary>
/// Some predicate that indicates ScriptArgs conditions.
/// </summary>
public delegate bool ScriptArgsPredicate(ScriptArgs args);

/// <summary>
/// Converts string value to some type.
/// For list params it can return more than one value, for single value params it returns one value.
/// Empty enumeration means NoValue.
/// </summary>
public delegate IEnumerable<T> ConvertFunc<T>(string value);

/// <summary>
/// ValueGetter is value(s) holder and it knows about value source. Value can be evaluated at runtime.
/// </summary>
/// <typeparam name="T">The type of value.</typeparam>
public class ValueGetter<T>
{
    public ValueGetter(ScriptArgsPredicate preCondition, GetParamValues<T> getValues, ParamSource paramSource)
    {
        PreCondition = preCondition;
        GetValues = getValues.CheckNotNull(nameof(getValues));
        ParamSource = paramSource;
    }

    public ValueGetter(
        ScriptArgsPredicate preCondition,
        GetSimpleValue<string> getValue,
        ConvertFunc<T> convertValue,
        ParamSource paramSource)
    {
        getValue.CheckNotNull(nameof(getValue));
        convertValue.CheckNotNull(nameof(convertValue));

        PreCondition = preCondition;
        GetValues = a => convertValue(getValue(a)).Select(value=>value.ToParamValue(paramSource));
        ParamSource = paramSource;
    }

    public ValueGetter(ScriptArgsPredicate preCondition, GetSimpleValue<T> getValue, ParamSource paramSource)
    {
        getValue.CheckNotNull(nameof(getValue));
        PreCondition = preCondition;
        GetValues = a => getValue(a).ToParamValue(paramSource).AsEnumerable();
        ParamSource = paramSource;
    }

    public ValueGetter(GetSimpleValue<T> getValue, ParamSource paramSource)
        :this(null, getValue, paramSource)
    {
    }

    public ValueGetter(T constantValue, ParamSource paramSource)
    {
        constantValue.CheckNotNull(nameof(constantValue));
        GetValues = a => constantValue.ToParamValue(paramSource).AsEnumerable();
        ParamSource = paramSource.CheckNotNull(nameof(paramSource));
    }

    public ScriptArgsPredicate PreCondition {get;}
    public GetParamValues<T> GetValues {get;}
    public ParamSource ParamSource {get;}
}

/***********************************************/
/*         GLOBAL STATIC METHODS               */
/***********************************************/

public static bool HasNoValue<T>(this ParamValue<T> paramValue)
{
    return paramValue.Source==ParamSource.NoValue || EqualityComparer<T>.Default.Equals(paramValue.Value, default(T));
}

public static bool HasValue<T>(this ParamValue<T> paramValue) => !paramValue.HasNoValue();

public static ScriptParam<T> ShouldHaveValue<T>(this ScriptParam<T> param)
{
    if(!param.BuildedValue.HasValue())
        throw new Exception($"Param {param.Name} should have value");
    return param;
}

/// <summary>
/// Describes ScriptParam.
/// </summary>
[AttributeUsage(AttributeTargets.Property|AttributeTargets.Field)]
public class ScriptParamAttribute : Attribute
{
    /// <summary>
    /// Param name. Overrides property name.
    /// </summary>
    public string Name {get; set;}

    /// <summary>
    /// Param value is list.
    /// </summary>
    public bool IsList {get; set;} = false;
}

/// <summary>
/// Setting for InitializeParam.
/// </summary>
public class InitializeParamSettings
{
    /// <summary>
    /// Create param if value already exists.
    /// </summary>
    public bool ReCreate {get; set;} = false;

    /// <summary>
    /// Init param properties from attributes.
    /// </summary>
    public bool InitFromAttributes {get; set;} = true;

    /// <summary>
    /// If true value initializes from command line arguments and from environment variables.
    /// </summary>
    public bool InitFromArgs {get; set;} = true;

    /// <summary>
    /// NamePrefix uses for param name prefixes. For example for hierarchical structures.
    /// </summary>
    public string NamePrefix {get; set;} = "";

    public InitializeParamSettings Clone() => new InitializeParamSettings{
        InitFromArgs = this.InitFromArgs,
        InitFromAttributes = this.InitFromAttributes,
        ReCreate = this.ReCreate,
        NamePrefix = this.NamePrefix};

    public InitializeParamSettings WithNamePrefix(string namePrefix) {var c = Clone(); c.NamePrefix = namePrefix; return c;}
}

public static class Initializer
{
    /// <summary>
    /// Initialize script params for <see cref="target"/>
    /// </summary>
    /// <returns>Enumerable of initialized script params.</returns>
    public static IEnumerable<IScriptParam> InitializeParams(ICakeContext context, object target, InitializeParamSettings settings = null)
    {
        target.CheckNotNull(nameof(target));
        settings = settings ?? new InitializeParamSettings();

        var properties = target.GetType().GetProperties();
        foreach (var property in properties)
        {
            if(typeof(IScriptParam).IsAssignableFrom(property.PropertyType))
            {
                yield return InitializeParam(context, target, property, settings);
            }
        }
    }

    /// <summary>
    /// Creates ScriptParam and initializes it from attributes.
    /// </summary>
    /// <returns>Initialized param.</returns>
    public static IScriptParam InitializeParam(ICakeContext context, object target, PropertyInfo property, InitializeParamSettings settings = null)
    {
        target.CheckNotNull(nameof(target));
        property.CheckNotNull(nameof(property));
        settings = settings ?? new InitializeParamSettings();

        var scriptParamAttr = property.GetCustomAttribute<ScriptParamAttribute>();

        string paramName = scriptParamAttr?.Name ?? property.Name;
        if(!string.IsNullOrWhiteSpace(settings.NamePrefix))
            paramName = $"{settings.NamePrefix}.{paramName}";
        Type paramType = property.PropertyType.GenericTypeArguments[0];

        IScriptParam scriptParam = (IScriptParam)property.GetValue(target);
        if(scriptParam == null || settings.ReCreate)
        {
            if(property.CanWrite)
            {
                // Create new ScriptParam
                var scriptParamType = typeof(ScriptParam<>).MakeGenericType(paramType);
                scriptParam = (IScriptParam)Activator.CreateInstance(scriptParamType, paramName);

                // Set property value
                property.SetValue(target, scriptParam);
                context.Debug($"ScriptParam created: {paramName}");
            }
        }

        // Invoke InitializeParamInternal for scriptParam
        var surroundType = typeof(Initializer).GetTypeInfo();
        var initParamMethod = surroundType.DeclaredMethods.First(mi=>mi.Name==nameof(InitializeParamInternal));
        initParamMethod.MakeGenericMethod(paramType).Invoke(null, new object[]{scriptParam, property, settings});
        context.Debug($"ScriptParam initialized: {paramName}");

        return scriptParam;
    }

    public static void InitializeParamInternal<T>(ScriptParam<T> scriptParam, PropertyInfo property, InitializeParamSettings settings = null)
    {
        if(settings.InitFromAttributes)
        {        
            var descriptionAttr = property.GetCustomAttribute<DescriptionAttribute>();
            if(descriptionAttr!=null)
            {
                scriptParam.Description = descriptionAttr.Description;
            }

            var scriptParamAttr = property.GetCustomAttribute<ScriptParamAttribute>();
            if(scriptParamAttr!=null)
            {
                if(scriptParamAttr.IsList)
                    scriptParam.IsList = true;
            }
        }

        if(settings.InitFromArgs)
            scriptParam.SetFromArgs();

        if(settings.InitFromAttributes)
        {
            var defaultValueAttr = property.GetCustomAttribute<DefaultValueAttribute>();
            if(defaultValueAttr!=null)
            {
                scriptParam.SetDefaultValue((T)defaultValueAttr.Value);
            }
        }
    }
}

public static T InitializeObject<T>(this T target, ScriptArgs args)
{
    Initializer.InitializeParams(args.Context, target);
    return target;
}
