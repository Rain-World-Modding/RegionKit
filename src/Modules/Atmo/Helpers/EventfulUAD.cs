namespace RegionKit.Modules.Atmo.Helpers;

/// <summary>
/// A composable UAD subclass.
/// </summary>
public class EventfulUAD : UpdatableAndDeletable, IDrawable
{
	/// <summary>
	/// This is used to distinguish eventfuls between each other.
	/// </summary>
	public Guid id = Guid.NewGuid();
	private bool _initRan = false;
	/// <summary>
	/// Code that should run on first update only.
	/// </summary>
	public Action? onInit;
	/// <summary>
	/// Code that should run every update.
	/// </summary>
	public Action<bool>? onUpdate;
	/// <summary>
	/// Code that should run on paused updates.
	/// </summary>
	public Action? onPausedUpdate;
	/// <summary>
	/// Code that should run on destruct.
	/// </summary>
	public Action? onDestroy;
	/// <summary>
	/// Code that should run on IDrawable.InitiateSprites.
	/// </summary>
	public Action<RoomCamera.SpriteLeaser, RoomCamera>? onInitSprites;
	/// <summary>
	/// Code that should run on IDrawable.DrawSprites.
	/// </summary>
	public Action<RoomCamera.SpriteLeaser, RoomCamera, float, Vector2>? onDraw;
	/// <summary>
	/// Code that should run on IDrawable.ApplyPalette.
	/// </summary>
	public Action<RoomCamera.SpriteLeaser, RoomCamera, RoomPalette>? onApplyPalette;
	/// <summary>
	/// Code that should run on IDrawable.AddToContainer.
	/// </summary>
	public Action<RoomCamera.SpriteLeaser, RoomCamera, FContainer?>? onAddToContainer;
	/// <summary>
	/// Additional data bundled with the object.
	/// </summary>
	public readonly Dictionary<string, object?> data = new();
	/// <summary>
	/// Performs a lookup in the Data field.
	/// </summary>
	public object? this[string field]
	{
		get => data.EnsureAndGet(field, () => null);
		set { data[field] = value; }
	}
	/// <inheritdoc/>
	public override void Update(bool eu)
	{
		base.Update(eu);
		try
		{
			onUpdate?.Invoke(eu);
			if (!_initRan) onInit?.Invoke();
		}
		catch (Exception ex)
		{
			__ReportError(this, Update, ex);
		}
		finally
		{
			_initRan = true;
		}

	}
	/// <inheritdoc/>
	public override void PausedUpdate()
	{
		base.PausedUpdate();
		try
		{
			onPausedUpdate?.Invoke();
		}
		catch (Exception ex)
		{
			__ReportError(this, PausedUpdate, ex);
		}
	}
	/// <inheritdoc/>
	public override void Destroy()
	{
		base.Destroy();
		try
		{
			onDestroy?.Invoke();
		}
		catch (Exception ex)
		{
			__ReportError(this, Destroy, ex);
		}
	}
	/// <inheritdoc/>
	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		//if (onInitSprites is null) return;
		try
		{
			onInitSprites?.Invoke(sLeaser, rCam);
		}
		catch (Exception ex)
		{
			__ReportError(this, InitiateSprites, ex);
		}
		finally
		{
			sLeaser.sprites ??= new FSprite[0];
		}
	}
	/// <inheritdoc/>
	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		if (onDraw is null) return;
		try
		{
			onDraw?.Invoke(sLeaser, rCam, timeStacker, camPos);
		}
		catch (Exception ex)
		{
			__ReportError(this, DrawSprites, ex);
		}
	}
	/// <inheritdoc/>
	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		try
		{
			onApplyPalette?.Invoke(sLeaser, rCam, palette);
		}
		catch (Exception ex)
		{
			__ReportError(this, ApplyPalette, ex);
		}
	}
	/// <inheritdoc/>
	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContatiner)
	{
		try
		{
			onAddToContainer?.Invoke(sLeaser, rCam, newContatiner);
		}
		catch (Exception ex)
		{
			__ReportError(this, PausedUpdate, ex);
		}
	}

	private static void __ReportError(EventfulUAD uad, Delegate where, object? err)
	{
		LogError($"EventfulUAD {uad.id}: error on {where.Method.Name}: {err}");
	}
	/// <inheritdoc/>
	public class Extra<T> : EventfulUAD
	{
		/// <summary>
		/// Creates a new instance with specified contents.
		/// </summary>
		public Extra(T item)
		{
			_0 = item;
		}

		/// <summary>
		/// First extra item.
		/// </summary>
		public T _0;
		/// <inheritdoc/>
		public void Deconstruct(out T? i0)
		{
			i0 = this._0;
		}
	}
	/// <inheritdoc/>
	public class Extra<T0, T1> : EventfulUAD
	{
		/// <inheritdoc/>
		public Extra(T0 item0, T1 item1)
		{
			_0 = item0;
			_1 = item1;
		}

		/// <summary>
		/// First extra item.
		/// </summary>
		public T0 _0;
		/// <summary>
		/// Second extra item.
		/// </summary>
		public T1 _1;/// <inheritdoc/>
		public void Deconstruct(out T0? i0, out T1 i1)
		{
			i0 = _0;
			i1 = _1;
		}
	}
	/// <inheritdoc/>
	public class Extra<T0, T1, T2> : EventfulUAD
	{
		/// <inheritdoc/>
		public Extra(T0 item0, T1 item1, T2 item2)
		{
			_0 = item0;
			_1 = item1;
			_2 = item2;
		}
		/// <summary>
		/// First extra item.
		/// </summary>
		public T0 _0;
		/// <summary>
		/// Second extra item.
		/// </summary>
		public T1 _1;
		/// <summary>
		/// Third extra item.
		/// </summary>
		public T2 _2;
		/// <inheritdoc/>
		public void Deconstruct(out T0 i0, out T1 i1, out T2 i2)
		{
			i0 = _0;
			i1 = _1;
			i2 = _2;
		}
	}
}
