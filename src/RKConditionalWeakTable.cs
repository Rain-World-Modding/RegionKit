using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Menu.Remix.MixedUI;

namespace RegionKit
{
	/// <summary>
	/// <para>
	/// Memory-safe replacement for <see cref="ConditionalWeakTable{TKey, TValue}"/>,
	/// which has a bug in Unity's .NET implementation where keys will never be properly cleared due to the garbage collector
	/// not implementing some important things correctly. As a bonus, this version also supports non-reference type values,
	/// all while conforming to a slightly more modern definition of ConditionalWeakTable, so you get the luxury of some extra methods.
	/// </para>
	/// <para>
	/// Credit goes to MVV012 and SlimeCubed for their own implementations of WeakTable and AttachedField respectively,
	/// which were used as a reference for this code. MVV012's WeakTable is licensed under CC0 1.0 while SlimeCubed's
	/// AttachedField is licensed under the MIT license. 
	/// </para>
	/// </summary>
	/// <remarks>
	/// <c>GetOrCreateValue</c> is not implemented as it is an expensive operation to perform using generics.
	/// </remarks>
	/// <typeparam name="TKey">The table's key type. Must be a reference type.</typeparam>
	/// <typeparam name="TValue">The table's value type. Does not have to be a reference type.</typeparam>
	public sealed class RKConditionalWeakTable<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TKey : class
	{
		// Implementation notes from MVV012's WeakTable: GCHandle and WeakReference are 6x slower than accessing from a dict,
		// so we access by computing the hash, basically making this class a more complicated hash table. On average, their
		// WeakTable seems to perform faster than a CWT, though I have not benchmarked this version.

		[StructLayout(LayoutKind.Auto)]
		private struct Entry
		{
			public int next;
			public int hash;
			public GCHandle key;
			public TValue value;
		}

		private int[] _buckets;
		private Entry[] _entries;
		private int _allocatedEntries = 0;
		private int _freeList = -1;
		private int _lastGCCount;
		private int _checkGCCount = 0;

		public RKConditionalWeakTable()
		{
			// Initial capacity for this needs to be a power of 2.
			// MVV012 used 8, Unity's Mono's implementation uses 13
			// so I chose 16 to be closer to that.
			int initialCapacity = 16;

			_buckets = new int[initialCapacity];
			for (int i = 0; i < initialCapacity; i++)
			{
				_buckets[i] = -1;
			}
			_entries = new Entry[initialCapacity];
			_lastGCCount = GC.CollectionCount(0);

			//__RKCWTDebugView.CreateDebugViewFor(this);
		}

		/// <summary>
		/// Adds a key to the table.
		/// </summary>
		/// <param name="key">The key to add. Represents the object to which the property is attached. May not be null.</param>
		/// <param name="value">The key's property value.</param>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
		/// <exception cref="ArgumentException"><paramref name="key"/> already exists as a key</exception>
		public void Add(TKey key, TValue value)
		{
			if (!TryAdd(key, value))
			{
				throw new ArgumentException($"{nameof(key)} already exists!");
			}
		}

