using System.Reflection;

namespace RegionKit.Extras;

public static class ReflectionTools
{


	#region refl flag templates
	/// <summary>
	/// Binding flags for all normal contexts.
	/// </summary>
	public const BindingFlags BF_ALL_CONTEXTS
		= BindingFlags.Public
		| BindingFlags.NonPublic
		| BindingFlags.Instance
		| BindingFlags.Static;
	/// <summary>
	/// Binding flags for all instance members regardless of visibility.
	/// </summary>
	public const BindingFlags BF_ALL_CONTEXTS_INSTANCE
		= BindingFlags.Public
		| BindingFlags.NonPublic
		| BindingFlags.Instance;
	/// <summary>
	/// Binding flags for all static members regardless of visibility.
	/// </summary>
	public const BindingFlags BF_ALL_CONTEXTS_STATIC
		= BindingFlags.Public
		| BindingFlags.NonPublic
		| BindingFlags.Static;
	/// <summary>
	/// Binding flags for all constructors.
	/// </summary>
	public const BindingFlags BF_ALL_CONTEXTS_CTOR
		= BindingFlags.Public
		| BindingFlags.NonPublic
		| BindingFlags.CreateInstance;
	#endregion
	#region refl helpers
	/// <summary>
	/// Gets a method regardless of visibility.
	/// </summary>
	/// <param name="self">Type to get methods from</param>
	/// <param name="name">Name of the method</param>
	/// <returns></returns>
	public static MethodInfo? GetMethodAllContexts(
		this Type self,
		string name)
	{
		return self.GetMethod(name, BF_ALL_CONTEXTS);
	}

	/// <summary>
	/// Gets
	/// </summary>
	/// <param name="self"></param>
	/// <param name="name"></param>
	/// <returns></returns>
	public static PropertyInfo? GetPropertyAllContexts(
		this Type self,
		string name)
	{
		return self.GetProperty(name, BF_ALL_CONTEXTS);
	}

	/// <summary>
	/// Returns autoimplemented property backing field name
	/// </summary>
	public static string BackingFieldName(string propname)
	{
		return $"<{propname}>k__BackingField";
	}

	/// <summary>
	/// Looks up methodinfo from T, defaults to <see cref="BF_ALL_CONTEXTS_INSTANCE"/>
	/// </summary>
	/// <typeparam name="T">Target type</typeparam>
	/// <param name="mname">Method name</param>
	/// <param name="context">Binding flags, default private+public+instance</param>
	/// <returns></returns>
	public static MethodInfo? methodof<T>(
		string mname,
		BindingFlags context = BF_ALL_CONTEXTS_INSTANCE)
	{
		return typeof(T).GetMethod(mname, context);
	}
	/// <summary>
	/// Looks up methodinfo from t, defaults to <see cref="BF_ALL_CONTEXTS_STATIC"/>
	/// </summary>
	/// <param name="t">Target type</param>
	/// <param name="mname">Method name</param>
	/// <param name="context">Binding flags, default private+public+static</param>
	/// <returns></returns>
	public static MethodInfo? methodof(
		Type t,
		string mname,
		BindingFlags context = BF_ALL_CONTEXTS_STATIC)
	{
		return t.GetMethod(mname, context);
	}

	/// <summary>
	/// Gets constructorinfo from T. no cctors by default.
	/// </summary>
	/// <typeparam name="T">Type to look at</typeparam>
	/// <param name="context">Binding flags. Does not include static constructors by default.</param>
	/// <param name="pms">Constructor parameter types.</param>
	/// <returns></returns>
	public static ConstructorInfo? ctorof<T>(
		BindingFlags context = BF_ALL_CONTEXTS_CTOR,
		params Type[] pms)
	{
		return typeof(T).GetConstructor(context, null, pms, null);
	}

	/// <summary>
	/// Gets constructorinfo from T.
	/// </summary>
	/// <typeparam name="T">Type to look at.</typeparam>
	/// <param name="pms">Constructor parameter types.</param>
	/// <returns></returns>
	public static ConstructorInfo? ctorof<T>(params Type[] pms)
	{
		return typeof(T).GetConstructor(pms);
	}

	/// <summary>
	/// Takes fieldinfo from T, defaults to <see cref="BF_ALL_CONTEXTS_INSTANCE"/>
	/// </summary>
	/// <typeparam name="T">Target type</typeparam>
	/// <param name="name">Field name</param>
	/// <param name="context">Context, default private+public+instance</param>
	/// <returns></returns>
	public static FieldInfo? fieldof<T>(string name, BindingFlags context = BF_ALL_CONTEXTS_INSTANCE)
	{
		return typeof(T).GetField(name, context);
	}

