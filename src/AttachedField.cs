using System;
using System.Collections.Generic;

namespace RegionKit;

/// <summary>
/// A collection that attaches values to objects using <see cref="WeakReference"/>. By Slime_Cubed
/// </summary>
/// <remarks>
/// This is like ConditionalWeakTable, but with one major drawback:
/// values that reference the key will stop the key from being garbage collected.
/// <para>Make sure that each instance of <typeparamref name="TValue"/> contains
/// no references to the key, otherwise a memory leak may occur!</para>
/// </remarks>
/// <typeparam name="TKey">The type to attach the value to.</typeparam>
/// <typeparam name="TValue">The type the the attached value.</typeparam>
public class AttachedField<TKey, TValue>
{
	private static IEqualityComparer<object> _comparer = new KeyComparer();
	/// <summary>
	/// Called after a key is garbage collected.
	/// </summary>
	public event Action<WeakReference, TValue> OnCulled;
	private Dictionary<object, TValue> _dict = new(_comparer);
	private int _lastGCCount = 0;

	/// <summary>
	/// Updates or attaches a value to an object.
	/// </summary>
	/// <param name="obj">The object to attach to.</param>
	/// <param name="value">The value to set.</param>
	public void Set(TKey obj, TValue value)
	{
		if (obj == null) return;
		if (_dict.ContainsKey(obj)) _dict[obj] = value;
		else Attach(obj, value);
	}

	/// <summary>
	/// Detaches a value from an object.
	/// </summary>
	/// <param name="obj">The object to remove the attached value from.</param>
	public void Unset(TKey obj)
	{
		if (obj == null) return;
		_dict.Remove(obj);
	}

	private void Attach(TKey key, TValue value)
	{
		_dict[new WeakRefWithHash(key)] = value;
		// Only bother performing garbage collection when the dictionary increases in size
		CullDead();
	}

	/// <summary>
	/// Retrieves a stored value for a given object.
	/// </summary>
	/// <param name="obj">The object to get from.</param>
	/// <returns>The previously set value for this object, or default(<typeparamref name="TValue"/>) if unset.</returns>
	public TValue Get(TKey obj)
	{
		if (obj == null) throw new ArgumentNullException(nameof(obj));
		if (_dict.TryGetValue(obj, out TValue value)) return value;
		return default;
	}

	/// <summary>
	/// Checks for and retrieves a stored value for a given object.
	/// </summary>
	/// <param name="obj">The object to get from.</param>
	/// <param name="value">The previously set value for this obejct.</param>
	/// <returns>True if a value exists for <paramref name="obj"/>, false otherwise.</returns>
	public bool TryGet(TKey obj, out TValue value)
	{
		if (obj == null)
		{
			value = default;
			return false;
		}
		return _dict.TryGetValue(obj, out value);
	}

	/// <summary>
	/// Sets or retrieves the value attached to object.
	/// </summary>
	/// <param name="obj">The object key.</param>
	/// <returns>The attached value, or default(<typeparamref name="TValue"/>) if the value has not been set.</returns>
	public TValue this[TKey obj]
	{
		get => Get(obj);
		set => Set(obj, value);
	}

	/// <summary>
	/// Clears all entries.
	/// </summary>
	public void Clear() => _dict.Clear();

	/// <summary>
	/// The number of entries currently stored.
	/// </summary>
	public int Count => _dict.Count;

	private List<KeyValuePair<object, TValue>> _toRemove = new();
	/// <summary>
	/// Removes entries for which the key has been garbage collected.
	/// </summary>
	public void CullDead()
	{
		// Assume the referenced objects are long-lived
		// Only cull dead refs when the garbage collector runs
		int gcCount = GC.CollectionCount(2);
		if (gcCount != _lastGCCount) _lastGCCount = gcCount;
		else return;
		// Search for dead references
		foreach (KeyValuePair<object, TValue> pair in _dict)
		{
			if (!(pair.Key is WeakReference wr) || (!wr.IsAlive)) _toRemove.Add(pair);
		}
		// Remove them from the dictionary
		for (int i = _toRemove.Count - 1; i >= 0; i--)
		{
			OnCulled?.Invoke(_toRemove[i].Key as WeakReference, _toRemove[i].Value);
			_dict.Remove(_toRemove[i].Key);
		}
		_toRemove.Clear();
	}

	private class WeakRefWithHash : WeakReference
	{
		public int hash;

		public WeakRefWithHash(object target) : base(target) => hash = target.GetHashCode();

		public override int GetHashCode() => hash;
	}

	private class KeyComparer : IEqualityComparer<object>
	{
		public new bool Equals(object x, object y)
		{
			// Treat WeakReferences and the objects they reference as equal
			// ... and treat WeakReferences that reference the same object as equal
			if (x is WeakReference wrx)
			{
				x = wrx.Target;
				if (!wrx.IsAlive) x = wrx;
			}
			if (y is WeakReference wry)
			{
				y = wry.Target;
				if (!wry.IsAlive) y = wry;
			}
			return x == y;
		}

		// WeakRefWithHash already overrides GetHashCode, so no change needs to be made
		public int GetHashCode(object obj) => obj.GetHashCode();
	}
}