		/// <summary>
		/// Adds the key and value if the key doesn't exist, or updates the existing key's value if it does exist.
		/// </summary>
		/// <param name="key">The key to add or update. May not be null.</param>
		/// <param name="value">The value to associate with key.</param>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
		public void AddOrUpdate(TKey key, TValue value)
		{
			if (key is null) throw new ArgumentNullException(nameof(key), "Null key");

			// Handle precursor operations
			if (_allocatedEntries == _entries.Length)
				Expand();
			else
				CullDeadIfNeeded();

			// Compute bucket and entry index
			int hash = RuntimeHelpers.GetHashCode(key);
			ref int bucket = ref GetBucket(hash);
			ref int entryIndex = ref bucket;

			// Try to find it if it exists currently
			while (entryIndex != -1)
			{
				ref Entry entry = ref _entries[entryIndex];
				if (entry.hash == hash)
				{
					object? keyTarget = entry.key.Target;
					if (keyTarget == null)
					{
						// Entry needs to be discarded
						RemoveEntry(ref entryIndex, ref entry);
						continue;
					}
					else if (ReferenceEquals(keyTarget, key))
					{
						// Found the key, update the value
						entry.value = value;
						return;
					}
				}
				entryIndex = ref entry.next;
			}

			// If we are here, it doesn't exist. Add it to the table and return true
			if (_freeList != -1)
			{
				// We can reuse an existing free slot
				int freeIndex = _freeList;
				ref Entry freeEntry = ref _entries[freeIndex];
				_freeList = freeEntry.next;
				freeEntry.key.Target = key;
				freeEntry.hash = hash;
				freeEntry.next = bucket;
				freeEntry.value = value;
				bucket = freeIndex;
			}
			else
			{
				// Allocate a new slot
				int newIndex = _allocatedEntries++;
				ref Entry newEntry = ref _entries[newIndex];
				newEntry.key = GCHandle.Alloc(key, GCHandleType.Weak);
				newEntry.hash = hash;
				newEntry.next = bucket;
				newEntry.value = value;
				bucket = newIndex;
			}
		}

		/// <summary>
		/// Adds a key to the table if it doesn't already exist.
		/// </summary>
		/// <param name="key">The key to add. May not be null.</param>
		/// <param name="value">The key's property value.</param>
		/// <returns><c>true</c> if the key/value pair was added; <c>false</c> if the table already contained the key.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
		public bool TryAdd(TKey key, TValue value)
		{
			if (key is null) throw new ArgumentNullException(nameof(key), "Null key");

			// Handle precursor operations
			if (_allocatedEntries == _entries.Length)
				Expand();
			else
				CullDeadIfNeeded();

			// Compute bucket and entry index
			int hash = RuntimeHelpers.GetHashCode(key);
			ref int bucket = ref GetBucket(hash);
			ref int entryIndex = ref bucket;

			// Try to find it if it exists currently
			while (entryIndex != -1)
			{
				ref Entry entry = ref _entries[entryIndex];
				if (entry.hash == hash)
				{
					object? keyTarget = entry.key.Target;
					if (keyTarget == null)
					{
						// Entry needs to be discarded
						RemoveEntry(ref entryIndex, ref entry);
						continue;
					}
					else if (ReferenceEquals(keyTarget, key))
					{
						// Key already exists, we don't need to add. Return false
						return false;
					}
				}
				entryIndex = ref entry.next;
			}

			// If we are here, it doesn't exist. Add it to the table and return true
			if (_freeList != -1)
			{
				// We can reuse an existing free slot
				int freeIndex = _freeList;
				ref Entry freeEntry = ref _entries[freeIndex];
				_freeList = freeEntry.next;
				freeEntry.key.Target = key;
				freeEntry.hash = hash;
				freeEntry.next = bucket;
				freeEntry.value = value;
				bucket = freeIndex;
			}
			else
			{
				// Allocate a new slot
				int newIndex = _allocatedEntries++;
				ref Entry newEntry = ref _entries[newIndex];
				newEntry.key = GCHandle.Alloc(key, GCHandleType.Weak);
				newEntry.hash = hash;
				newEntry.next = bucket;
				newEntry.value = value;
				bucket = newIndex;
			}
			return true;
		}

