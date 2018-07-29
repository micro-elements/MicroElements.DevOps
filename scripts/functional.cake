// Some functional stuff

public static IEnumerable<T> NotNull<T>(this IEnumerable<T> collection) => collection ?? Array.Empty<T>();

public static ICollection<T> NotNull<T>(this ICollection<T> collection) => collection ?? Array.Empty<T>();

public static T[] NotNull<T>(this T[] collection) => collection ?? Array.Empty<T>();

public static IEnumerable<T> AsEnumerable<T>(this T value) => new T[]{value};

public static IEnumerable<T> AsEnumerable<T>(this object value) => (new T[]{(T)value});

public static Func<T1, T3> Compose<T1, T2, T3>(this Func<T2, T3> f, Func<T1, T2> g) => x => f(g(x));

public static Func<T1, T3> Then<T1, T2, T3>(this Func<T1, T2> f, Func<T2, T3> g) => x => g(f(x));

public static Func<T, T> ToFunc<T>(this Action<T> act) => x => { act(x); return x; };

public static Action<T> ToAct<T>(this Func<T, T> f) => x => { f(x); };

public static T2 Pipe<T1, T2>(this T1 arg, Func<T1, T2> f) => f(arg);

public static Func<T1, Func<T2, TResult>> Curry<T1, T2, TResult>(this Func<T1, T2, TResult> func) =>
    x1 => x2 => func(x1, x2);

public static void ForEach<T1, T2>(this IEnumerable<T1> values, Func<T1, T2> f) => values.Select(value=>f(value)).ToList();
       