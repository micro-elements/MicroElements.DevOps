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
    /// <summary>
    /// Creates new instance of ScriptParam.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="getValueChain">GetValue chain.</param>
    public ScriptParam(string name, params ValueGetter<T>[] getValueChain)
    {
        Name = name;
        GetValueChain.AddRange(getValueChain.NotNull());
    }

    /// <summary>
    /// Creates new instance of ScriptParam.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="defaultValue">Default value.</param>
    public ScriptParam(string name, T defaultValue):
        this(name, new ValueGetter<T>(defaultValue, ParamSource.DefaultValue))
    {
        DefaultValue = defaultValue;
    }

    /// <summary>
    /// GetValue chain.
    /// </summary>
    private List<ValueGetter<T>> GetValueChain {get;} = new List<ValueGetter<T>>();

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

    public ParamValue<T> BuildedValue {get;private set;} = ParamValue<T>.NoValue;
    public T Value { get { return GetBuildedValue(); } }
    public T DefaultValue {get; set;}
    public string FormattedValue => IsSecret? "{Secured}" : this.BuildedValue.HasValue() ? $"{Value}" : "{NoValue}";

    public bool Required {get; set;} = false;
    public T[] ValidValues {get; set;}

    public ScriptParam<T> SetValue(ValueGetter<T> getValue, bool replaceSameSource = true)
    {
        getValue.CheckNotNull(nameof(getValue));
   
        var existed = GetValueChain.FirstOrDefault(gv=>gv.ParamSource==getValue.ParamSource);
        if(replaceSameSource && existed!=null)
        {
            var index = GetValueChain.IndexOf(existed);
            GetValueChain[index] = getValue;
        }
        else
        {
            GetValueChain.Add(getValue);
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

    public ScriptParam<T> SetDefaultValue(T constValue) => SetValue(constValue, ParamSource.DefaultValue);

    public ScriptParam<T> SetDefaultValue(GetSimpleValue<T> getValue) => SetValue(getValue, ParamSource.DefaultValue);

    public ScriptParam<T> SetFromArgs()
    {
        SetValues(ArgumentOrEnvVar<T>(this.Name));
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
        var paramValue = EvaluateValue(args);
        paramValue = CheckRules(paramValue);
        BuildedValue = paramValue;

        args.Context.Information($"PARAM: {Name}={FormattedValue}; SOURCE: {BuildedValue.Source}");
        return this;
    }

    /// <summary>
    /// Evaluates value.
    /// </summary>
    public ParamValue<T> EvaluateValue(ScriptArgs args)
    {
        ParamValue<T> paramValue = ParamValue<T>.NoValue;
        foreach (var getValue in GetValueChain)
        {
            if(getValue.ParamSource==ParamSource.NoValue)
                continue;
            if(getValue.PreCondition!=null && !getValue.PreCondition(args))
                continue;
            paramValue = getValue.GetValue(args) ?? ParamValue<T>.NoValue;
            if(paramValue.HasValue())
                break;
        }

        if(paramValue.HasValue())
            return paramValue;

        if(Required)
            throw new Exception($"Parameter {Name} is required but value is not provided.");

        return paramValue;
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
public delegate ParamValue<T> GetValue<T>(ScriptArgs args);

/// <summary>
/// Simplified GetValue. Translates to ParamValue with ParamSource.Conventions
/// </summary>
public delegate T GetSimpleValue<T>(ScriptArgs args);

/// <summary>
/// Some predicate that indicates ScriptArgs conditions.
/// </summary>
public delegate bool ScriptArgsPredicate<T>(ScriptArgs args);

/// <summary>
/// ValueGetter is value holder and it knows about value source. Also value can be evaluated at runtime.
/// </summary>
/// <typeparam name="T">The type of value.</typeparam>
public class ValueGetter<T>
{
    public ValueGetter(ScriptArgsPredicate<T> preCondition, GetValue<T> getValue, ParamSource paramSource)
    {
        GetValue = getValue.CheckNotNull(nameof(getValue));
        ParamSource = paramSource.CheckNotNull(nameof(paramSource));
        PreCondition = preCondition;
    }

    public ValueGetter(ScriptArgsPredicate<T> preCondition, GetSimpleValue<T> getValue, ParamSource paramSource)
    {
        getValue.CheckNotNull(nameof(getValue));
        GetValue = a => getValue(a).ToParamValue(paramSource);
        ParamSource = paramSource.CheckNotNull(nameof(paramSource));
        PreCondition = preCondition;
    }

    public ValueGetter(GetSimpleValue<T> getValue, ParamSource paramSource)
        :this(null, getValue, paramSource)
    {
    }

    public ValueGetter(T constantValue, ParamSource paramSource)
    {
        constantValue.CheckNotNull(nameof(constantValue));
        GetValue = a => constantValue.ToParamValue(paramSource);
        ParamSource = paramSource.CheckNotNull(nameof(paramSource));
    }

    public ScriptArgsPredicate<T> PreCondition {get;}
    public GetValue<T> GetValue {get;}
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
/// Describes the name of ScriptParam.
/// </summary>
[AttributeUsage(AttributeTargets.Property|AttributeTargets.Field)]
public class ParamNameAttribute : Attribute
{
    /// <summary>
    /// Param name.
    /// </summary>
    public string Name {get;}

    /// <summary>
    /// Creates ParamNameAttribute.
    /// </summary>
    /// <param name="name">The name of param.</param>
    public ParamNameAttribute(string name)
    {
        Name = name;
    }
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

        var paramNameAttr = property.GetCustomAttribute<ParamNameAttribute>();
        string paramName = paramNameAttr?.Name ?? property.Name;
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
            var defaultValueAttr = property.GetCustomAttribute<DefaultValueAttribute>();
            if(defaultValueAttr!=null)
            {
                scriptParam.SetDefaultValue((T)defaultValueAttr.Value);
            }
            
            var descriptionAttr = property.GetCustomAttribute<DescriptionAttribute>();
            if(descriptionAttr!=null)
            {
                scriptParam.Description = descriptionAttr.Description;
            }
        }

        if(settings.InitFromArgs)
            scriptParam.SetValues(ArgumentOrEnvVar<T>(scriptParam.Name));
    }
}