		/// <summary>
		/// Searches for a specified key in the table and returns the corresponding value. 
		/// If the key does not exist in the table, the method adds the given value and binds it to the specified key.
		/// </summary>
		/// <param name="key">The key of the value to find. May not be null.</param>
		/// <param name="value">The value to add and bind to <paramref name="key"/>, if one does not exist already.</param>
		/// <returns>The value bound to <paramref name="key"/> after the method completes.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
		public TValue GetOrAdd(TKey key, TValue value)
		{
			if (key is null) throw new ArgumentNullException(nameof(key), "Null key");

			// Handle precursor operations
			if (_allocatedEntries == _entries.Length)
				Expand();
			else
				CullDeadIfNeeded();

			// Compute bucket and entry index
			int hash = RuntimeHelpers.GetHashCode(key);
			ref int bucket = ref GetBucket(hash);
			ref int entryIndex = ref bucket;

			// Try to find it if it exists currently
			while (entryIndex != -1)
			{
				ref Entry entry = ref _entries[entryIndex];
				if (entry.hash == hash)
				{
					object? keyTarget = entry.key.Target;
					if (keyTarget == null)
					{
						// Entry needs to be discarded
						RemoveEntry(ref entryIndex, ref entry);
						continue;
					}
					else if (ReferenceEquals(keyTarget, key))
					{
						// We have found the key, retrieve the value
						return entry.value;
					}
				}
				entryIndex = ref entry.next;
			}

			// If we are here, it doesn't exist. 
			if (_freeList != -1)
			{
				// We can reuse an existing free slot
				int freeIndex = _freeList;
				ref Entry freeEntry = ref _entries[freeIndex];
				_freeList = freeEntry.next;
				freeEntry.key.Target = key;
				freeEntry.hash = hash;
				freeEntry.next = bucket;
				freeEntry.value = value;
				bucket = freeIndex;
			}
			else
			{
				// Allocate a new slot
				int newIndex = _allocatedEntries++;
				ref Entry newEntry = ref _entries[newIndex];
				newEntry.key = GCHandle.Alloc(key, GCHandleType.Weak);
				newEntry.hash = hash;
				newEntry.next = bucket;
				newEntry.value = value;
				bucket = newIndex;
			}
			return value;
		}

		/// <summary>
		/// Searches for a specified key in the table and returns the corresponding value. 
		/// If the key does not exist in the table, the method invokes the supplied factory to create a value that is bound to the specified key.
		/// </summary>
		/// <param name="key">The key of the value to find. May not be null.</param>
		/// <param name="valueFactory">The callback that creates a value for <paramref name="key"/>, if one does not exist already.</param>
		/// <returns>The value bound to <paramref name="key"/> after the method completes.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
		public TValue GetValue(TKey key, Func<TKey, TValue> valueFactory)
		{
			if (key is null) throw new ArgumentNullException(nameof(key), "Null key");

			// Handle precursor operations
			if (_allocatedEntries == _entries.Length)
				Expand();
			else
				CullDeadIfNeeded();

			// Compute bucket and entry index
			int hash = RuntimeHelpers.GetHashCode(key);
			ref int bucket = ref GetBucket(hash);
			ref int entryIndex = ref bucket;

			// Try to find it if it exists currently
			while (entryIndex != -1)
			{
				ref Entry entry = ref _entries[entryIndex];
				if (entry.hash == hash)
				{
					object? keyTarget = entry.key.Target;
					if (keyTarget == null)
					{
						RemoveEntry(ref entryIndex, ref entry);
						continue;
					}
					else if (ReferenceEquals(keyTarget, key))
					{
						return entry.value;
					}
				}
				entryIndex = ref entry.next;
			}

			// If we are here, it doesn't exist. Create the new value
			TValue value = valueFactory.Invoke(key);
			if (_freeList != -1)
			{
				// We can reuse an existing free slot
				int freeIndex = _freeList;
				ref Entry freeEntry = ref _entries[freeIndex];
				_freeList = freeEntry.next;
				freeEntry.key.Target = key;
				freeEntry.hash = hash;
				freeEntry.next = bucket;
				freeEntry.value = value;
				bucket = freeIndex;
				return value;
			}
			else
			{
				// Allocate a new slot
				int newIndex = _allocatedEntries++;
				ref Entry newEntry = ref _entries[newIndex];
				newEntry.key = GCHandle.Alloc(key, GCHandleType.Weak);
				newEntry.hash = hash;
				newEntry.next = bucket;
				newEntry.value = value;
				bucket = newIndex;
				return value;
			}
		}