	public static Type[] GetTypesSafe(this Assembly asm, out ReflectionTypeLoadException? err)
	{
		err = null;
		Type[] types;
		try
		{
			types = asm.GetTypes();
		}
		catch (ReflectionTypeLoadException e)
		{
			types = e.Types.Where(t => t != null).ToArray();
			err = e;
		}

		return types;
	}
	/// <summary>
	/// Yields all loaded assemblies with names matching a given regex.
	/// </summary>
	/// <param name="pattern">Regular expression to filter assemblies</param>
	/// <returns>A yield ienumerable with results</returns>

	public static IEnumerable<Assembly> FindAssemblies(string pattern)
	{
		Assembly[] lasms = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = lasms.Length - 1; i > -1; i--)
			if (System.Text.RegularExpressions.Regex.Match(lasms[i].FullName, pattern).Success) yield return lasms[i];
	}
	/// <summary>
	/// Force clones an object instance through reflection
	/// </summary>
	/// <typeparam name="T">Object type</typeparam>
	/// <param name="from">Source object</param>
	/// <param name="to">Destination object</param>
	/// <param name="context">Specifies context of fields to be cloned</param>
	public static void CloneInstance<T>(
		T from,
		T to,
		BindingFlags context = BF_ALL_CONTEXTS_INSTANCE)
	{
		Type tt = typeof(T);
		foreach (FieldInfo field in tt.GetFields(context))
		{
			if (field.IsStatic) continue;
			field.SetValue(to, field.GetValue(from), context, null, System.Globalization.CultureInfo.CurrentCulture);
		}
	}
	/// <summary>
	/// Cleans up static reference members in a type.
	/// </summary>
	/// <param name="t">Target type</param>
	public static (List<string>, List<string>) CleanupStatic(this Type t)
	{
		List<string> success = new();
		List<string> failure = new();

		foreach (FieldInfo field in t.GetFields(BF_ALL_CONTEXTS_STATIC))
			if (!field.FieldType.IsValueType)
			{
				string fullname = $"{t.FullName}.{field.Name}";
				try
				{
					field.SetValue(null, null, BF_ALL_CONTEXTS_STATIC, null, System.Globalization.CultureInfo.CurrentCulture);
					success.Add(fullname);
				}
				catch (Exception ex)
				{
					failure.Add(fullname + $" (exception: {ex.Message})");
				}
			}
		foreach (Type nested in t.GetNestedTypes(BF_ALL_CONTEXTS_STATIC))
		{
			var res = nested.CleanupStatic();
			success.AddRange(res.Item1);
			failure.AddRange(res.Item2);
		}
		return (success, failure);
	}

	/// <summary>
	/// Generic wrapper for <see cref="Delegate.CreateDelegate(Type, object, MethodInfo)"/>.
	/// </summary>
	/// <typeparam name="T">Delegate type</typeparam>
	/// <param name="inst">Instance. Set to null to treat method as static.</param>
	/// <param name="method">Target method.</param>
	/// <returns>Resulting delegate; null if failure.</returns>
	public static T? GetFn<T>(object? inst, MethodInfo method)
		where T : MulticastDelegate
	{
		BangBang(method, nameof(method));
		return (T)Delegate.CreateDelegate(typeof(T), inst, method);
	}
	/// <summary>
	/// Gets an internal char setter for given string.
	/// </summary>
	/// <param name="s"></param>
	/// <returns></returns>
	public static Action<int, char> Fn_SetChar(this string s)
	{
		MethodInfo method = methodof<string>("InternalSetChar", BF_ALL_CONTEXTS_INSTANCE)!;
		return GetFn<Action<int, char>>(s, method)!;
	}
    /// <summary>
	/// cleans up all non valuetype fields in a type. for realm cleanups
	/// </summary>
	/// <param name="t"></param>
	internal static void CleanUpStatic(this Type t)
	{
		foreach (var fld in t.GetFields(BF_ALL_CONTEXTS_STATIC))
		{
			try
			{
				if (fld.FieldType.IsValueType || fld.IsLiteral) continue;
				fld.SetValue(null, default);
			}
			catch { }

		}
		foreach (var child in t.GetNestedTypes()) child.CleanUpStatic();
	}
	#endregion

}