		/// <summary>
		/// Searches for a specified key in the table and returns the corresponding value. 
		/// If the key does not exist in the table, the method invokes the supplied factory to create a value that is bound to the specified key.
		/// </summary>
		/// <typeparam name="TArg">The type of the additional argument to use with the value factory.</typeparam>
		/// <param name="key">The key of the value to find. May not be null.</param>
		/// <param name="valueFactory">The callback that creates a value for <paramref name="key"/>, if one does not exist already.</param>
		/// <param name="factoryArgument">The additional argument to supply to <paramref name="valueFactory"/> upon invocation.</param>
		/// <returns>The value bound to <paramref name="key"/> after the method completes.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
		public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
		{
			return GetValue(key, k => valueFactory.Invoke(k, factoryArgument));
		}

		/// <summary>
		/// Gets the value of the specified key if it exists.
		/// </summary>
		/// <param name="key">The key that represents an object with an attached property. May not be null.</param>
		/// <param name="value">
		/// When this method returns, contains the attached property value. If <paramref name="key"/> is not found, value contains the default value.
		/// </param>
		/// <returns><c>true</c> if <paramref name="key"/> is found; otherwise, <c>false</c>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
		public bool TryGetValue(TKey key, out TValue value)
		{
			if (key is null) throw new ArgumentNullException(nameof(key), "Null key");

			value = default!;

			int hash = RuntimeHelpers.GetHashCode(key);
			ref int entryIndex = ref GetBucket(hash);

			while (entryIndex != -1)
			{
				ref Entry entry = ref _entries[entryIndex];
				if (entry.hash == hash)
				{
					object? keyTarget = entry.key.Target;
					if (keyTarget == null)
					{
						// Entry needs to be discarded
						RemoveEntry(ref entryIndex, ref entry);
						continue;
					}
					else if (ReferenceEquals(keyTarget, key))
					{
						// We have found the key, retrieve the value
						value = entry.value;
						CullDeadIfNeeded();
						return true;
					}
				}
				entryIndex = ref entry.next;
			}
			value = default!;
			return false;
		}

		/// <summary>
		/// Removes a key and its value from the table.
		/// </summary>
		/// <param name="key">The key to remove. May not be null.</param>
		/// <returns><c>true</c> if the key is found and removed; otherwise, <c>false</c>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
		public bool Remove(TKey key)
		{
			if (key is null) throw new ArgumentNullException(nameof(key), "Null key");

			return Remove(key, out _);
		}

		/// <summary>
		/// Removes a key and its value from the table, and returns the removed value if it was present.
		/// </summary>
		/// <param name="key">The key to remove. May not be null.</param>
		/// <param name="value">The value associated with <paramref name="key"/> before removal, if present.</param>
		/// <returns><c>true</c> if the key is found and removed; otherwise, <c>false</c>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
		public bool Remove(TKey key, out TValue? value)
		{
			if (key is null) throw new ArgumentNullException(nameof(key), "Null key");

			// Default value
			value = default;

			// Get potential bucket location
			int hash = RuntimeHelpers.GetHashCode(key);
			ref int entryIndex = ref GetBucket(hash);

			// Try to find the matching entry
			while (entryIndex != -1)
			{
				ref Entry entry = ref _entries[entryIndex];
				if (entry.hash == hash)
				{
					object? keyTarget = entry.key.Target;
					if (keyTarget == null)
					{
						// Entry that needs to be removed anyway
						RemoveEntry(ref entryIndex, ref entry);
						continue;
					}
					else if (ReferenceEquals(keyTarget, key))
					{
						// Matches the entry! Return the value
						value = entry.value;

						RemoveEntry(ref entryIndex, ref entry);
						CullDeadIfNeeded();
						return true;
					}
				}
				entryIndex = ref entry.next;
			}

			return false;
		}

		/// <summary>
		/// Clears all key-value pairs from the table.
		/// </summary>
		public void Clear()
		{
			for (int i = 0; i < _allocatedEntries; i++)
			{
				ref Entry entry = ref _entries[i];
				entry.key.Free();
				entry.value = default!;
			}
			for (int i = 0; i < _buckets.Length; i++)
			{
				_buckets[i] = -1;
			}
			_allocatedEntries = 0;
			_freeList = -1;
			_lastGCCount = GC.CollectionCount(0);
			_checkGCCount = 0;
		}

		/// <summary>
		/// Forces a cull of garbage collected keys.
		/// You should not need to call this most of the time, as it will be done automatically via other methods at intervals.
		/// </summary>
		public void CullDead()
		{
			ActuallyCullDead();
		}

		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <inheritdoc/>
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			// Force a cull
			CullDeadIfNeeded();

			// Loop through buckets and find active entries to yield
			for (int i = 0; i < _buckets.Length; i++)
			{
				int entryIndex = _buckets[i];
				while (entryIndex != -1)
				{
					Entry entry = _entries[entryIndex];
					if (entry.key.Target is TKey key)
					{
						yield return new KeyValuePair<TKey, TValue>(key, entry.value);
					}
				}
			}
		}

		private void CullDeadIfNeeded()
		{
			// On Unity's Mono implementation, GC.CollectionCount() returns the same values for 0, 1, and 2.
			// Dead referencess are only collected every so many GC intervals

			const int CheckGCCountInterval = 4;
			const int CullDeadAfterGCCollections = 8;

			if (++_checkGCCount < CheckGCCountInterval)
				return;
			_checkGCCount = 0;

			int gcCount = GC.CollectionCount(0);
			if (gcCount - _lastGCCount < CullDeadAfterGCCollections)
				return;
			_lastGCCount = gcCount;

			ActuallyCullDead();
		}

		private void ActuallyCullDead()
		{
			for (int i = 0; i < _buckets.Length; i++)
			{
				ref int entryIndex = ref _buckets[i];
				while (entryIndex != -1)
				{
					ref Entry entry = ref _entries[entryIndex];
					if (entry.key.Target == null)
					{
						RemoveEntry(ref entryIndex, ref entry);
					}
					else
					{
						entryIndex = ref entry.next;
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void RemoveEntry(ref int pointedBy, ref Entry removedEntry)
		{
			removedEntry.value = default!;

			int deadEntryIndex = pointedBy;
			pointedBy = removedEntry.next;
			removedEntry.next = _freeList;
			_freeList = deadEntryIndex;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ref int GetBucket(int hash)
		{
			return ref _buckets[(hash >> 8) & (_buckets.Length - 1)];
		}

		private void Expand()
		{
			int oldSize = _buckets.Length;
			int newSize = oldSize << 1;

			Entry[] oldEntries = _entries;
			int oldAllocated = _allocatedEntries;

			int freeEntryIndex = _freeList;
			while (freeEntryIndex != -1)
			{
				ref Entry freeEntry = ref oldEntries[freeEntryIndex];
				freeEntry.key.Free();
				freeEntryIndex = freeEntry.next;
			}

			_buckets = new int[newSize];
			for (int i = 0; i < newSize; i++)
			{
				_buckets[i] = -1;
			}
			_entries = new Entry[newSize];
			_allocatedEntries = 0;
			_freeList = -1;
			_lastGCCount = GC.CollectionCount(0);

			for (int i = 0; i < oldAllocated; i++)
			{
				ref Entry oldEntry = ref oldEntries[i];
				if (!oldEntry.key.IsAllocated)
				{
					// It was entry from free list
					continue;
				}
				if (oldEntry.key.Target == null)
				{
					oldEntry.key.Free();
					continue;
				}

				int newEntryIndex = _allocatedEntries++;
				ref Entry newEntry = ref _entries[newEntryIndex];

				int hash = oldEntry.hash;
				ref int newBucket = ref GetBucket(hash);

				newEntry.key = oldEntry.key;
				newEntry.value = oldEntry.value;
				newEntry.hash = hash;
				newEntry.next = newBucket;
				newBucket = newEntryIndex;
			}
		}

#if DEBUG
		internal List<bool> __DebugYieldFilled()
		{
			CullDead();
			return _entries.Select(x => x.key.IsAllocated && x.key.Target != null).ToList();
		}
#endif

		~RKConditionalWeakTable()
		{
			// Free all the keys
			for (int i = 0; i < _allocatedEntries; i++)
			{
				_entries[i].key.Free();
			}
		}
	}

#if DEBUG
#nullable disable
	internal static class __RKCWTDebugView
	{
		private static float createAtY = 20f;

		public static void CreateDebugViewFor<TKey, TValue>(RKConditionalWeakTable<TKey, TValue> table) where TKey : class
		{
			GameObject gameObject = new GameObject($"RK CWT debug <{typeof(TKey).Name},{typeof(TValue).Name}>");
			DebugDrawerMonoBehaviour drawer = gameObject.AddComponent<DebugDrawerMonoBehaviour>();
			drawer.GiveData(table, createAtY);
			createAtY += 18f;
		}

		private class DebugDrawerMonoBehaviour : MonoBehaviour
		{
			public DebugDrawerBase drawer;

			public void GiveData<TKey, TValue>(RKConditionalWeakTable<TKey, TValue> table, float yOffset) where TKey : class
			{
				drawer = new DebugDrawer<TKey, TValue>(table, yOffset);
			}

			public void Update()
			{
				drawer.Update();
			}
		}

		private abstract class DebugDrawerBase
		{
			public abstract void Update();
		}

		private sealed class DebugDrawer<TKey, TValue> : DebugDrawerBase where TKey : class
		{
			public RKConditionalWeakTable<TKey, TValue> table;

			public bool inititalized = false;
			public float yOffset;
			public FContainer container;
			public FLabel label;
			public string font = "font";
			public List<FSprite> sprites;
			public Color useColor;
			private int everyOther = 0;

			public FAtlasElement onElement, offElement;

			public string TypeName(Type type)
			{
				string name = type.Name;
				if (type.IsGenericType)
				{
					name += "<";
					for (int i = 0; i < type.GenericTypeArguments.Length; i++)
					{
						if (i > 0) name += ",";

						Type gen = type.GenericTypeArguments[i];
						name += gen.Name;
					}
					name += ">";
				}
				return name;
			}

			public DebugDrawer(RKConditionalWeakTable<TKey, TValue> table, float yOffset)
			{
				this.table = table;
				this.yOffset = yOffset;
				useColor = Custom.HSL2RGB((GetHashCode() & 65535) / 65536f, 1f, 0.5f);
			}

			public void TryInit()
			{
				if (table == null || Custom.rainWorld == null || Futile.atlasManager == null || !Futile.atlasManager.DoesContainFontWithName(font)) return;
				inititalized = true;

				// Init sprites
				container = new FContainer()
				{
					x = 20.01f,
					y = yOffset + 0.01f
				};
				Futile.stage.AddChild(container);

				label = new FLabel(Custom.GetFont(), $"CWT : {TypeName(typeof(TKey))} -> {TypeName(typeof(TValue))}")
				{
					alignment = FLabelAlignment.Left,
					color = useColor
				};
				container.AddChild(label);

				sprites = [];

				// Cache elements

				onElement = Futile.atlasManager.GetElementWithName("mouseEyeA1");
				offElement = Futile.atlasManager.GetElementWithName("mouseEyeB3");
			}

			public override void Update()
			{
				if (!Input.GetKeyDown(KeyCode.PageDown)) return;

				if (!inititalized)
				{
					TryInit();
					return;
				}
				else if (!Futile.atlasManager.DoesContainFontWithName(font))
				{
					inititalized = false;
					return;
				}

				container.MoveToFront();
				List<bool> results = table.__DebugYieldFilled();
				StringBuilder sb = new StringBuilder(label.text);
				for (int i = 0; i < results.Count; i++)
				{
					FSprite sprite;
					if (i >= sprites.Count)
					{
						sprite = new FSprite("pixel")
						{
							x = 200f + i * 10f,
							y = LabelTest.LineHeight(false) / 2f - 3.5f
						};
						container.AddChild(sprite);
					}
					else
					{
						sprite = sprites[i];
					}

					sprite.element = results[i] ? onElement : offElement;
					sb.Append(results[i] ? " O" : " -");
				}
				LogDebug(sb.ToString());
			}
		}
	}
#nullable restore
#endif

}
